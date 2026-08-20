using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;

namespace Assetly.Dominio.Saude;

/// <summary>Registro pontual de despesa de saúde (farmácia, consulta, exame, procedimento) —
/// diferente de <see cref="Saude"/> (cobertura por período), é um evento único com valor, sem estado.</summary>
public sealed class GastoSaude
{
    public GastoSaudeId Id { get; }
    public BemId BemId { get; }
    public TipoGastoSaude Tipo { get; private set; }
    public string Descricao { get; private set; }
    public DateOnly Data { get; private set; }
    public Dinheiro Valor { get; private set; }

    private GastoSaude(
        GastoSaudeId id,
        BemId bemId,
        TipoGastoSaude tipo,
        string? descricao,
        DateOnly data,
        Dinheiro valor)
    {
        Id = id;
        BemId = bemId;
        Tipo = tipo;
        Descricao = Validacao.ExigirTexto(descricao, "descrição do gasto de saúde", 240);
        Data = data;
        Valor = valor ?? throw new ArgumentNullException(nameof(valor), "valor do gasto de saúde é obrigatório");
    }

    public static GastoSaude Registrar(BemId bemId, TipoGastoSaude tipo, string? descricao, DateOnly data, Dinheiro valor) =>
        new(GastoSaudeId.Novo(), bemId, tipo, descricao, data, valor);

    public static GastoSaude Restaurar(
        GastoSaudeId id,
        BemId bemId,
        TipoGastoSaude tipo,
        string? descricao,
        DateOnly data,
        Dinheiro valor) =>
        new(id, bemId, tipo, descricao, data, valor);

    public void AlterarTipo(TipoGastoSaude tipo)
    {
        Tipo = tipo;
    }

    public void AtualizarDescricao(string? descricao)
    {
        Descricao = Validacao.ExigirTexto(descricao, "descrição do gasto de saúde", 240);
    }

    public void AlterarData(DateOnly data)
    {
        Data = data;
    }

    public void AlterarValor(Dinheiro valor)
    {
        Valor = valor ?? throw new ArgumentNullException(nameof(valor), "valor do gasto de saúde é obrigatório");
    }
}
