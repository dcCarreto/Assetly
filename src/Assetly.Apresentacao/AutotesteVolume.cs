using System.Diagnostics;
using System.IO;
using Assetly.Apresentacao.Visao;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Despesa;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Manutencao;
using Assetly.Dominio.Veiculo;
using Assetly.Infraestrutura.Armazenamento;

namespace Assetly.Apresentacao;

/// <summary>
/// <c>--autoteste --volume[=MB]</c>: monta um banco grande numa pasta temporária (500 bens, 5 mil contas, 2 mil
/// documentos com anexos que somam ~MB megabytes, garantias, manutenções e abastecimentos) e mede o que cresce com os
/// dados: o ciclo completo de atualização da tela, a busca, a exportação/impressão digital da sincronização, o backup,
/// o arquivo criptografado e o teste do backup. Os tempos vão para o relatório; só o ciclo da tela tem limite
/// (<see cref="LimiteCicloTela"/>), porque ele roda na thread da interface.
/// </summary>
internal static class AutotesteVolume
{
    internal const string Argumento = "--volume";
    internal static readonly TimeSpan LimiteCicloTela = TimeSpan.FromSeconds(5);

    private const int Bens = 500;
    private const int Despesas = 5_000;
    private const int Documentos = 2_000;
    private const int Garantias = 500;
    private const int Manutencoes = 1_000;
    private const int Abastecimentos = 2_000;

    internal static void Executar(Action<string, Func<string>> etapa, string pasta, int megabytesAnexos)
    {
        ContextoAplicacao? contexto = null;
        etapa($"Volume: montar banco grande ({Bens} bens, {Despesas} contas, {Documentos} documentos, ~{megabytesAnexos} MB de anexos)", () =>
        {
            var relogio = Stopwatch.StartNew();
            contexto = ContextoAplicacao.Isolado(Path.Combine(pasta, "volume"), semearDadosFicticios: false);
            Semear(contexto, megabytesAnexos);
            return $"{relogio.Elapsed.TotalSeconds:0.0} s";
        });
        if (contexto is null)
        {
            return;
        }

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        etapa("Volume: alertas automáticos (1ª e 2ª vez)", () =>
        {
            var primeira = Medir(() => contexto.GerarAlertasAutomaticos().Executar(hoje));
            var segunda = Medir(() => contexto.GerarAlertasAutomaticos().Executar(hoje));
            return $"{primeira} e {segunda} · {contexto.RepositorioAlerta.Listar().Count} alertas";
        });

        etapa($"Volume: ciclo completo da tela (limite {LimiteCicloTela.TotalSeconds:0} s)", () =>
        {
            var tela = new TelaPrincipal(contexto, () => { });
            try
            {
                var relogio = Stopwatch.StartNew();
                tela.AtualizarTudo();
                relogio.Stop();
                return relogio.Elapsed <= LimiteCicloTela
                    ? $"{relogio.Elapsed.TotalMilliseconds:0} ms"
                    : throw new InvalidOperationException($"o ciclo levou {relogio.Elapsed.TotalSeconds:0.0} s (limite {LimiteCicloTela.TotalSeconds:0} s)");
            }
            finally
            {
                tela.Encerrar();
            }
        });

        etapa("Volume: busca geral (índice + consulta)", () =>
        {
            Aplicacao.CasoDeUso.IndiceBusca? indice = null;
            var indexar = Medir(() => indice = contexto.BuscaGeral().Indexar());
            var resultados = 0;
            var consultar = Medir(() => resultados = indice!.Buscar("bem 42").Count);
            return $"índice {indexar}, consulta {consultar} ({resultados} resultados)";
        });

        etapa("Volume: exportação JSON e impressão digital da sincronização", () =>
        {
            var tamanho = 0;
            var exportar = Medir(() => tamanho = contexto.ExportarDados().Executar().Length);
            var impressao = Medir(() => contexto.ExportarDados().ImpressaoDigital());
            return $"exportação {exportar} ({tamanho / 1024} KB), impressão {impressao}";
        });

        var servico = new ServicoBackupLocal();
        string? pastaBackup = null;
        etapa("Volume: backup em pasta", () =>
        {
            var tempo = Medir(() => pastaBackup = servico.ExecutarBackup(contexto.ArquivoBanco, contexto.DiretorioDocumentos, Path.Combine(pasta, "volume-destino")));
            return $"{tempo} · {Tamanho(pastaBackup!)}";
        });

        etapa("Volume: arquivo criptografado (o mesmo da sincronização)", () =>
        {
            var arquivo = Path.Combine(pasta, "volume-sincronizacao" + ArquivoProtegido.Extensao);
            var tempo = Medir(() => servico.GerarArquivo(contexto.ArquivoBanco, contexto.DiretorioDocumentos, arquivo, "senha-do-autoteste"));
            return $"{tempo} · {Tamanho(arquivo)}";
        });

        etapa("Volume: testar o backup", () =>
        {
            Aplicacao.Servico.ResultadoTesteBackup? resultado = null;
            var tempo = Medir(() => resultado = servico.TestarBackup(pastaBackup!, contexto.DiretorioDocumentos, null));
            return resultado!.Completo && resultado.Documentos == Documentos
                ? $"{tempo} · {resultado.Documentos} documentos conferidos"
                : throw new InvalidOperationException($"backup incompleto: {resultado.DocumentosSemArquivo} documento(s) sem arquivo");
        });
    }

