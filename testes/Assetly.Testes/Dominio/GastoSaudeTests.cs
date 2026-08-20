using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Saude;
using Shouldly;

namespace Assetly.Testes.Dominio;

public class GastoSaudeTests
{
    [Fact]
    public void DeveRegistrarGastoComValor()
    {
        var gasto = GastoSaude.Registrar(
            BemId.Novo(),
            TipoGastoSaude.Farmacia,
            "Antibiótico",
            new DateOnly(2026, 3, 10),
            Dinheiro.De(89.90m, "BRL"));

        gasto.Tipo.ShouldBe(TipoGastoSaude.Farmacia);
        gasto.Valor.Valor.ShouldBe(89.90m);
    }

    [Fact]
    public void DeveRejeitarDescricaoEmBranco()
    {
        var excecao = Should.Throw<ArgumentException>(() => GastoSaude.Registrar(
            BemId.Novo(),
            TipoGastoSaude.Consulta,
            " ",
            new DateOnly(2026, 3, 10),
            Dinheiro.De(150m, "BRL")));
        excecao.Message.ShouldContain("descrição do gasto de saúde");
    }

    [Fact]
    public void DeveAlterarValor()
    {
        var gasto = GastoSaude.Registrar(
            BemId.Novo(),
            TipoGastoSaude.Exame,
            "Hemograma",
            new DateOnly(2026, 3, 10),
            Dinheiro.De(120m, "BRL"));

        gasto.AlterarValor(Dinheiro.De(135m, "BRL"));

        gasto.Valor.Valor.ShouldBe(135m);
    }
}
