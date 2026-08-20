using Assetly.Dominio.Bem;
using Assetly.Dominio.Saude;
using Shouldly;

namespace Assetly.Testes.Dominio;

public class SaudeTests
{
    [Fact]
    public void DeveRejeitarPeriodoTerminandoAntesDoInicio()
    {
        var iniciaEm = new DateOnly(2026, 1, 10);
        var terminaEm = new DateOnly(2026, 1, 9);

        var excecao = Should.Throw<ArgumentException>(() => new PeriodoSaude(iniciaEm, terminaEm));
        excecao.Message.ShouldContain("data final");
    }

    [Fact]
    public void DeveCalcularStatusDaSaude()
    {
        var saude = Saude.Criar(
            BemId.Novo(),
            TipoSaude.Convenio,
            "Amil",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            "0800-123456");

        saude.StatusEm(new DateOnly(2025, 12, 31)).ShouldBe(StatusSaude.NaoIniciada);
        saude.StatusEm(new DateOnly(2026, 6, 1)).ShouldBe(StatusSaude.Ativa);
        saude.StatusEm(new DateOnly(2026, 12, 15)).ShouldBe(StatusSaude.PertoDoVencimento);
        saude.StatusEm(new DateOnly(2027, 1, 1)).ShouldBe(StatusSaude.Vencida);
    }

    [Fact]
    public void DeveManterCanceladaComoEstadoTerminalIndependenteDaData()
    {
        var saude = Saude.Criar(
            BemId.Novo(),
            TipoSaude.Convenio,
            "Amil",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            "0800-123456");

        saude.Cancelar(new DateOnly(2026, 3, 1));

        saude.StatusEm(new DateOnly(2026, 6, 1)).ShouldBe(StatusSaude.Cancelada);
        saude.StatusEm(new DateOnly(2027, 1, 1)).ShouldBe(StatusSaude.Cancelada);
    }

    [Fact]
    public void DeveRejeitarEdicaoDeRegistroJaCancelado()
    {
        var saude = Saude.Criar(
            BemId.Novo(),
            TipoSaude.Convenio,
            "Amil",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            "0800-123456");

        saude.Cancelar(new DateOnly(2026, 3, 1));

        Should.Throw<InvalidOperationException>(() => saude.AlterarFornecedor("Outra operadora"));
        Should.Throw<InvalidOperationException>(() => saude.Cancelar(new DateOnly(2026, 3, 2)));
    }
}
