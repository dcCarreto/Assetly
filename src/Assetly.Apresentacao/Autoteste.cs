using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Assetly.Apresentacao.Visao;
using Assetly.Infraestrutura.Armazenamento;
using Assetly.Infraestrutura.Atualizacao;

namespace Assetly.Apresentacao;

/// <summary>
/// Verificação de ponta a ponta do executável publicado, usada pelo workflow de release antes de publicar
/// (e depois de instalar pelo instalador): <c>Assetly.exe --autoteste [--relatorio=arquivo] [--telas=pasta]
/// [--versao-esperada=AAAA.MM.DD]</c>. Tudo acontece numa pasta temporária — os dados do usuário nunca são
/// tocados — e nenhuma janela é aberta: as telas são desenhadas em memória. Código de saída 0 = tudo certo.
/// </summary>
internal static class Autoteste
{
    internal const string Argumento = "--autoteste";

    internal static int Executar(string[] argumentos)
    {
        var relatorio = new StringBuilder();
        var arquivoRelatorio = Valor(argumentos, "--relatorio=") ?? Path.Combine(Path.GetTempPath(), "Assetly-Autoteste.txt");
        var pastaTelas = Valor(argumentos, "--telas=");
        var versaoEsperada = Valor(argumentos, "--versao-esperada=");
        var pasta = Path.Combine(Path.GetTempPath(), "Assetly-Autoteste-" + Guid.NewGuid().ToString("N")[..8]);
        var falhas = 0;

        void Etapa(string nome, Func<string> acao)
        {
            try
            {
                relatorio.AppendLine($"[ok]    {nome}: {acao()}");
            }
            catch (Exception excecao)
            {
                falhas++;
                relatorio.AppendLine($"[FALHA] {nome}: {excecao}");
            }
        }

        relatorio.AppendLine($"Assetly — autoteste ({DateTime.Now:dd/MM/yyyy HH:mm:ss})");
        relatorio.AppendLine($"Executável: {Environment.ProcessPath}");
        try
        {
            Etapa("Versão", () =>
            {
                var versao = ServicoAtualizacaoGithub.VersaoDoPrograma();
                if (versaoEsperada is not null && versao?.Identificador != versaoEsperada)
                {
                    throw new InvalidOperationException($"esperada {versaoEsperada}, encontrada {versao?.Identificador ?? "(nenhuma)"}");
                }

                return versao?.Identificador ?? "desenvolvimento";
            });

            ContextoAplicacao? contexto = null;
            Etapa("Banco novo, migrações e dados fictícios", () =>
            {
                contexto = ContextoAplicacao.Isolado(Path.Combine(pasta, "dados"), semearDadosFicticios: true);
                AparenciaApoio.Aplicar(contexto.ObterPreferencias().Executar());
                var bens = contexto.ListarBens().Executar().Count;
                return bens > 0 ? $"{bens} bens" : throw new InvalidOperationException("nenhum bem semeado");
            });

            if (contexto is null)
            {
                return Concluir(relatorio, arquivoRelatorio, falhas, pasta);
            }

            Etapa("Telas (todos os módulos e abas)", () => DesenharTelas(contexto, pastaTelas));

            Etapa("Exportação JSON", () =>
            {
                using var json = JsonDocument.Parse(contexto.ExportarDados().Executar());
                var dados = json.RootElement.GetProperty("dados");
                var vazios = new[] { "bens", "garantias", "manutencoes", "documentos", "veiculos", "imoveis" }
                    .Where(nome => dados.GetProperty(nome).GetArrayLength() == 0).ToList();
                return vazios.Count == 0 ? $"{dados.EnumerateObject().Count()} coleções" : throw new InvalidOperationException("coleções vazias: " + string.Join(", ", vazios));
            });

            Etapa("Painel patrimonial", () =>
            {
                var resumo = contexto.CalcularPatrimonio().Executar(DateOnly.FromDateTime(DateTime.Today));
                return resumo.ValorAtual > 0 && resumo.ValorAtual <= resumo.ValorCompra
                    ? $"{resumo.Itens.Count} itens, valor atual {resumo.ValorAtual:C}"
                    : throw new InvalidOperationException($"valores incoerentes: compra {resumo.ValorCompra}, atual {resumo.ValorAtual}");
            });

            Etapa("Backup criptografado (gerar, senha errada, restaurar)", () => TestarBackup(contexto, pasta));
        }
        catch (Exception excecao)
        {
            falhas++;
            relatorio.AppendLine($"[FALHA] inesperada: {excecao}");
        }

        return Concluir(relatorio, arquivoRelatorio, falhas, pasta);
    }

