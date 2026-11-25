using Assetly.Dominio.Preferencias;
using Shouldly;

namespace Assetly.Testes.Dominio;

public class ConfiguracaoPreferenciasTests
{
    private static readonly string[] TresModulos = ["Painel", "Bens", "Garantias"];

    [Fact]
    public void SincronizarModulosDeveDescartarTitulosDesconhecidosEAnexarNovos()
    {
        var configuracao = ConfiguracaoPreferencias.Restaurar(
            TemaVisual.Escuro, CorDestaque.Ciano, DensidadeTabela.Confortavel, IntervaloAtualizacao.Segundos5,
            ["Painel", "ModuloRemovido"], ["ModuloRemovido"], "ModuloRemovido");

        configuracao.SincronizarModulos(["Painel", "Bens", "Garantias"]);

        configuracao.OrdemModulos.ShouldBe(["Painel", "Bens", "Garantias"]);
        configuracao.ModulosOcultos.ShouldBeEmpty();
        configuracao.ModuloInicial.ShouldBeNull();
    }

    [Fact]
    public void DeveRejeitarOcultarUltimoModuloVisivel()
    {
        var configuracao = ConfiguracaoPreferencias.Padrao();
        configuracao.SincronizarModulos(TresModulos);
        configuracao.OcultarModulo("Bens");
        configuracao.OcultarModulo("Garantias");

        Should.Throw<InvalidOperationException>(() => configuracao.OcultarModulo("Painel"));
    }

    [Fact]
    public void DeveOcultarEMostrarModulo()
    {
        var configuracao = ConfiguracaoPreferencias.Padrao();
        configuracao.SincronizarModulos(TresModulos);

        configuracao.OcultarModulo("Bens");
        configuracao.ModulosOcultos.ShouldContain("Bens");

        configuracao.MostrarModulo("Bens");
        configuracao.ModulosOcultos.ShouldNotContain("Bens");
    }

    [Fact]
    public void DeveLimparModuloInicialAoOcultaLo()
    {
        var configuracao = ConfiguracaoPreferencias.Padrao();
        configuracao.SincronizarModulos(TresModulos);
        configuracao.DefinirModuloInicial("Bens");

        configuracao.OcultarModulo("Bens");

        configuracao.ModuloInicial.ShouldBeNull();
    }

    [Fact]
    public void DeveRejeitarModuloInicialOcultoOuDesconhecido()
    {
        var configuracao = ConfiguracaoPreferencias.Padrao();
        configuracao.SincronizarModulos(TresModulos);
        configuracao.OcultarModulo("Bens");

        Should.Throw<ArgumentException>(() => configuracao.DefinirModuloInicial("Bens"));
        Should.Throw<ArgumentException>(() => configuracao.DefinirModuloInicial("Inexistente"));
    }

    [Fact]
    public void ResolverModuloInicialDeveCairParaOPrimeiroVisivelQuandoNaoDefinido()
    {
        var configuracao = ConfiguracaoPreferencias.Padrao();
        configuracao.SincronizarModulos(TresModulos);
        configuracao.OcultarModulo("Painel");

        configuracao.ResolverModuloInicial().ShouldBe("Bens");
    }

    [Fact]
    public void ResolverModuloInicialDeveUsarOModuloDefinidoQuandoAindaVisivel()
    {
        var configuracao = ConfiguracaoPreferencias.Padrao();
        configuracao.SincronizarModulos(TresModulos);
        configuracao.DefinirModuloInicial("Garantias");

        configuracao.ResolverModuloInicial().ShouldBe("Garantias");
    }

    [Fact]
    public void DeveMoverModuloParaCimaEParaBaixo()
    {
        var configuracao = ConfiguracaoPreferencias.Padrao();
        configuracao.SincronizarModulos(TresModulos);

        configuracao.MoverModuloParaCima("Garantias");
        configuracao.OrdemModulos.ShouldBe(["Painel", "Garantias", "Bens"]);

        configuracao.MoverModuloParaBaixo("Painel");
        configuracao.OrdemModulos.ShouldBe(["Garantias", "Painel", "Bens"]);
    }

    [Fact]
    public void MoverModuloNasExtremidadesNaoDeveAlterarOrdem()
    {
        var configuracao = ConfiguracaoPreferencias.Padrao();
        configuracao.SincronizarModulos(TresModulos);

        configuracao.MoverModuloParaCima("Painel");
        configuracao.MoverModuloParaBaixo("Garantias");

        configuracao.OrdemModulos.ShouldBe(TresModulos);
    }

    [Fact]
    public void DeveRejeitarOperacoesEmModuloDesconhecido()
    {
        var configuracao = ConfiguracaoPreferencias.Padrao();
        configuracao.SincronizarModulos(TresModulos);

        Should.Throw<ArgumentException>(() => configuracao.OcultarModulo("Inexistente"));
        Should.Throw<ArgumentException>(() => configuracao.MostrarModulo("Inexistente"));
        Should.Throw<ArgumentException>(() => configuracao.MoverModuloParaCima("Inexistente"));
    }

    [Fact]
    public void IntervaloComoTimeSpanDeveRefletirOValorEscolhido()
    {
        // Os dados só mudam por ação do usuário (que já atualiza na hora): o ciclo automático não precisa ser frequente.
        ConfiguracaoPreferencias.Padrao().IntervaloComoTimeSpan().ShouldBe(TimeSpan.FromSeconds(30));

        var configuracao = ConfiguracaoPreferencias.Padrao();
        configuracao.DefinirIntervaloAtualizacao(IntervaloAtualizacao.Segundos5);
        configuracao.IntervaloComoTimeSpan().ShouldBe(TimeSpan.FromSeconds(5));
    }
}
