namespace Assetly.Dominio.Garantia;

public readonly record struct GarantiaId(Guid Valor)
{
    public static GarantiaId Novo() => new(Guid.NewGuid());
}
