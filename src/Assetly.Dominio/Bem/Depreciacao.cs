using Assetly.Dominio.Compartilhado;

namespace Assetly.Dominio.Bem;

/// <summary>Regra de depreciação de um tipo de bem: perda linear por ano até um valor residual mínimo.</summary>
public sealed record RegraDepreciacao(decimal TaxaAnual, decimal ResidualMinimo)
{
    public bool Deprecia => TaxaAnual > 0;
}

/// <summary>
/// Estimativa de valor atual pelo método linear, inspirada nas taxas usuais de depreciação de bens
/// (eletrônicos ~5 anos, móveis e eletrodomésticos ~10 anos). É uma estimativa para acompanhamento
/// pessoal — não substitui avaliação de mercado. Imóveis não depreciam aqui (costumam manter ou ganhar
/// valor); pessoas (titulares) não têm valor patrimonial.
/// </summary>
public static class Depreciacao
{
    public static RegraDepreciacao Regra(TipoBem tipo) => tipo switch
    {
        TipoBem.Eletronico => new RegraDepreciacao(0.20m, 0.10m),
        TipoBem.Eletrodomestico => new RegraDepreciacao(0.10m, 0.10m),
        TipoBem.Veiculo => new RegraDepreciacao(0.12m, 0.20m),
        TipoBem.Ferramenta => new RegraDepreciacao(0.10m, 0.10m),
        TipoBem.Movel => new RegraDepreciacao(0.10m, 0.10m),
        TipoBem.Imovel => new RegraDepreciacao(0m, 1m),
        TipoBem.Pessoa => new RegraDepreciacao(0m, 0m),
        _ => new RegraDepreciacao(0.10m, 0.10m)
    };

    /// <summary>Se o bem entra no patrimônio: tem preço de compra e não é pessoa.</summary>
    public static bool ContaNoPatrimonio(Bem bem) => bem.PrecoCompra is not null && bem.Tipo != TipoBem.Pessoa;

    /// <summary>Valor estimado do bem na data. Sem data de compra, não há como depreciar: vale o preço
    /// de compra. Antes da compra, o bem ainda não fazia parte do patrimônio (nulo).</summary>
    public static Dinheiro? ValorEstimado(Bem bem, DateOnly data)
    {
        if (!ContaNoPatrimonio(bem))
        {
            return null;
        }

        var preco = bem.PrecoCompra!;
        if (bem.CompradoEm is null)
        {
            return preco;
        }

        if (data < bem.CompradoEm.Value)
        {
            return null;
        }

        var regra = Regra(bem.Tipo);
        if (!regra.Deprecia)
        {
            return preco;
        }

        var anos = (data.DayNumber - bem.CompradoEm.Value.DayNumber) / 365.25m;
        var fator = Math.Max(regra.ResidualMinimo, 1m - regra.TaxaAnual * anos);
        return Dinheiro.De(preco.Valor * fator, preco.CodigoMoeda);
    }
}
