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

    /// <summary>Conta sem fim (condomínio, energia, internet): ao ser paga, gera a do mês seguinte.</summary>
    public bool Recorrente { get; private set; }

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

    public static Despesa Restaurar(DespesaId id, BemId bemId, TipoDespesa tipo, string? descricao, DateOnly vencimento, Dinheiro valor, DateOnly? pagaEm,
        bool recorrente = false)
    {
        var despesa = new Despesa(id, bemId);
        despesa.Atualizar(tipo, descricao, vencimento, valor);
        despesa.PagaEm = pagaEm;
        despesa.Recorrente = recorrente;
        return despesa;
    }

    public void DefinirRecorrencia(bool recorrente) => Recorrente = recorrente;

    /// <summary>A conta do mês seguinte (mesmo tipo, descrição e valor, em aberto), para uma conta recorrente.</summary>
    public Despesa ProximaRecorrencia()
    {
        if (!Recorrente)
        {
            throw new InvalidOperationException("esta despesa não é recorrente");
        }

        var proxima = Registrar(BemId, Tipo, Descricao, Vencimento.AddMonths(1), Valor);
        proxima.Recorrente = true;
        return proxima;
    }

    /// <summary>Se <paramref name="outra"/> é a conta do mês seguinte desta (já gerada antes, ex.: pagamento desfeito e refeito).</summary>
    public bool EhProximaRecorrencia(Despesa outra) =>
        outra.Recorrente && outra.BemId.Equals(BemId) && outra.Tipo == Tipo && outra.Descricao == Descricao && outra.Vencimento == Vencimento.AddMonths(1);

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
