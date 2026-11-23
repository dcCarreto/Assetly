using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Despesa;
using Assetly.Dominio.Imovel;
using Assetly.Dominio.Veiculo;
using Assetly.Infraestrutura.Banco;
using Assetly.Infraestrutura.Repositorio;
using Assetly.Testes.Dominio;
using Shouldly;

namespace Assetly.Testes.Infraestrutura;

public class RepositoriosVeiculoImovelSqliteTests
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Now);

    [Fact]
    public void DeveSalvarERecuperarVeiculoAbastecimentosEDespesas()
    {
        var fabrica = Banco();
        var bem = Bem.Criar(new NomeBem("Corolla"), TipoBem.Veiculo);
        new RepositorioBemSqlite(fabrica).Salvar(bem);
        var repositorioVeiculo = new RepositorioVeiculoSqlite(fabrica);
        var ficha = VeiculoImovelTests.Ficha(renavam: "12345678901", chassi: "9BWZZZ377VT004251") with
        {
            ProximaRevisaoKm = 60000,
            ProximaRevisaoEm = Hoje.AddMonths(3),
            VencimentoLicenciamento = Hoje.AddMonths(2),
            Seguradora = "Seguradora Demo",
            Apolice = "AP-1",
            VencimentoSeguro = Hoje.AddMonths(8)
        };
        repositorioVeiculo.Salvar(Veiculo.Criar(bem.Id, ficha));

        var recuperado = repositorioVeiculo.BuscarPorBem(bem.Id)!;
        recuperado.Dados().ShouldBe(Veiculo.Criar(bem.Id, ficha).Dados());

        var repositorioAbastecimento = new RepositorioAbastecimentoSqlite(fabrica);
        var abastecimento = Abastecimento.Registrar(bem.Id, Hoje, 50500, 42.357m, Dinheiro.De(254.10m, "BRL"), TipoCombustivel.Etanol, true, "Posto");
        repositorioAbastecimento.Salvar(abastecimento);
        var abastecimentoLido = repositorioAbastecimento.ListarPorBem(bem.Id).Single();
        abastecimentoLido.Litros.ShouldBe(42.357m);
        abastecimentoLido.Valor.Valor.ShouldBe(254.10m);
        abastecimentoLido.TanqueCheio.ShouldBeTrue();
        abastecimentoLido.Combustivel.ShouldBe(TipoCombustivel.Etanol);

        var repositorioDespesa = new RepositorioDespesaSqlite(fabrica);
        var despesa = Despesa.Registrar(bem.Id, TipoDespesa.SeguroVeiculo, "Seguro anual", Hoje.AddDays(10), Dinheiro.De(2400, "BRL"));
        despesa.Pagar(Hoje);
        repositorioDespesa.Salvar(despesa);
        var despesaLida = repositorioDespesa.BuscarPorId(despesa.Id)!;
        despesaLida.Tipo.ShouldBe(TipoDespesa.SeguroVeiculo);
        despesaLida.PagaEm.ShouldBe(Hoje);

        new RepositorioBemSqlite(fabrica).Excluir(bem.Id);
        repositorioVeiculo.Listar().ShouldBeEmpty();
        repositorioAbastecimento.Listar().ShouldBeEmpty();
        repositorioDespesa.Listar().ShouldBeEmpty();
    }

    [Fact]
    public void DeveSalvarERecuperarImovel()
    {
        var fabrica = Banco();
        var bem = Bem.Criar(new NomeBem("Apartamento"), TipoBem.Imovel);
        new RepositorioBemSqlite(fabrica).Salvar(bem);
        var repositorio = new RepositorioImovelSqlite(fabrica);
        var ficha = VeiculoImovelTests.FichaImovel(situacao: SituacaoImovel.AlugadoParaTerceiros, fimContrato: Hoje.AddYears(1), vencimentoSeguro: Hoje.AddMonths(6));

        repositorio.Salvar(Imovel.Criar(bem.Id, ficha));
        var lido = repositorio.BuscarPorBem(bem.Id)!;

        lido.Dados().ShouldBe(Imovel.Criar(bem.Id, ficha).Dados());
        lido.ValorAluguel!.Valor.ShouldBe(3200m);
        lido.AreaM2.ShouldBe(72.5m);

        repositorio.Excluir(bem.Id);
        repositorio.Listar().ShouldBeEmpty();
    }

    private static FabricaConexaoSqlite Banco()
    {
        var banco = new BancoDadosAssetly(new FabricaConexaoSqlite(Path.Combine(Path.GetTempPath(), $"assetly-{Guid.NewGuid()}", "assetly.sqlite3")));
        banco.Inicializar();
        return banco.FabricaConexao;
    }
}
