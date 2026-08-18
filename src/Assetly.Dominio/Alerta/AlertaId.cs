namespace Assetly.Dominio.Alerta;

public readonly record struct AlertaId(Guid Valor)
{
    public static AlertaId Novo() => new(Guid.NewGuid());
}
