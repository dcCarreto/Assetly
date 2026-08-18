using Assetly.Dominio.Bem;
using Assetly.Dominio.Garantia;
using Shouldly;

namespace Assetly.Testes.Dominio;

public class GarantiaTests
{
    [Fact]
    public void DeveRejeitarPeriodoTerminandoAntesDoInicio()
    {
        var iniciaEm = new DateOnly(2026, 1, 10);
        var terminaEm = new DateOnly(2026, 1, 9);

        var excecao = Should.Throw<ArgumentException>(() => new PeriodoGarantia(iniciaEm, terminaEm));
        excecao.Message.ShouldContain("data final");
    }

    [Fact]
    public void DeveCalcularStatusDaGarantia()
    {
        var garantia = Garantia.Criar(
            BemId.Novo(),
            TipoGarantia.Fabricante,
            "Acme",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31));

        garantia.StatusEm(new DateOnly(2025, 12, 31)).ShouldBe(StatusGarantia.NaoIniciada);
        garantia.StatusEm(new DateOnly(2026, 6, 1)).ShouldBe(StatusGarantia.Ativa);
        garantia.StatusEm(new DateOnly(2026, 12, 15)).ShouldBe(StatusGarantia.PertoDoVencimento);
        garantia.StatusEm(new DateOnly(2027, 1, 1)).ShouldBe(StatusGarantia.Vencida);
    }
}
