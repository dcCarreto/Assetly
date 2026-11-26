using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Assetly.Aplicacao.Dados;
using Assetly.Apresentacao.Leitura;
using Assetly.Dominio.Documento;
using static Assetly.Apresentacao.Visao.AcoesApoio;
using static Assetly.Apresentacao.Visao.ContextoApoio;
using static Assetly.Apresentacao.Visao.DialogoApoio;
using static Assetly.Apresentacao.Visao.LayoutApoio;

namespace Assetly.Apresentacao.Visao;

internal sealed class ModuloDocumentos : IModulo
{
    private readonly ContextoAplicacao _contexto;
    private readonly DataGrid _tabela = new();
    private readonly FiltroTabela<DocumentoLinha> _filtro;
    private readonly TextBlock _resumoEsquerda = new();
    private readonly TextBlock _resumoDireita = new();
    private int _contador;

    public string Titulo => "Documentos";
    public string Descricao => "Arquivos vinculados";
    public bool ComContador => true;
    public UIElement Conteudo { get; }
    public int Contador => _contador;

    internal ModuloDocumentos(ContextoAplicacao contexto)
    {
        _contexto = contexto;
        ConfigurarTabela();

        var (direita, busca) = CabecalhoBuscaCriacao("Buscar por bem, tipo ou nome do documento", "Novo documento", () => AbrirDialogoDocumento(null));

        _filtro = new FiltroTabela<DocumentoLinha>(_tabela, l => l.Id, l => $"{l.Bem} {l.Tipo} {l.Nome} {l.NotaEletronica} {l.Origem.ChaveAcesso}",
        [
            new("Todos", _ => true),
            new("Disponíveis", l => l.Origem.Status == StatusDocumento.Disponivel),
            new("Ausentes", l => l.StatusClasse == "etiqueta-ausente"),
            new("Arquivados", l => l.StatusClasse == "etiqueta-arquivado")
        ]) { TextoSemRegistros = "Nenhum documento. Anexe notas fiscais, manuais e comprovantes aos seus bens — eles entram no backup." };
        _filtro.ConectarBusca(busca);

        var contextual = BarraContextual(_tabela,
        [
            ("Abrir", () => AbrirDocumento(SelecionadoAtual().Origem)),
            ("Mostrar na pasta", () => MostrarNaPasta(SelecionadoAtual().Origem)),
            ("Editar", () => AbrirDialogoDocumento(SelecionadoAtual().Origem)),
            ("Consultar nota", () => ConsultarNota(SelecionadoAtual().Origem)),
            ("Marcar ausente", () => MarcarDocumentoAusente(SelecionadoAtual().Origem))
        ],
        [
            ("Arquivar", () => ArquivarDocumento(SelecionadoAtual().Origem)),
            ("Excluir", () => ExcluirDocumento(SelecionadoAtual()))
        ]);

        Conteudo = TelaComTabela("Documentos", "Notas fiscais, comprovantes e arquivos ligados aos bens.", direita,
            _filtro.Barra, _tabela, contextual, BarraResumoTabela(_resumoEsquerda, _resumoDireita));

        ConfigurarAtalhosTabela(Conteudo, _tabela, busca, () => AbrirDialogoDocumento(null),
            () => AbrirDialogoDocumento(SelecionadoAtual().Origem), () => ExcluirDocumento(SelecionadoAtual()));
    }

    public bool Localizar(object chave) => _filtro.Localizar(chave);

    public void Atualizar()
    {
        var documentos = _contexto.ListarDocumentos().Executar();
        _contador = documentos.Count;
        _filtro.AtualizarDados(ConstruirLinhas(documentos));
    }

    private DocumentoLinha SelecionadoAtual() => Selecionado<DocumentoLinha>(_tabela, "Selecione um documento.");