    private static string TestarBackup(ContextoAplicacao contexto, string pasta)
    {
        const string senha = "senha-do-autoteste";
        var servico = new ServicoBackupLocal();
        var arquivo = Path.Combine(pasta, "backup" + ArquivoProtegido.Extensao);
        servico.GerarArquivo(contexto.ArquivoBanco, contexto.DiretorioDocumentos, arquivo, senha);
        if (!ArquivoProtegido.EhProtegidoPorSenha(arquivo))
        {
            throw new InvalidOperationException("o arquivo gerado não está protegido por senha");
        }

        var destino = Path.Combine(pasta, "restaurado");
        var banco = Path.Combine(destino, "banco", "assetly.sqlite3");
        var documentos = Path.Combine(destino, "documentos");
        Directory.CreateDirectory(Path.GetDirectoryName(banco)!);
        try
        {
            servico.RestaurarBackup(arquivo, banco, documentos, "senha-errada");
            throw new InvalidOperationException("a restauração com senha errada foi aceita");
        }
        catch (Exception excecao) when (excecao is not InvalidOperationException { Message: "a restauração com senha errada foi aceita" })
        {
            // esperado
        }

        servico.RestaurarBackup(arquivo, banco, documentos, senha);
        var restaurado = ContextoAplicacao.Isolado(destino, semearDadosFicticios: false);
        var original = contexto.ExportarDados().ImpressaoDigital();
        var copia = restaurado.ExportarDados().ImpressaoDigital();
        return original == copia
            ? $"{new FileInfo(arquivo).Length / 1024} KB, conteúdo idêntico após restaurar"
            : throw new InvalidOperationException("o conteúdo restaurado é diferente do original");
    }

    /// <summary>Monta a tela principal real e desenha cada módulo (e cada aba) em memória — pega exceções
    /// de montagem/atualização que só aparecem com dados. Com <c>--telas</c>, grava os PNGs.</summary>
    private static string DesenharTelas(ContextoAplicacao contexto, string? pastaTelas)
    {
        if (pastaTelas is not null)
        {
            Directory.CreateDirectory(pastaTelas);
        }

        var tela = new TelaPrincipal(contexto, () => { });
        var desenhadas = 0;
        try
        {
            foreach (var modulo in tela.Modulos)
            {
                tela.IrPara(modulo.Titulo);
                Desenhar(tela.Raiz, pastaTelas, $"{++desenhadas:D2}-{modulo.Titulo}");
                if (modulo is ModuloComAbas abas)
                {
                    for (var indice = 1; indice < abas.QuantidadeAbas; indice++)
                    {
                        abas.Selecionar(indice);
                        Desenhar(tela.Raiz, pastaTelas, $"{++desenhadas:D2}-{modulo.Titulo}-{indice + 1}");
                    }

                    abas.Selecionar(0);
                }
            }

            desenhadas += DesenharFormularios(contexto, tela, pastaTelas);
        }
        finally
        {
            DialogoApoio.InterceptarDialogo = null;
            tela.Encerrar();
        }

        return $"{desenhadas} telas e formulários desenhados";
    }

