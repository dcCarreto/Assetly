namespace Assetly.Dominio.Documento;

public readonly record struct DocumentoId(Guid Valor)
{
    public static DocumentoId Novo() => new(Guid.NewGuid());
}
