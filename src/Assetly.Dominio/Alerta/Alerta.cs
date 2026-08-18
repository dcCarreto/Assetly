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

    private Alerta(
        AlertaId id,
        BemId bemId,
        TipoAlerta tipo,
        SeveridadeAlerta severidade,
        string? mensagem,
        DateOnly criadoEm,
        DateOnly? prazoEm)
    {
        Id = id;
        BemId = bemId;
        Tipo = tipo;
        Severidade = severidade;
        Mensagem = Validacao.ExigirTexto(mensagem, "mensagem do alerta", 240);
        CriadoEm = criadoEm;
        PrazoEm = prazoEm;
        Status = StatusAlerta.Aberto;
    }

    public static Alerta Criar(
        BemId bemId,
        TipoAlerta tipo,
        SeveridadeAlerta severidade,
        string? mensagem,
        DateOnly criadoEm,
        DateOnly? prazoEm) =>
        new(AlertaId.Novo(), bemId, tipo, severidade, mensagem, criadoEm, prazoEm);

    public static Alerta Restaurar(
        AlertaId id,
        BemId bemId,
        TipoAlerta tipo,
        SeveridadeAlerta severidade,
        string? mensagem,
        DateOnly criadoEm,
        DateOnly? prazoEm,
        StatusAlerta status)
    {
        var alerta = new Alerta(id, bemId, tipo, severidade, mensagem, criadoEm, prazoEm)
        {
            Status = status
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
}
