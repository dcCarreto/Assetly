namespace Assetly.Dominio.Saude;

public readonly record struct SaudeId(Guid Valor)
{
    public static SaudeId Novo() => new(Guid.NewGuid());
}
