using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;

namespace Assetly.Dominio.Saude;

public sealed class Saude
{
    public const int DiasPadraoPertoDoVencimento = 30;

    public SaudeId Id { get; }
    public BemId BemId { get; }
    public TipoSaude Tipo { get; private set; }
    public string Fornecedor { get; private set; }
    public string Contato { get; private set; }
    public PeriodoSaude Periodo { get; private set; }
    public DateOnly? DataCancelamento { get; private set; }

    private Saude(
        SaudeId id,
        BemId bemId,
        TipoSaude tipo,
        string? fornecedor,
        PeriodoSaude periodo,
        string? contato)
    {
        Id = id;
        BemId = bemId;
        Tipo = tipo;
        Fornecedor = Validacao.TextoOpcional(fornecedor, "fornecedor do convênio", 120);
        Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo), "período de saúde é obrigatório");
        Contato = Validacao.TextoOpcional(contato, "contatos de emergência", 160);
    }

    public static Saude Criar(
        BemId bemId,
        TipoSaude tipo,
        string? fornecedor,
        DateOnly iniciaEm,
        DateOnly terminaEm,
        string? contato) =>
        new(SaudeId.Novo(), bemId, tipo, fornecedor, new PeriodoSaude(iniciaEm, terminaEm), contato);

    public static Saude Restaurar(
        SaudeId id,
        BemId bemId,
        TipoSaude tipo,
        string? fornecedor,
        DateOnly iniciaEm,
        DateOnly terminaEm,
        string? contato,
        DateOnly? dataCancelamento)
    {
        var saude = new Saude(id, bemId, tipo, fornecedor, new PeriodoSaude(iniciaEm, terminaEm), contato)
        {
            DataCancelamento = dataCancelamento
        };
        return saude;
    }

    public StatusSaude StatusEm(DateOnly data)
    {
        if (DataCancelamento.HasValue)
        {
            return StatusSaude.Cancelada;
        }

        if (data < Periodo.IniciaEm)
        {
            return StatusSaude.NaoIniciada;
        }

        if (Periodo.EstaVencidaEm(data))
        {
            return StatusSaude.Vencida;
        }

        if (Periodo.VenceEmAte(data, DiasPadraoPertoDoVencimento))
        {
            return StatusSaude.PertoDoVencimento;
        }

        return StatusSaude.Ativa;
    }

    public void AlterarTipo(TipoSaude tipo)
    {
        ExigirNaoCancelada();
        Tipo = tipo;
    }

    public void AlterarFornecedor(string? fornecedor)
    {
        ExigirNaoCancelada();
        Fornecedor = Validacao.TextoOpcional(fornecedor, "fornecedor do convênio", 120);
    }

    public void AlterarPeriodo(DateOnly iniciaEm, DateOnly terminaEm)
    {
        ExigirNaoCancelada();
        Periodo = new PeriodoSaude(iniciaEm, terminaEm);
    }

    public void AtualizarContato(string? contato)
    {
        ExigirNaoCancelada();
        Contato = Validacao.TextoOpcional(contato, "contatos de emergência", 160);
    }

    public void Cancelar(DateOnly dataCancelamento)
    {
        ExigirNaoCancelada();
        DataCancelamento = dataCancelamento;
    }

    private void ExigirNaoCancelada()
    {
        if (DataCancelamento.HasValue)
        {
            throw new InvalidOperationException("registro de saúde já está cancelado");
        }
    }
}
