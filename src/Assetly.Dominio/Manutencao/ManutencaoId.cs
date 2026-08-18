namespace Assetly.Dominio.Manutencao;

public readonly record struct ManutencaoId(Guid Valor)
{
    public static ManutencaoId Novo() => new(Guid.NewGuid());
}
