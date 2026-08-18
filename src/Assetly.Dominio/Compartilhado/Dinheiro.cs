namespace Assetly.Dominio.Compartilhado;

public sealed record Dinheiro
{
    public decimal Valor { get; }
    public string CodigoMoeda { get; }

    private Dinheiro(decimal valor, string codigoMoeda)
    {
        Valor = Math.Round(Validacao.ExigirNaoNegativo(valor, "valor"), 2, MidpointRounding.AwayFromZero);
        CodigoMoeda = codigoMoeda;
    }

    public static Dinheiro De(decimal valor, string codigoMoeda)
    {
        if (string.IsNullOrWhiteSpace(codigoMoeda))
        {
            throw new ArgumentNullException(nameof(codigoMoeda), "moeda é obrigatória");
        }

        return new Dinheiro(valor, codigoMoeda.Trim().ToUpperInvariant());
    }

    public static Dinheiro Zero(string codigoMoeda) => De(0m, codigoMoeda);
}
