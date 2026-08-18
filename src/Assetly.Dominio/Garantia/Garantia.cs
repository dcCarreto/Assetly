using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;

namespace Assetly.Dominio.Garantia;

public sealed class Garantia
{
    public const int DiasPadraoPertoDoVencimento = 30;

    public GarantiaId Id { get; }
    public BemId BemId { get; }
    public TipoGarantia Tipo { get; private set; }
    public string Fornecedor { get; private set; }
    public PeriodoGarantia Periodo { get; private set; }
    public string ContatoSuporte { get; private set; }

    private Garantia(
        GarantiaId id,
        BemId bemId,
        TipoGarantia tipo,
        string? fornecedor,
        PeriodoGarantia periodo,
        string? contatoSuporte)
    {
        Id = id;
        BemId = bemId;
        Tipo = tipo;
        Fornecedor = Validacao.TextoOpcional(fornecedor, "fornecedor da garantia", 120);
        Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo), "período da garantia é obrigatório");
        ContatoSuporte = Validacao.TextoOpcional(contatoSuporte, "contato de suporte", 160);
    }

    public static Garantia Criar(
        BemId bemId,
        TipoGarantia tipo,
        string? fornecedor,
        DateOnly iniciaEm,
        DateOnly terminaEm) =>
        new(GarantiaId.Novo(), bemId, tipo, fornecedor, new PeriodoGarantia(iniciaEm, terminaEm), string.Empty);

    public static Garantia Restaurar(
        GarantiaId id,
        BemId bemId,
        TipoGarantia tipo,
        string? fornecedor,
        DateOnly iniciaEm,
        DateOnly terminaEm,
        string? contatoSuporte) =>
        new(id, bemId, tipo, fornecedor, new PeriodoGarantia(iniciaEm, terminaEm), contatoSuporte);

    public StatusGarantia StatusEm(DateOnly data)
    {
        if (data < Periodo.IniciaEm)
        {
            return StatusGarantia.NaoIniciada;
        }

        if (Periodo.EstaVencidaEm(data))
        {
            return StatusGarantia.Vencida;
        }

        if (Periodo.VenceEmAte(data, DiasPadraoPertoDoVencimento))
        {
            return StatusGarantia.PertoDoVencimento;
        }

        return StatusGarantia.Ativa;
    }

    public void AlterarFornecedor(string? fornecedor)
    {
        Fornecedor = Validacao.TextoOpcional(fornecedor, "fornecedor da garantia", 120);
    }

    public void AlterarPeriodo(DateOnly iniciaEm, DateOnly terminaEm)
    {
        Periodo = new PeriodoGarantia(iniciaEm, terminaEm);
    }

    public void AlterarTipo(TipoGarantia tipo)
    {
        Tipo = tipo;
    }

    public void AtualizarContatoSuporte(string? contatoSuporte)
    {
        ContatoSuporte = Validacao.TextoOpcional(contatoSuporte, "contato de suporte", 160);
    }
}
