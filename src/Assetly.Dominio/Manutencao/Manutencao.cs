using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;

namespace Assetly.Dominio.Manutencao;

public sealed class Manutencao
{
    public ManutencaoId Id { get; }
    public BemId BemId { get; }
    public TipoManutencao Tipo { get; private set; }
    public string Descricao { get; private set; }
    public DateOnly AgendadaPara { get; private set; }
    public DateOnly? ConcluidaEm { get; private set; }
    public Dinheiro? Custo { get; private set; }
    public StatusManutencao Status { get; private set; }

    private Manutencao(
        ManutencaoId id,
        BemId bemId,
        TipoManutencao tipo,
        string? descricao,
        DateOnly agendadaPara)
    {
        Id = id;
        BemId = bemId;
        Tipo = tipo;
        Descricao = Validacao.ExigirTexto(descricao, "descrição da manutenção", 240);
        AgendadaPara = agendadaPara;
        Status = StatusManutencao.Agendada;
    }

    public static Manutencao Agendar(BemId bemId, TipoManutencao tipo, string? descricao, DateOnly agendadaPara) =>
        new(ManutencaoId.Novo(), bemId, tipo, descricao, agendadaPara);

    public static Manutencao Restaurar(
        ManutencaoId id,
        BemId bemId,
        TipoManutencao tipo,
        string? descricao,
        DateOnly agendadaPara,
        DateOnly? concluidaEm,
        Dinheiro? custo,
        StatusManutencao status)
    {
        var manutencao = new Manutencao(id, bemId, tipo, descricao, agendadaPara)
        {
            ConcluidaEm = concluidaEm,
            Custo = custo,
            Status = status
        };
        return manutencao;
    }

    public StatusManutencao StatusEm(DateOnly data)
    {
        if (Status is StatusManutencao.Concluida or StatusManutencao.Cancelada)
        {
            return Status;
        }

        return AgendadaPara < data ? StatusManutencao.Atrasada : StatusManutencao.Agendada;
    }

    public void Reagendar(DateOnly agendadaPara)
    {
        ExigirAberta();
        AgendadaPara = agendadaPara;
        Status = StatusManutencao.Agendada;
    }

    public void AtualizarDescricao(string? descricao)
    {
        Descricao = Validacao.ExigirTexto(descricao, "descrição da manutenção", 240);
    }

    public void AlterarTipo(TipoManutencao tipo)
    {
        Tipo = tipo;
    }

    public void Concluir(DateOnly concluidaEm, Dinheiro? custo)
    {
        ExigirAberta();
        ConcluidaEm = concluidaEm;
        Custo = custo;
        Status = StatusManutencao.Concluida;
    }

    public void Cancelar()
    {
        ExigirAberta();
        Status = StatusManutencao.Cancelada;
    }

    private void ExigirAberta()
    {
        if (Status is StatusManutencao.Concluida or StatusManutencao.Cancelada)
        {
            throw new InvalidOperationException("manutenção já está encerrada");
        }
    }
}
