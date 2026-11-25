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

    /// <summary>Quem prestou o serviço (médico, clínica, laboratório) — vai para o relatório do IRPF.</summary>
    public string Prestador { get; private set; } = string.Empty;

    /// <summary>CPF ou CNPJ do prestador, só os dígitos (vazio = não informado). A Receita exige para deduzir.</summary>
    public string DocumentoPrestador { get; private set; } = string.Empty;

    /// <summary>Consultas, exames e procedimentos são dedutíveis no IRPF; farmácia (remédios) e "outro" não.</summary>
    public bool DedutivelNoIrpf => Tipo is TipoGastoSaude.Consulta or TipoGastoSaude.Exame or TipoGastoSaude.Procedimento;

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
        Dinheiro valor,
        string? prestador = null,
        string? documentoPrestador = null)
    {
        var gasto = new GastoSaude(id, bemId, tipo, descricao, data, valor);
        gasto.DefinirPrestador(prestador, documentoPrestador);
        return gasto;
    }

    public void DefinirPrestador(string? nome, string? cpfCnpj)
    {
        Prestador = Validacao.TextoOpcional(nome, "prestador", 120);
        DocumentoPrestador = CpfCnpj.Normalizar(cpfCnpj, "documento do prestador");
    }

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