    /// <summary>Abre os formulários principais sem exibi-los (o diálogo é interceptado e desenhado em memória).</summary>
    private static int DesenharFormularios(ContextoAplicacao contexto, TelaPrincipal tela, string? pastaTelas)
    {
        var abertos = 0;
        DialogoApoio.InterceptarDialogo = janela =>
        {
            abertos++;
            DesenharJanela(janela, pastaTelas, $"formulario-{abertos:D2}-{janela.Title}");
        };
        var bens = tela.Modulos.OfType<ModuloBens>().Single();
        var documentos = tela.Modulos.OfType<ModuloDocumentos>().Single();
        bens.AbrirDialogoBem(null);
        bens.AbrirDialogoBem(contexto.ListarBens().Executar().First(b => b.ResponsavelId is not null));
        documentos.AbrirDialogoDocumento(null);
        documentos.AbrirDialogoDocumento(contexto.ListarDocumentos().Executar().First(d => !string.IsNullOrEmpty(d.ChaveAcesso)));
        return abertos == 4 ? abertos : throw new InvalidOperationException($"esperados 4 formulários, abertos {abertos}");
    }

    private static void DesenharJanela(Window janela, string? pastaTelas, string nome)
    {
        var conteudo = (FrameworkElement)janela.Content;
        janela.Content = null;
        var largura = double.IsNaN(janela.Width) ? 520 : janela.Width;
        var moldura = new Border { Background = janela.Background, Width = largura, Child = conteudo };
        moldura.Measure(new Size(largura, 4000));
        var altura = Math.Max(1, Math.Ceiling(moldura.DesiredSize.Height));
        moldura.Arrange(new Rect(0, 0, largura, altura));
        moldura.UpdateLayout();
        if (pastaTelas is null)
        {
            return;
        }

        var bitmap = new RenderTargetBitmap((int)largura, (int)altura, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(moldura);
        var codificador = new PngBitmapEncoder();
        codificador.Frames.Add(BitmapFrame.Create(bitmap));
        var arquivo = string.Concat(nome.Split(Path.GetInvalidFileNameChars())).Replace(' ', '-');
        using var fluxo = File.Create(Path.Combine(pastaTelas, arquivo + ".png"));
        codificador.Save(fluxo);
    }

    private static void Desenhar(UIElement raiz, string? pastaTelas, string nome)
    {
        const int largura = 1440;
        const int altura = 900;
        var moldura = new Grid { Background = (Brush)Application.Current.Resources["BgChrome"], Width = largura, Height = altura };
        moldura.Children.Add(raiz);
        try
        {
            for (var passada = 0; passada < 3; passada++)
            {
                moldura.Measure(new Size(largura, altura));
                moldura.Arrange(new Rect(0, 0, largura, altura));
                moldura.UpdateLayout();
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
            }

            if (pastaTelas is null)
            {
                return;
            }

            var bitmap = new RenderTargetBitmap(largura, altura, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(moldura);
            var codificador = new PngBitmapEncoder();
            codificador.Frames.Add(BitmapFrame.Create(bitmap));
            var arquivo = string.Concat(nome.Split(Path.GetInvalidFileNameChars())).Replace(' ', '-');
            using var fluxo = File.Create(Path.Combine(pastaTelas, arquivo + ".png"));
            codificador.Save(fluxo);
        }
        finally
        {
            moldura.Children.Clear();
        }
    }

    private static int Concluir(StringBuilder relatorio, string arquivoRelatorio, int falhas, string pasta)
    {
        try
        {
            Directory.Delete(pasta, recursive: true);
        }
        catch (Exception excecao) when (excecao is IOException or UnauthorizedAccessException)
        {
            // pasta temporária; o SQLite pode ainda segurar o arquivo por um instante
        }

        relatorio.AppendLine(falhas == 0 ? "RESULTADO: tudo certo" : $"RESULTADO: {falhas} falha(s)");
        File.WriteAllText(arquivoRelatorio, relatorio.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        return falhas == 0 ? 0 : 1;
    }

    private static string? Valor(string[] argumentos, string prefixo) =>
        argumentos.FirstOrDefault(a => a.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase))?[prefixo.Length..].Trim('"');
}
