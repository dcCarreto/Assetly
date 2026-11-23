namespace Assetly.Dominio.Despesa;

public readonly record struct DespesaId(Guid Valor)
{
    public static DespesaId Novo() => new(Guid.NewGuid());
}
