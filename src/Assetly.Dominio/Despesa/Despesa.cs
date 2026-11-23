using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;

namespace Assetly.Dominio.Despesa;

/// <summary>Conta a pagar ligada a um bem (IPVA, multa, IPTU, condomínio, energia...), com vencimento e
/// registro de pagamento.</summary>
public sealed class Despesa
{
    public const int DiasAvisoVencimento = 7;
    public const int TamanhoMaximoDescricao = 160;

    public DespesaId Id { get; }
    public BemId BemId { get; }
    public TipoDespesa Tipo { get; private set; }
    public string Descricao { get; private set; } = string.Empty;
    public DateOnly Vencimento { get; private set; }
    public Dinheiro Valor { get; private set; } = null!;
    public DateOnly? PagaEm { get; private set; }

    private Despesa(DespesaId id, BemId bemId)
    {
        Id = id;
        BemId = bemId;
    }

    public static Despesa Registrar(BemId bemId, TipoDespesa tipo, string? descricao, DateOnly vencimento, Dinheiro valor)
    {
        var despesa = new Despesa(DespesaId.Novo(), bemId);
        despesa.Atualizar(tipo, descricao, vencimento, valor);
        return despesa;
    }

    public static Despesa Restaurar(DespesaId id, BemId bemId, TipoDespesa tipo, string? descricao, DateOnly vencimento, Dinheiro valor, DateOnly? pagaEm)
    {
        var despesa = new Despesa(id, bemId);
        despesa.Atualizar(tipo, descricao, vencimento, valor);
        despesa.PagaEm = pagaEm;
        return despesa;
    }

    public void Atualizar(TipoDespesa tipo, string? descricao, DateOnly vencimento, Dinheiro valor)
    {
        Tipo = tipo;
        Descricao = Validacao.ExigirTexto(descricao, "descrição da despesa", TamanhoMaximoDescricao);
        Vencimento = vencimento;
        Valor = valor ?? throw new ArgumentNullException(nameof(valor), "valor da despesa é obrigatório");
    }

    public void Pagar(DateOnly pagaEm)
    {
        if (PagaEm is not null)
        {
            throw new InvalidOperationException("esta despesa já está paga");
        }

        PagaEm = Validacao.ExigirDataNaoFutura(pagaEm, "data do pagamento", DateOnly.FromDateTime(DateTime.Now));
    }

    public void DesfazerPagamento()
    {
        if (PagaEm is null)
        {
            throw new InvalidOperationException("esta despesa não está paga");
        }

        PagaEm = null;
    }

    public StatusDespesa StatusEm(DateOnly data)
    {
        if (PagaEm is not null)
        {
            return StatusDespesa.Paga;
        }

        if (Vencimento < data)
        {
            return StatusDespesa.Vencida;
        }

        return Vencimento <= data.AddDays(DiasAvisoVencimento) ? StatusDespesa.AVencer : StatusDespesa.Pendente;
    }
}
