namespace Assetly.Dominio.Veiculo;

public readonly record struct AbastecimentoId(Guid Valor)
{
    public static AbastecimentoId Novo() => new(Guid.NewGuid());
}
