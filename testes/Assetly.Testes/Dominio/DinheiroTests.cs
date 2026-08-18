using Assetly.Dominio.Compartilhado;
using Shouldly;

namespace Assetly.Testes.Dominio;

public class DinheiroTests
{
    [Fact]
    public void DeveNormalizarEscalaDoValor()
    {
        var dinheiro = Dinheiro.De(1999.995m, "USD");

        dinheiro.Valor.ShouldBe(2000.00m);
        dinheiro.CodigoMoeda.ShouldBe("USD");
    }

    [Fact]
    public void DeveRejeitarValorNegativo()
    {
        var excecao = Should.Throw<ArgumentException>(() => Dinheiro.De(-1.00m, "USD"));
        excecao.Message.ShouldContain("valor");
    }
}
