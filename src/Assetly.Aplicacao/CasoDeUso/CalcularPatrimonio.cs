using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed record ItemPatrimonio(Bem Bem, decimal ValorCompra, decimal ValorAtual)
{
    public decimal Perda => ValorCompra - ValorAtual;
    public decimal PercentualAtual => ValorCompra == 0 ? 0 : ValorAtual / ValorCompra;
}

public sealed record GrupoPatrimonio(string Rotulo, int Quantidade, decimal ValorCompra, decimal ValorAtual);

public sealed record PontoEvolucao(int Ano, decimal Valor);

/// <summary>Visão consolidada do patrimônio (bens em uso com preço de compra, em reais).</summary>
public sealed record ResumoPatrimonio(
    decimal ValorCompra,
    decimal ValorAtual,
    IReadOnlyList<ItemPatrimonio> Itens,
    IReadOnlyList<(TipoBem Tipo, GrupoPatrimonio Grupo)> PorTipo,
    IReadOnlyList<GrupoPatrimonio> PorResponsavel,
    IReadOnlyList<PontoEvolucao> Evolucao,
    int SemPreco,
    int SemDataCompra,
    int EmOutraMoeda)
{
    public decimal Depreciacao => ValorCompra - ValorAtual;
    public decimal PercentualDepreciado => ValorCompra == 0 ? 0 : Depreciacao / ValorCompra;
}

/// <summary>
/// Calcula o patrimônio: valor de compra x valor estimado hoje (ver <see cref="Depreciacao"/>), por
/// tipo, por responsável (modo família) e a evolução estimada ano a ano. Considera os bens em uso
/// (ativos ou em manutenção); valores em outras moedas ficam fora dos totais (e são contados à parte).
/// </summary>
public sealed class CalcularPatrimonio(IRepositorioBem repositorioBem)
{
    public const string MoedaTotais = "BRL";
    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem));

    public ResumoPatrimonio Executar(DateOnly hoje, int anosEvolucao = 6)
    {
        var todos = _repositorioBem.Listar();
        var emUso = todos.Where(b => b.Status is StatusBem.Ativo or StatusBem.EmManutencao && b.Tipo != TipoBem.Pessoa).ToList();
        var nomes = todos.ToDictionary(b => b.Id, b => b.Nome.Valor);

        var semPreco = emUso.Count(b => b.PrecoCompra is null);
        var emOutraMoeda = emUso.Count(b => b.PrecoCompra is not null && b.PrecoCompra.CodigoMoeda != MoedaTotais);
        var considerados = emUso.Where(b => b.PrecoCompra?.CodigoMoeda == MoedaTotais).ToList();

        var itens = considerados
            .Select(b => new ItemPatrimonio(b, b.PrecoCompra!.Valor, Depreciacao.ValorEstimado(b, hoje)?.Valor ?? b.PrecoCompra.Valor))
            .OrderByDescending(i => i.ValorAtual)
            .ToList();

        var porTipo = itens
            .GroupBy(i => i.Bem.Tipo)
            .Select(g => (g.Key, new GrupoPatrimonio(g.Key.ToString(), g.Count(), g.Sum(i => i.ValorCompra), g.Sum(i => i.ValorAtual))))
            .OrderByDescending(t => t.Item2.ValorAtual)
            .ToList();

        var porResponsavel = itens
            .GroupBy(i => i.Bem.ResponsavelId)
            .Select(g => new GrupoPatrimonio(
                g.Key is { } id && nomes.TryGetValue(id, out var nome) ? nome : "Sem responsável",
                g.Count(), g.Sum(i => i.ValorCompra), g.Sum(i => i.ValorAtual)))
            .OrderByDescending(g => g.ValorAtual)
            .ToList();

        var evolucao = Enumerable.Range(hoje.Year - anosEvolucao + 1, anosEvolucao)
            .Select(ano =>
            {
                var referencia = ano == hoje.Year ? hoje : new DateOnly(ano, 12, 31);
                return new PontoEvolucao(ano, considerados.Sum(b => Depreciacao.ValorEstimado(b, referencia)?.Valor ?? 0m));
            })
            .ToList();

        return new ResumoPatrimonio(
            itens.Sum(i => i.ValorCompra),
            itens.Sum(i => i.ValorAtual),
            itens,
            porTipo,
            porResponsavel,
            evolucao,
            semPreco,
            considerados.Count(b => b.CompradoEm is null),
            emOutraMoeda);
    }
}
