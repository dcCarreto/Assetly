using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;

namespace Assetly.Dominio.Veiculo;

/// <summary>Um abastecimento (ou recarga, no caso de elétricos: "litros" vira kWh).</summary>
public sealed class Abastecimento
{
    public AbastecimentoId Id { get; }
    public BemId BemId { get; }
    public DateOnly Data { get; private set; }
    public int Quilometragem { get; private set; }
    public decimal Litros { get; private set; }
    public Dinheiro Valor { get; private set; } = null!;
    public TipoCombustivel Combustivel { get; private set; }
    public bool TanqueCheio { get; private set; }
    public string Posto { get; private set; } = string.Empty;

    private Abastecimento(AbastecimentoId id, BemId bemId)
    {
        Id = id;
        BemId = bemId;
    }

    public static Abastecimento Registrar(BemId bemId, DateOnly data, int quilometragem, decimal litros, Dinheiro valor,
        TipoCombustivel combustivel, bool tanqueCheio, string? posto)
    {
        var abastecimento = new Abastecimento(AbastecimentoId.Novo(), bemId);
        abastecimento.Atualizar(data, quilometragem, litros, valor, combustivel, tanqueCheio, posto);
        return abastecimento;
    }

    public static Abastecimento Restaurar(AbastecimentoId id, BemId bemId, DateOnly data, int quilometragem, decimal litros,
        Dinheiro valor, TipoCombustivel combustivel, bool tanqueCheio, string? posto)
    {
        var abastecimento = new Abastecimento(id, bemId)
        {
            Data = data,
            Quilometragem = quilometragem,
            Litros = litros,
            Valor = valor,
            Combustivel = combustivel,
            TanqueCheio = tanqueCheio,
            Posto = posto ?? string.Empty
        };
        return abastecimento;
    }

    public void Atualizar(DateOnly data, int quilometragem, decimal litros, Dinheiro valor, TipoCombustivel combustivel, bool tanqueCheio, string? posto)
    {
        Data = Validacao.ExigirDataNaoFutura(data, "data do abastecimento", DateOnly.FromDateTime(DateTime.Now));
        if (quilometragem < 0)
        {
            throw new ArgumentException("quilometragem não pode ser negativa");
        }

        if (litros <= 0 || litros > 2000)
        {
            throw new ArgumentException("quantidade abastecida deve ser maior que zero (e até 2.000)");
        }

        Quilometragem = quilometragem;
        Litros = Math.Round(litros, 3, MidpointRounding.AwayFromZero);
        Valor = valor ?? throw new ArgumentNullException(nameof(valor), "valor do abastecimento é obrigatório");
        Combustivel = combustivel;
        TanqueCheio = tanqueCheio;
        Posto = Validacao.TextoOpcional(posto, "posto", 120);
    }

    public decimal PrecoPorLitro => Math.Round(Valor.Valor / Litros, 3, MidpointRounding.AwayFromZero);
}
