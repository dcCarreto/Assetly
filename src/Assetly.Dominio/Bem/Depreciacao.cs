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

    /// <summary>Regra deste bem: a do tipo, ou a taxa própria informada (com o mesmo valor residual mínimo;
    /// 10% para tipos que normalmente não depreciam, como imóvel).</summary>
    public static RegraDepreciacao Regra(Bem bem)
    {
        var doTipo = Regra(bem.Tipo);
        return bem.TaxaDepreciacao is { } taxa
            ? new RegraDepreciacao(taxa, doTipo.Deprecia ? doTipo.ResidualMinimo : 0.10m)
            : doTipo;
    }

    /// <summary>Se o bem entra no patrimônio: tem preço de compra ou valor informado e não é pessoa.</summary>
    public static bool ContaNoPatrimonio(Bem bem) =>
        (bem.PrecoCompra is not null || bem.ValorInformado is not null) && bem.Tipo != TipoBem.Pessoa;

    /// <summary>Valor estimado do bem na data. O valor informado pelo usuário vale a partir da data da avaliação.
    /// Sem ele: sem data de compra, não há como depreciar (vale o preço de compra); antes da compra, o bem ainda
    /// não fazia parte do patrimônio (nulo).</summary>
    public static Dinheiro? ValorEstimado(Bem bem, DateOnly data)
    {
        if (!ContaNoPatrimonio(bem))
        {
            return null;
        }

        if (bem.CompradoEm is { } compra && data < compra)
        {
            return null;
        }

        if (bem.ValorInformado is { } informado && (data >= bem.ValorInformadoEm || bem.PrecoCompra is null))
        {
            return informado;
        }

        var preco = bem.PrecoCompra!;
        var regra = Regra(bem);
        if (bem.CompradoEm is null || !regra.Deprecia)
        {
            return preco;
        }

        var anos = (data.DayNumber - bem.CompradoEm.Value.DayNumber) / 365.25m;
        var fator = Math.Max(regra.ResidualMinimo, 1m - regra.TaxaAnual * anos);
        return Dinheiro.De(preco.Valor * fator, preco.CodigoMoeda);
    }

    /// <summary>Moeda em que o bem é avaliado (a do preço de compra ou, sem ele, a do valor informado).</summary>
    public static string? Moeda(Bem bem) => bem.PrecoCompra?.CodigoMoeda ?? bem.ValorInformado?.CodigoMoeda;
}
