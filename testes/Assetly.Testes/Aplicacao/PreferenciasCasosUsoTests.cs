using Assetly.Aplicacao.CasoDeUso;
using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Preferencias;
using Shouldly;

namespace Assetly.Testes.Aplicacao;

public class PreferenciasCasosUsoTests
{
    private static readonly string[] TresModulos = ["Painel", "Bens", "Garantias"];

    [Fact]
    public void DeveObterConfiguracaoPadraoQuandoNadaFoiSalvo()
    {
        var repositorio = new RepositorioConfiguracaoPreferenciasMemoria();

        var configuracao = new ObterPreferencias(repositorio).Executar();

        configuracao.Tema.ShouldBe(TemaVisual.Escuro);
        configuracao.CorDestaque.ShouldBe(CorDestaque.Ciano);
    }

    [Fact]
    public void DeveSincronizarModulosEPersistir()
    {
        var repositorio = new RepositorioConfiguracaoPreferenciasMemoria();

        var configuracao = new SincronizarModulosPreferencias(repositorio).Executar(TresModulos);

        configuracao.OrdemModulos.ShouldBe(TresModulos);
        new ObterPreferencias(repositorio).Executar().OrdemModulos.ShouldBe(TresModulos);
    }

    [Fact]
    public void DeveDefinirTema()
    {
        var repositorio = new RepositorioConfiguracaoPreferenciasMemoria();

        var configuracao = new DefinirTema(repositorio).Executar(new DefinirTemaComando(TemaVisual.Claro));

        configuracao.Tema.ShouldBe(TemaVisual.Claro);
        new ObterPreferencias(repositorio).Executar().Tema.ShouldBe(TemaVisual.Claro);
    }

    [Fact]
    public void DeveDefinirCorDestaque()
    {
        var repositorio = new RepositorioConfiguracaoPreferenciasMemoria();

        var configuracao = new DefinirCorDestaque(repositorio).Executar(new DefinirCorDestaqueComando(CorDestaque.Violeta));

        configuracao.CorDestaque.ShouldBe(CorDestaque.Violeta);
    }

    [Fact]
    public void DeveDefinirDensidadeTabela()
    {
        var repositorio = new RepositorioConfiguracaoPreferenciasMemoria();

        var configuracao = new DefinirDensidadeTabela(repositorio).Executar(new DefinirDensidadeTabelaComando(DensidadeTabela.Compacta));

        configuracao.DensidadeTabela.ShouldBe(DensidadeTabela.Compacta);
    }

    [Fact]
    public void DeveDefinirIntervaloAtualizacao()
    {
        var repositorio = new RepositorioConfiguracaoPreferenciasMemoria();

        var configuracao = new DefinirIntervaloAtualizacao(repositorio).Executar(new DefinirIntervaloAtualizacaoComando(IntervaloAtualizacao.Segundos60));

        configuracao.IntervaloAtualizacao.ShouldBe(IntervaloAtualizacao.Segundos60);
    }

    [Fact]
    public void DeveMoverOcultarMostrarEDefinirModuloInicial()
    {
        var repositorio = new RepositorioConfiguracaoPreferenciasMemoria();
        new SincronizarModulosPreferencias(repositorio).Executar(TresModulos);

        new MoverModuloParaCima(repositorio).Executar(new MoverModuloComando("Garantias"));
        new OcultarModulo(repositorio).Executar(new OcultarModuloComando("Bens"));
        var configuracao = new DefinirModuloInicial(repositorio).Executar(new DefinirModuloInicialComando("Garantias"));

        configuracao.OrdemModulos.ShouldBe(["Painel", "Garantias", "Bens"]);
        configuracao.ModulosOcultos.ShouldContain("Bens");
        configuracao.ModuloInicial.ShouldBe("Garantias");

        var reexibido = new MostrarModulo(repositorio).Executar(new MostrarModuloComando("Bens"));
        reexibido.ModulosOcultos.ShouldNotContain("Bens");
    }

    private sealed class RepositorioConfiguracaoPreferenciasMemoria : IRepositorioConfiguracaoPreferencias
    {
        private ConfiguracaoPreferencias _configuracao = ConfiguracaoPreferencias.Padrao();

        public ConfiguracaoPreferencias Obter() => _configuracao;

        public void Salvar(ConfiguracaoPreferencias configuracao) => _configuracao = configuracao;
    }
}
