namespace Assetly.Dominio.Saude;

public readonly record struct GastoSaudeId(Guid Valor)
{
    public static GastoSaudeId Novo() => new(Guid.NewGuid());
}
