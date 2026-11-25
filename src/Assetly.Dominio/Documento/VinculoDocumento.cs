namespace Assetly.Dominio.Documento;

public enum TipoVinculoDocumento
{
    Manutencao,
    Despesa,
    Abastecimento
}

/// <summary>Registro do bem a que o documento é o comprovante (a revisão, o IPVA, o abastecimento). O documento
/// continua ligado ao bem; se o registro for excluído, o comprovante fica só no bem.</summary>
public sealed record VinculoDocumento(TipoVinculoDocumento Tipo, Guid Id);
