using Assetly.Aplicacao.CasoDeUso;
using Assetly.Aplicacao.Dados;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Despesa;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Imovel;
using Assetly.Dominio.Veiculo;
using Assetly.Testes.Dominio;
using Shouldly;

namespace Assetly.Testes.Aplicacao;

public class VeiculoImovelCasosUsoTests
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Now);

    private readonly RepositorioBemMemoria _bens = new();
    private readonly RepositorioVeiculoMemoria _veiculos = new();
    private readonly RepositorioImovelMemoria _imoveis = new();
    private readonly RepositorioAbastecimentoMemoria _abastecimentos = new();
    private readonly RepositorioDespesaMemoria _despesas = new();
    private readonly RepositorioDocumentoMemoria _documentos = new();
    private readonly RepositorioAlertaMemoria _alertas = new();

    private GerarAlertasAutomaticos Gerador() =>
        new(_bens, new RepositorioGarantiaMemoria(), new RepositorioManutencaoMemoria(), _documentos, _alertas,
            new RepositorioSaudeMemoria(), _veiculos, _imoveis, _despesas);

    [Fact]
    public void DeveCadastrarVeiculoCriandoOBemComDadosDeCompra()
    {
        var veiculo = new CadastrarVeiculo(_bens, _veiculos).Executar(new CadastrarVeiculoComando(
            null, null, new DateOnly(2021, 3, 10), 98000m, "BRL", VeiculoImovelTests.Ficha()));

        var bem = _bens.BuscarPorId(veiculo.BemId)!;
        bem.Nome.Valor.ShouldBe("Toyota Corolla");
        bem.Tipo.ShouldBe(TipoBem.Veiculo);
        bem.PrecoCompra!.Valor.ShouldBe(98000m);
        _veiculos.Listar().ShouldBe([veiculo]);
    }

    [Fact]
    public void DeveVincularFichaABemExistenteDoTipoCertoUmaUnicaVez()
    {
        var carro = new CadastrarBem(_bens).Executar(new CadastrarBemComando("Meu carro", TipoBem.Veiculo, null, null, null, null));
        var sofa = new CadastrarBem(_bens).Executar(new CadastrarBemComando("Sofá", TipoBem.Movel, null, null, null, null));
        var cadastro = new CadastrarVeiculo(_bens, _veiculos);

        cadastro.Executar(new CadastrarVeiculoComando(carro.Id, null, null, null, null, VeiculoImovelTests.Ficha()));

        Should.Throw<InvalidOperationException>(() => cadastro.Executar(new CadastrarVeiculoComando(carro.Id, null, null, null, null, VeiculoImovelTests.Ficha())));
        Should.Throw<ArgumentException>(() => cadastro.Executar(new CadastrarVeiculoComando(sofa.Id, null, null, null, null, VeiculoImovelTests.Ficha())));
        _bens.Listar().Count.ShouldBe(2);
    }

    [Fact]
    public void FichaInvalidaNaoDeixaBemOrfao()
    {
        Should.Throw<ArgumentException>(() => new CadastrarVeiculo(_bens, _veiculos).Executar(new CadastrarVeiculoComando(
            null, "Carro", null, null, null, VeiculoImovelTests.Ficha(placa: "XYZ"))));

        _bens.Listar().ShouldBeEmpty();
    }

    [Fact]
    public void AbastecimentoAvancaQuilometragemDoVeiculo()
    {
        var veiculo = new CadastrarVeiculo(_bens, _veiculos).Executar(new CadastrarVeiculoComando(null, "Carro", null, null, null, VeiculoImovelTests.Ficha(km: 50000)));
        var registrar = new RegistrarAbastecimento(_veiculos, _abastecimentos);

        registrar.Executar(new RegistrarAbastecimentoComando(veiculo.BemId, Hoje, 50450, 40m, 240m, "BRL", TipoCombustivel.Gasolina, true, "Posto Demo"));
        veiculo.Quilometragem.ShouldBe(50450);

        registrar.Executar(new RegistrarAbastecimentoComando(veiculo.BemId, Hoje, 50100, 10m, 60m, "BRL", TipoCombustivel.Gasolina, false, null));
        veiculo.Quilometragem.ShouldBe(50450);
        _abastecimentos.ListarPorBem(veiculo.BemId).Count.ShouldBe(2);

        var semFicha = new CadastrarBem(_bens).Executar(new CadastrarBemComando("Bicicleta", TipoBem.Veiculo, null, null, null, null));
        Should.Throw<Assetly.Aplicacao.Excecao.RecursoNaoEncontradoExcecao>(() => registrar.Executar(new RegistrarAbastecimentoComando(
            semFicha.Id, Hoje, 10, 1m, 6m, "BRL", TipoCombustivel.Gasolina, true, null)));
    }

    [Fact]
    public void DeveRegistrarDespesaParceladaEPagar()
    {
        var bem = new CadastrarBem(_bens).Executar(new CadastrarBemComando("Carro", TipoBem.Veiculo, null, null, null, null));

        var parcelas = new RegistrarDespesa(_bens, _despesas).Executar(new RegistrarDespesaComando(
            bem.Id, TipoDespesa.Ipva, "IPVA 2027", new DateOnly(2027, 1, 31), 500m, "BRL", 3));

        parcelas.Select(p => p.Descricao).ShouldBe(["IPVA 2027 (1/3)", "IPVA 2027 (2/3)", "IPVA 2027 (3/3)"]);
        parcelas.Select(p => p.Vencimento).ShouldBe([new DateOnly(2027, 1, 31), new DateOnly(2027, 2, 28), new DateOnly(2027, 3, 31)]);

        new PagarDespesa(_despesas).Executar(parcelas[0].Id, Hoje);
        parcelas[0].PagaEm.ShouldBe(Hoje);
        new DesfazerPagamentoDespesa(_despesas).Executar(parcelas[0].Id);
        new ExcluirDespesa(_despesas).Executar(parcelas[2].Id);
        _despesas.Listar().Count.ShouldBe(2);

        Should.Throw<ArgumentException>(() => new RegistrarDespesa(_bens, _despesas).Executar(new RegistrarDespesaComando(
            bem.Id, TipoDespesa.Multa, "Multa", Hoje, 100m, "BRL", 0)));
    }

    [Fact]
    public void DeveGerarEResolverAlertasDeVeiculo()
    {
        var ficha = VeiculoImovelTests.Ficha(km: 49500) with
        {
            ProximaRevisaoKm = 50000,
            VencimentoLicenciamento = Hoje.AddDays(10),
            VencimentoSeguro = Hoje.AddDays(-2)
        };
        var veiculo = new CadastrarVeiculo(_bens, _veiculos).Executar(new CadastrarVeiculoComando(null, "Carro", null, null, null, ficha));
        _documentos.Salvar(Documento.Registrar(veiculo.BemId, TipoDocumento.NotaFiscal, new NomeDocumento("NF"), new CaminhoDocumentoLocal("nf.pdf"), Hoje));

        var gerados = Gerador().Executar(Hoje);

        gerados.Select(a => (a.Tipo, a.Severidade)).ShouldBe(
            [
                (TipoAlerta.LicenciamentoVencendo, SeveridadeAlerta.Aviso),
                (TipoAlerta.SeguroVencido, SeveridadeAlerta.Critico),
                (TipoAlerta.RevisaoVeiculo, SeveridadeAlerta.Aviso)
            ],
            ignoreOrder: true);

        // Revisão passa do km previsto: o aviso "próxima" é resolvido e nasce o crítico "atrasada".
        new AtualizarQuilometragem(_veiculos).Executar(veiculo.BemId, 50100);
        var segunda = Gerador().Executar(Hoje);
        segunda.Single().Mensagem.ShouldStartWith("Revisão atrasada");
        _alertas.Listar().Single(a => a.Mensagem.StartsWith("Revisão próxima")).Status.ShouldBe(StatusAlerta.Resolvido);

        // Licenciamento e seguro renovados, revisão feita: tudo se resolve sozinho.
        new EditarVeiculo(_veiculos).Executar(new EditarVeiculoComando(veiculo.BemId, veiculo.Dados() with
        {
            ProximaRevisaoKm = 60000,
            VencimentoLicenciamento = Hoje.AddYears(1),
            VencimentoSeguro = Hoje.AddYears(1)
        }));
        Gerador().Executar(Hoje).ShouldBeEmpty();
        _alertas.Listar().ShouldAllBe(a => a.Status == StatusAlerta.Resolvido);
    }

    [Fact]
    public void DeveGerarAlertasDeImovelEContasSemExigirNotaFiscal()
    {
        var imovel = new CadastrarImovel(_bens, _imoveis).Executar(new CadastrarImovelComando(null, null, null, null, null,
            VeiculoImovelTests.FichaImovel(situacao: SituacaoImovel.AlugadoParaTerceiros, fimContrato: Hoje.AddDays(45), vencimentoSeguro: Hoje.AddDays(20))));
        _bens.BuscarPorId(imovel.BemId)!.Nome.Valor.ShouldBe("Av. Paulista, 1000");
        var despesas = new RegistrarDespesa(_bens, _despesas);
        var vencida = despesas.Executar(new RegistrarDespesaComando(imovel.BemId, TipoDespesa.Condominio, "Condomínio", Hoje.AddDays(-3), 850m, "BRL")).Single();
        despesas.Executar(new RegistrarDespesaComando(imovel.BemId, TipoDespesa.Energia, "Energia", Hoje.AddDays(4), 210m, "BRL"));
        despesas.Executar(new RegistrarDespesaComando(imovel.BemId, TipoDespesa.Iptu, "IPTU", Hoje.AddDays(40), 300m, "BRL"));

        var gerados = Gerador().Executar(Hoje);

        gerados.Select(a => a.Tipo).ShouldBe(
            [TipoAlerta.SeguroVencendo, TipoAlerta.ContratoAluguel, TipoAlerta.ContaVencida, TipoAlerta.ContaVencendo],
            ignoreOrder: true);
        gerados.ShouldNotContain(a => a.Tipo == TipoAlerta.DocumentoAusente);

        new PagarDespesa(_despesas).Executar(vencida.Id, Hoje);
        Gerador().Executar(Hoje);
        _alertas.Listar().Single(a => a.Tipo == TipoAlerta.ContaVencida).Status.ShouldBe(StatusAlerta.Resolvido);
    }
}