    private static void Semear(ContextoAplicacao contexto, int megabytesAnexos)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var aleatorio = new Random(42);
        var tipos = new[] { TipoBem.Eletronico, TipoBem.Eletrodomestico, TipoBem.Movel, TipoBem.Ferramenta, TipoBem.Veiculo, TipoBem.Outro };
        var bens = Enumerable.Range(1, Bens).Select(i =>
        {
            var bem = Bem.Criar(new NomeBem($"Bem {i}"), tipos[i % tipos.Length], hoje.AddDays(-aleatorio.Next(30, 3650)), Dinheiro.De(aleatorio.Next(100, 50_000), "BRL"));
            bem.AtualizarInventario(new InventarioBem($"Marca {i % 37}", $"Modelo {i}", $"SN-{i:D6}", "Loja", $"Cômodo {i % 9}"));
            return bem;
        }).ToList();
        bens.ForEach(contexto.RepositorioBem.Salvar);

        var veiculos = bens.Where(b => b.Tipo == TipoBem.Veiculo).Take(20).ToList();
        foreach (var veiculo in veiculos)
        {
            contexto.RepositorioVeiculo.Salvar(Veiculo.Criar(veiculo.Id, new DadosVeiculo($"ABC{aleatorio.Next(1000, 9999)}", "Marca", "Modelo", 2020, 2021, "Prata",
                null, null, TipoCombustivel.Flex, 10_000, 20_000, hoje.AddMonths(3), hoje.AddMonths(6), "Seguradora", "AP-1", hoje.AddMonths(8))));
        }

        contexto.RepositorioDespesa.SalvarTodos(Enumerable.Range(1, Despesas).Select(i =>
        {
            var despesa = Despesa.Registrar(bens[i % bens.Count].Id, i % 3 == 0 ? TipoDespesa.Financiamento : TipoDespesa.Energia, $"Conta {i}",
                hoje.AddDays(aleatorio.Next(-400, 120)), Dinheiro.De(aleatorio.Next(50, 3_000), "BRL"));
            if (despesa.Vencimento < hoje && i % 4 != 0)
            {
                despesa.Pagar(despesa.Vencimento);
            }

            return despesa;
        }).ToList());

        for (var i = 0; i < Garantias; i++)
        {
            var inicio = hoje.AddDays(-aleatorio.Next(0, 900));
            contexto.RepositorioGarantia.Salvar(Garantia.Criar(bens[i % bens.Count].Id, TipoGarantia.Fabricante, $"Fabricante {i}", inicio, inicio.AddYears(1)));
        }

        for (var i = 0; i < Manutencoes; i++)
        {
            contexto.RepositorioManutencao.Salvar(Manutencao.Agendar(bens[i % bens.Count].Id, TipoManutencao.Preventiva, $"Revisão {i}", hoje.AddDays(aleatorio.Next(-200, 200))));
        }

        for (var i = 0; i < Abastecimentos && veiculos.Count > 0; i++)
        {
            var veiculo = veiculos[i % veiculos.Count];
            contexto.RepositorioAbastecimento.Salvar(Abastecimento.Registrar(veiculo.Id, hoje.AddDays(-(Abastecimentos - i) / veiculos.Count), 10_000 + i * 50 / veiculos.Count,
                40m, Dinheiro.De(240m, "BRL"), TipoCombustivel.Flex, true, "Posto"));
        }

        Directory.CreateDirectory(contexto.DiretorioDocumentos);
        var bytesPorArquivo = Math.Max(1, megabytesAnexos) * 1024L * 1024L / Documentos;
        var conteudo = new byte[bytesPorArquivo];
        for (var i = 0; i < Documentos; i++)
        {
            aleatorio.NextBytes(conteudo);
            var arquivo = Path.Combine(contexto.DiretorioDocumentos, $"{Guid.NewGuid()}-nota-{i}.pdf");
            File.WriteAllBytes(arquivo, conteudo);
            contexto.RepositorioDocumento.Salvar(Documento.Registrar(bens[i % bens.Count].Id, TipoDocumento.NotaFiscal, new NomeDocumento($"Nota {i}"),
                new CaminhoDocumentoLocal(arquivo), hoje));
        }

        // Alertas manuais também crescem com o uso.
        contexto.RepositorioAlerta.SalvarTodos(bens.Take(200).Select(b => Alerta.Criar(b.Id, TipoAlerta.ManutencaoProxima, SeveridadeAlerta.Informativo,
            "Lembrete manual", hoje, hoje.AddDays(10))).ToList());
    }

    private static string Medir(Action acao)
    {
        var relogio = Stopwatch.StartNew();
        acao();
        return relogio.Elapsed.TotalSeconds >= 1 ? $"{relogio.Elapsed.TotalSeconds:0.0} s" : $"{relogio.Elapsed.TotalMilliseconds:0} ms";
    }

    private static string Tamanho(string caminho)
    {
        var bytes = File.Exists(caminho)
            ? new FileInfo(caminho).Length
            : Directory.EnumerateFiles(caminho, "*", SearchOption.AllDirectories).Sum(arquivo => new FileInfo(arquivo).Length);
        return $"{bytes / (1024.0 * 1024.0):0.0} MB";
    }
}
