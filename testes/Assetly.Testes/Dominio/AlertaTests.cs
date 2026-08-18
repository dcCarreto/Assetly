using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Shouldly;

namespace Assetly.Testes.Dominio;

public class AlertaTests
{
    [Fact]
    public void DeveTomarCienciaEResolverAlerta()
    {
        var alerta = Alerta.Criar(
            BemId.Novo(),
            TipoAlerta.GarantiaVencendo,
            SeveridadeAlerta.Aviso,
            "Garantia vence em breve",
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 20));

        alerta.TomarCiencia();
        alerta.Status.ShouldBe(StatusAlerta.Ciente);
        alerta.EstaAcionavel().ShouldBeTrue();

        alerta.Resolver();
        alerta.Status.ShouldBe(StatusAlerta.Resolvido);
        alerta.EstaAcionavel().ShouldBeFalse();
    }

    [Fact]
    public void DeveRejeitarMensagemEmBranco()
    {
        var excecao = Should.Throw<ArgumentException>(() => Alerta.Criar(
            BemId.Novo(),
            TipoAlerta.DocumentoAusente,
            SeveridadeAlerta.Informativo,
            " ",
            new DateOnly(2026, 7, 1),
            null));
        excecao.Message.ShouldContain("mensagem do alerta");
    }

    [Fact]
    public void DeveRejeitarTomarCienciaDeAlertaResolvido()
    {
        var alerta = Alerta.Criar(
            BemId.Novo(),
            TipoAlerta.ManutencaoAtrasada,
            SeveridadeAlerta.Critico,
            "Manutenção está atrasada",
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 6, 1));

        alerta.Resolver();

        var excecao = Should.Throw<InvalidOperationException>(() => alerta.TomarCiencia());
        excecao.Message.ShouldContain("alerta resolvido");
    }
}
