using Assetly.Aplicacao.CasoDeUso;
using Assetly.Aplicacao.Dados;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Despesa;
using Assetly.Infraestrutura.Banco;
using Assetly.Infraestrutura.Repositorio;
using Shouldly;

namespace Assetly.Testes.Aplicacao;

public class InventarioPatrimonioRecorrenciaTests
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Now);

    [Fact]
    public void ValorInformadoSubstituiADepreciacaoAPartirDaAvaliacao()
    {
        var carro = Bem.Criar(new NomeBem("Carro"), TipoBem.Veiculo, Hoje.AddYears(-4), Dinheiro.De(100_000m, "BRL"));
        carro.InformarValorAtual(Dinheiro.De(70_000m, "BRL"), Hoje.AddMonths(-1));

        Depreciacao.ValorEstimado(carro, Hoje)!.Valor.ShouldBe(70_000m);
        // Antes da avaliação continua valendo a estimativa por depreciação (12% ao ano para veículo).
        Depreciacao.ValorEstimado(carro, Hoje.AddYears(-3))!.Valor.ShouldBeInRange(87_000m, 89_000m);

        Should.Throw<ArgumentException>(() => carro.InformarValorAtual(Dinheiro.De(10m, "USD"), Hoje));
    }

    [Fact]
    public void BemSemPrecoComValorInformadoEntraNoPatrimonio()
    {
        var heranca = Bem.Criar(new NomeBem("Relógio herdado"), TipoBem.Outro);
        heranca.InformarValorAtual(Dinheiro.De(3_000m, "BRL"), Hoje);

        Depreciacao.ContaNoPatrimonio(heranca).ShouldBeTrue();
        Depreciacao.ValorEstimado(heranca, Hoje)!.Valor.ShouldBe(3_000m);
    }

    [Fact]
    public void TaxaPropriaDeDepreciacaoValeSobreADoTipo()
    {
        var notebook = Bem.Criar(new NomeBem("Notebook"), TipoBem.Eletronico, Hoje.AddYears(-1), Dinheiro.De(10_000m, "BRL"));
        notebook.DefinirTaxaDepreciacao(0.50m);

        Depreciacao.ValorEstimado(notebook, Hoje)!.Valor.ShouldBeInRange(4_950m, 5_050m);
        Should.Throw<ArgumentException>(() => notebook.DefinirTaxaDepreciacao(1.5m));
    }

    [Fact]
    public void PatrimonioLiquidoDescontaFinanciamentosEmAberto()
    {
        var bens = new RepositorioBemMemoria();
        var despesas = new RepositorioDespesaMemoria();
        var casa = Bem.Criar(new NomeBem("Casa"), TipoBem.Imovel, Hoje.AddYears(-2), Dinheiro.De(400_000m, "BRL"));
        casa.InformarValorAtual(Dinheiro.De(500_000m, "BRL"), Hoje);
        bens.Salvar(casa);
        var paga = Despesa.Registrar(casa.Id, TipoDespesa.Financiamento, "Parcela 1", Hoje.AddMonths(-1), Dinheiro.De(2_000m, "BRL"));
        paga.Pagar(Hoje);
        despesas.Salvar(paga);
        despesas.Salvar(Despesa.Registrar(casa.Id, TipoDespesa.Financiamento, "Parcela 2", Hoje, Dinheiro.De(2_000m, "BRL")));
        despesas.Salvar(Despesa.Registrar(casa.Id, TipoDespesa.Financiamento, "Parcela 3", Hoje.AddMonths(1), Dinheiro.De(2_000m, "BRL")));
        despesas.Salvar(Despesa.Registrar(casa.Id, TipoDespesa.Iptu, "IPTU", Hoje, Dinheiro.De(900m, "BRL")));

        var resumo = new CalcularPatrimonio(bens, despesas).Executar(Hoje);

        resumo.ValorAtual.ShouldBe(500_000m);
        resumo.Dividas.ShouldBe(4_000m);
        resumo.ParcelasEmAberto.ShouldBe(2);
        resumo.PatrimonioLiquido.ShouldBe(496_000m);
    }

    [Fact]
    public void ContaRecorrenteGeraADoMesSeguinteUmaSoVez()
    {
        var bens = new RepositorioBemMemoria();
        var despesas = new RepositorioDespesaMemoria();
        var apartamento = Bem.Criar(new NomeBem("Apartamento"), TipoBem.Imovel);
        bens.Salvar(apartamento);
        var vencimento = new DateOnly(2026, 1, 31);

        var conta = new RegistrarDespesa(bens, despesas)
            .Executar(new RegistrarDespesaComando(apartamento.Id, TipoDespesa.Condominio, "Condomínio", vencimento, 800m, "BRL", Recorrente: true))
            .Single();
        new PagarDespesa(despesas).Executar(conta.Id, Hoje);
        new DesfazerPagamentoDespesa(despesas).Executar(conta.Id);
        new PagarDespesa(despesas).Executar(conta.Id, Hoje);

        var todas = despesas.Listar();
        todas.Count.ShouldBe(2);
        var proxima = todas.Single(d => !d.Id.Equals(conta.Id));
        proxima.Vencimento.ShouldBe(new DateOnly(2026, 2, 28));
        proxima.Recorrente.ShouldBeTrue();
        proxima.PagaEm.ShouldBeNull();
        proxima.Valor.Valor.ShouldBe(800m);

        Should.Throw<ArgumentException>(() => new RegistrarDespesa(bens, despesas).Executar(
            new RegistrarDespesaComando(apartamento.Id, TipoDespesa.Energia, "Luz", vencimento, 100m, "BRL", Repeticoes: 3, Recorrente: true)));
    }

    [Fact]
    public void ContaComumNaoGeraOutraAoSerPaga()
    {
        var bens = new RepositorioBemMemoria();
        var despesas = new RepositorioDespesaMemoria();
        var carro = Bem.Criar(new NomeBem("Carro"), TipoBem.Veiculo);
        bens.Salvar(carro);
        var ipva = new RegistrarDespesa(bens, despesas)
            .Executar(new RegistrarDespesaComando(carro.Id, TipoDespesa.Ipva, "IPVA", Hoje, 1_500m, "BRL")).Single();

        new PagarDespesa(despesas).Executar(ipva.Id, Hoje);

        despesas.Listar().Count.ShouldBe(1);
    }

    [Fact]
    public void BancoGuardaInventarioValorInformadoTaxaERecorrencia()
    {
        var banco = new BancoDadosAssetly(new FabricaConexaoSqlite(Path.Combine(Path.GetTempPath(), $"assetly-{Guid.NewGuid()}", "assetly.sqlite3")));
        banco.Inicializar();
        var repositorioBem = new RepositorioBemSqlite(banco.FabricaConexao);
        var tv = new CadastrarBem(repositorioBem).Executar(new CadastrarBemComando("TV", TipoBem.Eletronico, Hoje.AddYears(-1), 3_000m, "BRL", null,
            Inventario: new InventarioBem("Samsung", "QN55", "SN-123", "Loja X", "Sala"), ValorAtual: 2_000m, ValorAtualEm: Hoje, TaxaDepreciacao: 0.25m));

        var lido = repositorioBem.BuscarPorId(tv.Id)!;
        lido.Inventario.ShouldBe(new InventarioBem("Samsung", "QN55", "SN-123", "Loja X", "Sala"));
        lido.ValorInformado.ShouldBe(Dinheiro.De(2_000m, "BRL"));
        lido.ValorInformadoEm.ShouldBe(Hoje);
        lido.TaxaDepreciacao.ShouldBe(0.25m);

        var repositorioDespesa = new RepositorioDespesaSqlite(banco.FabricaConexao);
        var conta = Despesa.Registrar(tv.Id, TipoDespesa.Internet, "Internet", Hoje, Dinheiro.De(100m, "BRL"));
        conta.DefinirRecorrencia(true);
        repositorioDespesa.Salvar(conta);
        repositorioDespesa.BuscarPorId(conta.Id)!.Recorrente.ShouldBeTrue();
    }

    [Fact]
    public void CamposNovosVaziosNaoMudamAImpressaoDigitalDaSincronizacao()
    {
        var fonte = NovosRecursosCasosUsoTests.FonteMemoria();
        var bem = Bem.Criar(new NomeBem("Geladeira"), TipoBem.Eletrodomestico);
        fonte.Bens.Salvar(bem);
        var exportar = new ExportarDados(fonte);

        exportar.Executar().ShouldNotContain("numeroSerie");
        var antes = exportar.ImpressaoDigital();

        bem.AtualizarInventario(new InventarioBem(null, null, "ABC", null, null));
        exportar.Executar().ShouldContain("\"numeroSerie\": \"ABC\"");
        exportar.ImpressaoDigital().ShouldNotBe(antes);
    }
}
