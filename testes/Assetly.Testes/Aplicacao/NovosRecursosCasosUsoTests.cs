using System.Text.Json;
using Assetly.Aplicacao.Atualizacao;
using Assetly.Aplicacao.CasoDeUso;
using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Leitura;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Testes.Dominio;
using Shouldly;

namespace Assetly.Testes.Aplicacao;

public class NovosRecursosCasosUsoTests
{
    private static readonly DateOnly Hoje = new(2026, 11, 23);

    // ---------- Versão por data (sem número de versão) ----------

    [Theory]
    [InlineData("2026.11.23", 2026, 11, 23, 1)]
    [InlineData("v2026.11.23.3", 2026, 11, 23, 3)]
    [InlineData("2026.11.23+abc123", 2026, 11, 23, 1)]
    public void VersaoPorDataDeveLerTagsDeRelease(string texto, int ano, int mes, int dia, int sequencia) =>
        VersaoPorData.TentarLer(texto).ShouldBe(new VersaoPorData(new DateOnly(ano, mes, dia), sequencia));

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("0.0.0")]
    [InlineData("2026.13.01")]
    [InlineData("2026.02.30")]
    [InlineData("2026.11.23.0")]
    [InlineData("desenvolvimento")]
    [InlineData(null)]
    public void VersaoPorDataDeveRecusarFormatosQueNaoSaoData(string? texto) => VersaoPorData.TentarLer(texto).ShouldBeNull();

    [Fact]
    public void VersaoPorDataDeveNumerarReleasesDoMesmoDiaEOrdenar()
    {
        VersaoPorData.Proxima(Hoje, ["2026.11.22", "outra"]).Identificador.ShouldBe("2026.11.23");
        var segunda = VersaoPorData.Proxima(Hoje, ["2026.11.23", "2026.11.22"]);
        segunda.Identificador.ShouldBe("2026.11.23.2");
        segunda.Exibicao.ShouldBe("23/11/2026 (2ª do dia)");
        VersaoPorData.Proxima(Hoje, ["2026.11.23", "2026.11.23.2"]).Sequencia.ShouldBe(3);

        (segunda > VersaoPorData.TentarLer("2026.11.23")!).ShouldBeTrue();
        (VersaoPorData.TentarLer("2026.12.01")! > segunda).ShouldBeTrue();
        new VersaoPorData(Hoje, 1).Exibicao.ShouldBe("23/11/2026");
    }

    // ---------- Leitura de nota fiscal (QR Code e OCR) ----------

    [Fact]
    public void DeveInterpretarQrCodeV2DaNfce()
    {
        var chave = NotaFamiliaDepreciacaoTests.ChaveValida();
        var url = $"https://www.fazenda.pr.gov.br/nfce/qrcode?p={chave}|2|1|1|ABCDEF";

        var dados = LeituraNotaFiscal.InterpretarQrCode(url, Hoje);

        dados.Chave!.Valor.ShouldBe(chave);
        dados.UrlConsulta.ShouldBe(url);
        dados.CnpjEmitente.ShouldBe("12.345.678/0001-95");
        dados.TemAlgo.ShouldBeTrue();
    }

    [Fact]
    public void DeveInterpretarQrCodeV2EmContingenciaComDiaEValor()
    {
        var chave = NotaFamiliaDepreciacaoTests.ChaveValida();

        var dados = LeituraNotaFiscal.InterpretarQrCode($"https://sefaz/qrcode?p={chave}|2|1|15|149.90|646967|1|HASH", Hoje);

        dados.Emissao.ShouldBe(new DateOnly(2026, 11, 15));
        dados.ValorTotal.ShouldBe(149.90m);
    }

    [Fact]
    public void DeveInterpretarQrCodeV1()
    {
        var chave = NotaFamiliaDepreciacaoTests.ChaveValida();
        var dataHex = Convert.ToHexString("2026-11-10T14:30:00-03:00"u8.ToArray());

        var dados = LeituraNotaFiscal.InterpretarQrCode($"https://sefaz/nfce?chNFe={chave}&nVersao=100&tpAmb=1&dhEmi={dataHex}&vNF=89.50&vICMS=0", Hoje);

        dados.Chave!.Valor.ShouldBe(chave);
        dados.Emissao.ShouldBe(new DateOnly(2026, 11, 10));
        dados.ValorTotal.ShouldBe(89.50m);
    }

    [Fact]
    public void DeveExtrairDadosDoTextoReconhecidoPorOcr()
    {
        var texto = """
            LOJA EXEMPLO LTDA
            CNPJ: 12.345.678/0001-95
            NOTA FISCAL DE CONSUMIDOR ELETRONICA
            Qtde total de itens 3
            Valor total R$
            1.234,56
            Emissao: 10/11/2026 14:30:12
            """;

        var dados = LeituraNotaFiscal.InterpretarTexto(texto, Hoje);

        dados.CnpjEmitente.ShouldBe("12.345.678/0001-95");
        dados.Emissao.ShouldBe(new DateOnly(2026, 11, 10));
        dados.ValorTotal.ShouldBe(1234.56m);
    }

    [Fact]
    public void LeituraDeTextoVazioOuSemDadosNaoInventaNada()
    {
        LeituraNotaFiscal.InterpretarTexto("", Hoje).TemAlgo.ShouldBeFalse();
        LeituraNotaFiscal.InterpretarTexto("texto qualquer sem nota", Hoje).TemAlgo.ShouldBeFalse();
        LeituraNotaFiscal.InterpretarQrCode("https://exemplo.com", Hoje).TemAlgo.ShouldBeFalse();
        // Data no futuro é descartada (provável erro de leitura).
        LeituraNotaFiscal.InterpretarTexto("EMISSAO 10/11/2030", Hoje).Emissao.ShouldBeNull();
    }

    // ---------- Modo família ----------

    [Fact]
    public void ResponsavelPrecisaSerUmaPessoaCadastrada()
    {
        var repositorio = new RepositorioBemMemoria();
        var pessoa = new CadastrarBem(repositorio).Executar(new CadastrarBemComando("Ana", TipoBem.Pessoa, null, null, null, null));
        var carro = new CadastrarBem(repositorio).Executar(new CadastrarBemComando("Carro", TipoBem.Veiculo, null, null, null, null));

        var notebook = new CadastrarBem(repositorio).Executar(new CadastrarBemComando("Notebook", TipoBem.Eletronico, null, null, null, null, pessoa.Id));

        notebook.ResponsavelId.ShouldBe(pessoa.Id);
        Should.Throw<ArgumentException>(() => new CadastrarBem(repositorio).Executar(new CadastrarBemComando("Celular", TipoBem.Eletronico, null, null, null, null, carro.Id)));
        Should.Throw<Exception>(() => new CadastrarBem(repositorio).Executar(new CadastrarBemComando("Tablet", TipoBem.Eletronico, null, null, null, null, BemId.Novo())));
        new EditarBem(repositorio).Executar(new EditarBemComando(notebook.Id, "Notebook", TipoBem.Eletronico, null, null, null, null, null))
            .ResponsavelId.ShouldBeNull();
    }

    // ---------- Patrimônio ----------

    [Fact]
    public void PatrimonioDeveSomarBensEmUsoEmReaisPorTipoEResponsavel()
    {
        var repositorio = new RepositorioBemMemoria();
        var ana = Bem.Criar(new NomeBem("Ana"), TipoBem.Pessoa);
        var notebook = Bem.Criar(new NomeBem("Notebook"), TipoBem.Eletronico, Hoje.AddYears(-1), Dinheiro.De(5_000m, "BRL"));
        notebook.DefinirResponsavel(ana.Id);
        var casa = Bem.Criar(new NomeBem("Casa"), TipoBem.Imovel, Hoje.AddYears(-3), Dinheiro.De(300_000m, "BRL"));
        var importado = Bem.Criar(new NomeBem("Câmera importada"), TipoBem.Eletronico, Hoje.AddYears(-1), Dinheiro.De(1_000m, "USD"));
        var semPreco = Bem.Criar(new NomeBem("Cadeira"), TipoBem.Movel);
        var descartado = Bem.Criar(new NomeBem("Velho"), TipoBem.Eletronico, Hoje.AddYears(-1), Dinheiro.De(9_999m, "BRL"));
        descartado.Descartar();
        foreach (var bem in new[] { ana, notebook, casa, importado, semPreco, descartado })
        {
            repositorio.Salvar(bem);
        }

        var resumo = new CalcularPatrimonio(repositorio).Executar(Hoje);

        resumo.ValorCompra.ShouldBe(305_000m);
        resumo.ValorAtual.ShouldBeInRange(303_990m, 304_010m);
        resumo.Depreciacao.ShouldBeInRange(990m, 1_010m);
        resumo.Itens.Count.ShouldBe(2);
        resumo.SemPreco.ShouldBe(1);
        resumo.EmOutraMoeda.ShouldBe(1);
        resumo.PorTipo.Select(p => p.Tipo).ShouldBe([TipoBem.Imovel, TipoBem.Eletronico]);
        resumo.PorResponsavel.ShouldContain(g => g.Rotulo == "Ana" && g.Quantidade == 1);
        resumo.Evolucao.Count.ShouldBe(6);
        resumo.Evolucao[^1].Ano.ShouldBe(2026);
        resumo.Evolucao[^1].Valor.ShouldBe(resumo.ValorAtual);
    }

    // ---------- Exportação JSON ----------

    [Fact]
    public void ExportacaoJsonDeveTrazerTodasAsColecoesEImpressaoEstavel()
    {
        var fonte = FonteMemoria();
        var notebook = Bem.Criar(new NomeBem("Notebook"), TipoBem.Eletronico, Hoje, Dinheiro.De(5_000m, "BRL"));
        fonte.Bens.Salvar(notebook);
        var exportar = new ExportarDados(fonte);

        using var json = JsonDocument.Parse(exportar.Executar());
        var dados = json.RootElement.GetProperty("dados");
        dados.GetProperty("bens").GetArrayLength().ShouldBe(1);
        foreach (var colecao in new[] { "garantias", "manutencoes", "documentos", "alertas", "convenios", "gastosSaude", "veiculos", "abastecimentos", "imoveis", "despesas" })
        {
            dados.TryGetProperty(colecao, out _).ShouldBeTrue(colecao);
        }

        var impressao = exportar.ImpressaoDigital();
        impressao.Length.ShouldBe(64);
        exportar.ImpressaoDigital().ShouldBe(impressao);

        // Alertas automáticos são recalculados em cada computador: não podem gerar diferença na sincronização.
        fonte.Alertas.Salvar(Alerta.CriarAutomatico(notebook.Id, TipoAlerta.DocumentoAusente, SeveridadeAlerta.Aviso, "sem nota", Hoje, null));
        exportar.ImpressaoDigital().ShouldBe(impressao);

        notebook.AtualizarObservacoes("mudou");
        fonte.Bens.Salvar(notebook);
        exportar.ImpressaoDigital().ShouldNotBe(impressao);
    }

    internal static FonteDados FonteMemoria() => new(
        new RepositorioBemMemoria(), new RepositorioGarantiaMemoria(), new RepositorioManutencaoMemoria(), new RepositorioDocumentoMemoria(),
        new RepositorioAlertaMemoria(), new RepositorioSaudeMemoria(), new RepositorioGastoSaudeMemoria(), new RepositorioVeiculoMemoria(),
        new RepositorioAbastecimentoMemoria(), new RepositorioImovelMemoria(), new RepositorioDespesaMemoria());
}