    private void ConfigurarTabela()
    {
        TabelaApoio.AplicarEstiloBase(_tabela);
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Bem", nameof(DocumentoLinha.Bem), Estrela(1.3), estiloBase: "CelulaDestaqueStyle"));
        _tabela.Columns.Add(TabelaApoio.ColunaEtiqueta("Status", nameof(DocumentoLinha.Status), nameof(DocumentoLinha.StatusClasse), new DataGridLength(128)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Tipo", nameof(DocumentoLinha.Tipo), new DataGridLength(150)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Nome", nameof(DocumentoLinha.Nome), Estrela(1.6)));
        _tabela.Columns.Add(TabelaApoio.ColunaArquivo("Arquivo local", nameof(DocumentoLinha.ArquivoLocal), Estrela(1.3),
            contexto => AbrirDocumentoDaLinha((DocumentoLinha)contexto)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Nota eletrônica", nameof(DocumentoLinha.NotaEletronica), new DataGridLength(140), estiloBase: "CelulaSecundariaStyle"));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Registrado", nameof(DocumentoLinha.Registrado), new DataGridLength(110), nameof(DocumentoLinha.RegistradoOrdenacao)));
        TabelaApoio.ConfigurarLinhas<DocumentoLinha>(_tabela, l => l.LinhaClasse, MenuContexto, l => ExecutarComTratamento(() => AbrirDocumento(l.Origem)));
    }

    private IReadOnlyList<DocumentoLinha> ConstruirLinhas(IReadOnlyList<Documento> documentos)
    {
        var nomeBem = NomesBens(_contexto);
        var disponiveis = documentos.Count(d => d.Status == StatusDocumento.Disponivel);
        var ausentes = documentos.Count(d => d.Status == StatusDocumento.Ausente);
        _resumoEsquerda.Text = $"{documentos.Count} documentos · {disponiveis} disponíve{(disponiveis == 1 ? "l" : "is")} · {ausentes} ausente{(ausentes == 1 ? "" : "s")}";
        _resumoDireita.Text = $"Pasta {_contexto.DiretorioDocumentos}";

        return documentos.Select(d => new DocumentoLinha(
            d.Id,
            nomeBem(d.BemId),
            Formatador.TextoEnum(d.Tipo),
            d.Nome.Valor,
            d.CaminhoLocal.Valor,
            Formatador.Data(d.RegistradoEm),
            // 22 selos "Disponível" iguais não dizem nada: só as exceções (Ausente, Arquivado) ganham selo.
            d.Status == StatusDocumento.Disponivel ? string.Empty : Formatador.TextoEnum(d.Status),
            Formatador.ClasseEtiqueta(d.Status),
            ClasseLinha(d.Status),
            ChaveAcessoNfe.TentarCriar(d.ChaveAcesso) is { } chave ? $"{chave.Modelo} nº {chave.Numero}" : string.Empty,
            d)).ToList();
    }

    private static string ClasseLinha(StatusDocumento status) => status switch
    {
        StatusDocumento.Disponivel => "linha-ok",
        StatusDocumento.Ausente => "linha-aviso",
        StatusDocumento.Arquivado => "linha-neutra",
        _ => "linha-neutra"
    };

    private void AbrirDocumentoDaLinha(DocumentoLinha linha)
    {
        _tabela.SelectedItem = linha;
        ExecutarComTratamento(() => AbrirDocumento(linha.Origem));
    }

    private ContextMenu MenuContexto(DocumentoLinha linha)
    {
        var documento = linha.Origem;
        var menu = new ContextMenu();
        menu.Items.Add(ItemMenu("Abrir documento", () => AbrirDocumento(documento), documento.Status != StatusDocumento.Ausente));
        menu.Items.Add(ItemMenu("Mostrar na pasta", () => MostrarNaPasta(documento), documento.Status != StatusDocumento.Ausente));
        menu.Items.Add(ItemMenu("Editar", () => AbrirDialogoDocumento(documento)));
        menu.Items.Add(ItemMenu("Consultar nota na Sefaz", () => ConsultarNota(documento), !string.IsNullOrEmpty(documento.ChaveAcesso)));
        menu.Items.Add(new Separator());
        menu.Items.Add(ItemMenu("Marcar ausente", () => MarcarDocumentoAusente(documento), documento.Status != StatusDocumento.Ausente));
        menu.Items.Add(ItemMenu("Arquivar", () => ArquivarDocumento(documento), documento.Status != StatusDocumento.Arquivado));
        menu.Items.Add(ItemMenu("Excluir…", () => ExcluirDocumento(linha)));
        return menu;
    }

