namespace Assetly.Dominio.Bem;

public readonly record struct BemId(Guid Valor)
{
    public static BemId Novo() => new(Guid.NewGuid());
}
