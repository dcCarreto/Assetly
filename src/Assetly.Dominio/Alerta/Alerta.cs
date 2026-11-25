using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;

namespace Assetly.Dominio.Alerta;

public sealed class Alerta
{
    public AlertaId Id { get; }
    public BemId BemId { get; }
    public TipoAlerta Tipo { get; }
    public SeveridadeAlerta Severidade { get; }
    public string Mensagem { get; }
    public DateOnly CriadoEm { get; }
    public DateOnly? PrazoEm { get; }
    public StatusAlerta Status { get; private set; }

    /// <summary>Verdadeiro para alertas gerados por <c>GerarAlertasAutomaticos</c>: só estes são
    /// resolvidos automaticamente quando a condição que os originou deixa de existir. Alertas
    /// criados manualmente pelo usuário nunca são tocados pelo gerador.</summary>
    public bool Automatico { get; }

    /// <summary>Registro que originou um alerta automático (garantia, manutenção, convênio, conta...): dois
    /// registros com a mesma mensagem continuam gerando alertas distintos. Nulo em alertas manuais, nos
    /// ligados ao próprio bem e nos gravados antes da migração V009.</summary>
    public Guid? OrigemId { get; private init; }

    private Alerta(
        AlertaId id,
        BemId bemId,
        TipoAlerta tipo,
        SeveridadeAlerta severidade,
        string? mensagem,
        DateOnly criadoEm,
        DateOnly? prazoEm,
        bool automatico)
    {
        Id = id;
        BemId = bemId;
        Tipo = tipo;
        Severidade = severidade;
        Mensagem = Validacao.ExigirTexto(mensagem, "mensagem do alerta", 240);
        CriadoEm = criadoEm;
        PrazoEm = prazoEm;
        Automatico = automatico;
        Status = StatusAlerta.Aberto;
    }

    public static Alerta Criar(
        BemId bemId,
        TipoAlerta tipo,
        SeveridadeAlerta severidade,
        string? mensagem,
        DateOnly criadoEm,
        DateOnly? prazoEm) =>
        new(AlertaId.Novo(), bemId, tipo, severidade, mensagem, criadoEm, prazoEm, automatico: false);

    public static Alerta CriarAutomatico(
        BemId bemId,
        TipoAlerta tipo,
        SeveridadeAlerta severidade,
        string? mensagem,
        DateOnly criadoEm,
        DateOnly? prazoEm,
        Guid? origemId = null) =>
        new(AlertaId.Novo(), bemId, tipo, severidade, mensagem, criadoEm, prazoEm, automatico: true) { OrigemId = origemId };

    public static Alerta Restaurar(
        AlertaId id,
        BemId bemId,
        TipoAlerta tipo,
        SeveridadeAlerta severidade,
        string? mensagem,
        DateOnly criadoEm,
        DateOnly? prazoEm,
        StatusAlerta status,
        bool automatico = false,
        Guid? origemId = null)
    {
        var alerta = new Alerta(id, bemId, tipo, severidade, mensagem, criadoEm, prazoEm, automatico)
        {
            Status = status,
            OrigemId = origemId
        };
        return alerta;
    }

    public bool EstaAcionavel() => Status != StatusAlerta.Resolvido;

    public void TomarCiencia()
    {
        if (Status == StatusAlerta.Resolvido)
        {
            throw new InvalidOperationException("alerta resolvido não pode ser marcado como ciente");
        }

        Status = StatusAlerta.Ciente;
    }

    public void Resolver()
    {
        Status = StatusAlerta.Resolvido;
    }

    /// <summary>Devolve um alerta resolvido para a lista de pendências (ex.: resolvido por engano).</summary>
    public void Reabrir()
    {
        Status = StatusAlerta.Aberto;
    }
}