    private static MenuItem ItemMenu(string texto, Action acao, bool habilitado = true)
    {
        var item = new MenuItem { Header = texto, IsEnabled = habilitado };
        item.Click += (_, _) => ExecutarComTratamento(acao);
        return item;
    }

    internal void AbrirDialogoDocumento(Documento? documento)
    {
        var bemSelecionado = documento is null ? null : _contexto.BuscarBem().Executar(documento.BemId);
        var bem = ComboBens(_contexto, bemSelecionado);
        bem.IsEnabled = documento is null;
        var tipo = Combo(documento?.Tipo ?? TipoDocumento.NotaFiscal);
        var nome = CampoTexto("Nome", documento?.Nome.Valor ?? string.Empty);
        // O caminho completo (interno, na pasta privada) fica guardado aqui, fora da tela; o formulário mostra só o nome.
        var caminho = new TextBox { Text = documento?.CaminhoLocal.Valor ?? string.Empty };
        var registradoEm = DataPicker(documento?.RegistradoEm ?? Hoje());
        var grade = GradeFormulario();
        AdicionarCampo(grade, 0, 0, "Bem *", bem);
        AdicionarCampo(grade, 0, 1, "Tipo *", tipo);
        AdicionarCampo(grade, 1, 0, "Nome *", nome);
        AdicionarCampo(grade, 1, 1, "Registrado em *", registradoEm);
        AdicionarCampo(grade, 2, 0, "Arquivo *", SeletorArquivo(caminho, nome, documento is null ? "Selecionar…" : "Substituir…"), ocupaLinhaToda: true);

        var chave = CampoTexto("44 dígitos (opcional)", ChaveAcessoNfe.TentarCriar(documento?.ChaveAcesso)?.Formatada ?? string.Empty);
        var url = CampoTexto("Link do QR Code (opcional)", documento?.UrlConsulta ?? string.Empty);
        var situacao = UiApoio.Texto("Nota fiscal eletrônica (NF-e/NFC-e): informe a chave de acesso ou leia o QR Code de uma foto da nota.",
            11.5, FontWeights.Normal, (System.Windows.Media.Brush)Application.Current.Resources["TextMuted"]);
        situacao.TextWrapping = TextWrapping.Wrap;
        var lerQr = BotaoSecundario("Ler QR Code / OCR da imagem", () =>
        {
            var imagem = !string.IsNullOrWhiteSpace(caminho.Text) && LeitorNotaImagem.EhImagem(caminho.Text) && File.Exists(caminho.Text.Trim())
                ? caminho.Text.Trim()
                : SelecionarImagem();
            if (imagem is null)
            {
                return;
            }

            var cursor = System.Windows.Input.Mouse.OverrideCursor;
            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            LeituraImagemNota leitura;
            try
            {
                leitura = LeitorNotaImagem.Ler(imagem, Hoje());
            }
            finally
            {
                System.Windows.Input.Mouse.OverrideCursor = cursor;
            }

            if (leitura.Dados.Chave is { } lida)
            {
                chave.Text = lida.Formatada;
            }

            if (leitura.Dados.UrlConsulta is { } link)
            {
                url.Text = link;
            }

            if (leitura.Dados.Emissao is { } emissao)
            {
                registradoEm.SelectedDate = emissao.ToDateTime(TimeOnly.MinValue);
            }

            if (string.IsNullOrWhiteSpace(caminho.Text))
            {
                caminho.Text = imagem;
                if (string.IsNullOrWhiteSpace(nome.Text))
                {
                    nome.Text = leitura.Dados.Chave is { } c ? $"Nota fiscal nº {c.Numero}" : Path.GetFileNameWithoutExtension(imagem);
                }
            }

            situacao.Text = LeitorNotaImagem.Descrever(leitura);
        });
        lerQr.HorizontalAlignment = HorizontalAlignment.Left;
        AdicionarSecao(grade, 3, "Nota fiscal eletrônica");
        AdicionarCampo(grade, 4, 0, "Chave de acesso", chave);
        AdicionarCampo(grade, 4, 1, "Link de consulta", url);
        AdicionarCampo(grade, 5, 0, "Leitura automática", UiApoio.Coluna(6, lerQr, situacao), ocupaLinhaToda: true);

        var nota = UiApoio.Texto("O arquivo escolhido é copiado para a pasta privada do Assetly — o original pode ser movido ou apagado depois.",
            11.5, FontWeights.Normal, (System.Windows.Media.Brush)Application.Current.Resources["TextMuted"]);
        nota.Margin = new Thickness(0, 10, 0, 0);
        Grid.SetRow(nota, 6);
        Grid.SetColumnSpan(nota, 2);
        while (grade.RowDefinitions.Count < 7)
        {
            grade.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        grade.Children.Add(nota);

        var salvou = MostrarFormulario(documento is null ? "Novo documento" : "Editar documento", grade, () =>
        {
            var registro = DataObrigatoria(registradoEm, "registro");
            if (string.IsNullOrWhiteSpace(caminho.Text) && documento is null)
            {
                throw new ArgumentException("selecione o arquivo do documento.");
            }

            if (documento is null)
            {
                _contexto.RegistrarDocumento().Executar(new RegistrarDocumentoComando(ExigirBemSelecionado(bem).Id, ValorEnum<TipoDocumento>(tipo), nome.Text, caminho.Text.Trim(), registro,
                    TextoOpcional(chave.Text), TextoOpcional(url.Text)));
            }
            else
            {
                _contexto.EditarDocumento().Executar(new EditarDocumentoComando(documento.Id, ValorEnum<TipoDocumento>(tipo), nome.Text, caminho.Text.Trim(), registro,
                    TextoOpcional(chave.Text), TextoOpcional(url.Text)));
            }
        });

        if (salvou)
        {
            _contexto.NotificarAlteracao();
        }
    }

    private static FrameworkElement SeletorArquivo(TextBox campo, TextBox nome, string textoBotao)
    {
        var exibicao = CampoTexto("Nenhum arquivo escolhido", NomeExibido(campo.Text));
        exibicao.IsReadOnly = true;
        exibicao.Focusable = false;
        exibicao.ToolTip = string.IsNullOrEmpty(campo.Text) ? null : campo.Text;
        campo.TextChanged += (_, _) =>
        {
            exibicao.Text = NomeExibido(campo.Text);
            exibicao.ToolTip = campo.Text;
        };
        var selecionar = BotaoSecundario(textoBotao, () =>
        {
            var arquivo = SelecionarArquivo("Selecionar documento", campo.Text);
            if (arquivo is null)
            {
                return;
            }

            campo.Text = arquivo;
            if (string.IsNullOrWhiteSpace(nome.Text))
            {
                nome.Text = Path.GetFileNameWithoutExtension(arquivo);
            }
        });
        selecionar.Margin = new Thickness(8, 0, 0, 0);
        var grade = new Grid();
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(exibicao, 0);
        Grid.SetColumn(selecionar, 1);
        grade.Children.Add(exibicao);
        grade.Children.Add(selecionar);
        return grade;
    }

    /// <summary>Nome do arquivo sem a pasta e sem o identificador que a cópia privada ganha ("{guid}-nota.pdf").</summary>
    private static string NomeExibido(string caminho)
    {
        var nome = Path.GetFileName(caminho.Trim());
        return nome.Length > 37 && nome[36] == '-' && Guid.TryParse(nome[..36], out _) ? nome[37..] : nome;
    }

    private static string ArquivoExistente(Documento documento)
    {
        if (documento.Status == StatusDocumento.Ausente)
        {
            throw new InvalidOperationException("documento marcado como ausente não possui arquivo disponível. Use Editar para anexar o arquivo.");
        }

        var arquivo = Path.GetFullPath(documento.CaminhoLocal.Valor);
        if (!File.Exists(arquivo))
        {
            throw new InvalidOperationException($"o arquivo do documento não foi encontrado: {arquivo}\n\nUse Editar para escolher o arquivo novamente, ou Marcar ausente.");
        }

        return arquivo;
    }

    internal static void AbrirDocumento(Documento documento)
    {
        var arquivo = ArquivoExistente(documento);
        try
        {
            Process.Start(new ProcessStartInfo(arquivo) { UseShellExecute = true });
        }
        catch (Exception excecao) when (excecao is Win32Exception or InvalidOperationException)
        {
            throw new InvalidOperationException($"não foi possível abrir o documento (verifique se há um programa instalado para este tipo de arquivo): {arquivo}", excecao);
        }
    }

    private static void MostrarNaPasta(Documento documento)
    {
        var arquivo = ArquivoExistente(documento);
        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{arquivo}\"") { UseShellExecute = true });
    }

    /// <summary>Abre a consulta pública da nota: o link do QR Code (portal da Sefaz do estado) ou, sem ele,
    /// o portal nacional da NF-e com a chave já copiada para a área de transferência.</summary>
    private static void ConsultarNota(Documento documento)
    {
        var chave = ChaveAcessoNfe.TentarCriar(documento.ChaveAcesso)
            ?? throw new InvalidOperationException("este documento não tem chave de acesso. Use Editar para informar a chave ou ler o QR Code da nota.");
        if (!string.IsNullOrWhiteSpace(documento.UrlConsulta))
        {
            Process.Start(new ProcessStartInfo(documento.UrlConsulta) { UseShellExecute = true });
            return;
        }

        Clipboard.SetText(chave.Valor);
        MostrarInformacao("Consultar nota",
            $"{chave.Modelo} nº {chave.Numero} — emitente CNPJ {chave.CnpjFormatado} ({chave.Uf}).\n\nA chave de acesso foi copiada. " +
            "O portal da Nota Fiscal será aberto: cole a chave no campo de consulta.");
        Process.Start(new ProcessStartInfo("https://www.nfe.fazenda.gov.br/portal/consultaRecaptcha.aspx?tipoConsulta=resumo") { UseShellExecute = true });
    }

    private void MarcarDocumentoAusente(Documento documento)
    {
        _contexto.MarcarDocumentoAusente().Executar(documento.Id);
        _contexto.NotificarAlteracao();
    }

    private void ArquivarDocumento(Documento documento)
    {
        if (!MostrarConfirmacao("Arquivar documento", "O documento será arquivado (continua disponível para consulta, mas deixa de contar como nota fiscal válida). Deseja continuar?", "Arquivar"))
        {
            return;
        }

        _contexto.ArquivarDocumento().Executar(documento.Id);
        _contexto.NotificarAlteracao();
    }

    private void ExcluirDocumento(DocumentoLinha linha)
    {
        if (!MostrarConfirmacao("Excluir documento",
                $"\"{linha.Nome}\" será excluído permanentemente, junto com a cópia do arquivo guardada pelo Assetly. Deseja continuar?",
                "Excluir"))
        {
            return;
        }

        _contexto.ExcluirDocumento().Executar(linha.Id);
        _contexto.NotificarAlteracao();
    }
}
