using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Shouldly;

namespace Assetly.Testes.Dominio;

public class BemTests
{
    [Fact]
    public void DeveCriarBemAtivoComNomeNormalizado()
    {
        var bem = Bem.Criar(new NomeBem("  Notebook  "), TipoBem.Eletronico);

        bem.Nome.Valor.ShouldBe("Notebook");
        bem.Tipo.ShouldBe(TipoBem.Eletronico);
        bem.Status.ShouldBe(StatusBem.Ativo);
        bem.CompradoEm.ShouldBeNull();
    }

    [Fact]
    public void DeveRejeitarNomeEmBranco()
    {
        var excecao = Should.Throw<ArgumentException>(() => new NomeBem("   "));
        excecao.Message.ShouldContain("nome do bem");
    }

    [Fact]
    public void DeveRejeitarDataDeCompraFutura()
    {
        var futuro = DateOnly.FromDateTime(DateTime.Now).AddDays(1);
        var preco = Dinheiro.De(100.00m, "USD");

        var excecao = Should.Throw<ArgumentException>(
            () => Bem.Criar(new NomeBem("Câmera"), TipoBem.Eletronico, futuro, preco));
        excecao.Message.ShouldContain("data de compra");
    }

    [Fact]
    public void DevePermitirDesarquivarBemMasNaoColocarArquivadoEmManutencao()
    {
        var bem = Bem.Criar(new NomeBem("Furadeira"), TipoBem.Ferramenta);
        bem.Arquivar();

        Should.Throw<InvalidOperationException>(() => bem.MarcarEmManutencao());
        bem.Ativar();

        bem.Status.ShouldBe(StatusBem.Ativo);
    }

    [Fact]
    public void DeveImpedirReativarBemDescartado()
    {
        var bem = Bem.Criar(new NomeBem("Bicicleta"), TipoBem.Outro);

        bem.Descartar();

        var excecao = Should.Throw<InvalidOperationException>(() => bem.Ativar());
        excecao.Message.ShouldContain("descartado");
    }
}
