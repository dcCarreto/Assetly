using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Despesa;

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
    int EmOutraMoeda,
    decimal Dividas,
    int ParcelasEmAberto)
{
    public decimal Depreciacao => ValorCompra - ValorAtual;

    /// <summary>Quanto o patrimônio vale descontadas as dívidas (financiamentos ainda não pagos).</summary>
    public decimal PatrimonioLiquido => ValorAtual - Dividas;
    public decimal PercentualDepreciado => ValorCompra == 0 ? 0 : Depreciacao / ValorCompra;
}

/// <summary>
/// Calcula o patrimônio: valor de compra x valor estimado hoje (ver <see cref="Depreciacao"/>), por
/// tipo, por responsável (modo família) e a evolução estimada ano a ano. Considera os bens em uso
/// (ativos ou em manutenção); valores em outras moedas ficam fora dos totais (e são contados à parte).
/// As dívidas são as parcelas de financiamento ainda não pagas (despesas do tipo Financiamento em aberto).
/// </summary>
public sealed class CalcularPatrimonio(IRepositorioBem repositorioBem, IRepositorioDespesa repositorioDespesa)
{
    public const string MoedaTotais = "BRL";
    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem));
    private readonly IRepositorioDespesa _repositorioDespesa = repositorioDespesa ?? throw new ArgumentNullException(nameof(repositorioDespesa));

    public ResumoPatrimonio Executar(DateOnly hoje, int anosEvolucao = 6)
    {
        var todos = _repositorioBem.Listar();
        var emUso = todos.Where(b => b.Status is StatusBem.Ativo or StatusBem.EmManutencao && b.Tipo != TipoBem.Pessoa).ToList();
        var nomes = todos.ToDictionary(b => b.Id, b => b.Nome.Valor);

        var semPreco = emUso.Count(b => !Depreciacao.ContaNoPatrimonio(b));
        var emOutraMoeda = emUso.Count(b => Depreciacao.Moeda(b) is { } moeda && moeda != MoedaTotais);
        var considerados = emUso.Where(b => Depreciacao.Moeda(b) == MoedaTotais).ToList();

        // Sem preço de compra (só valor informado), a compra conta pelo próprio valor: não há perda a mostrar.
        var itens = considerados
            .Select(b =>
            {
                var atual = Depreciacao.ValorEstimado(b, hoje)?.Valor ?? b.PrecoCompra?.Valor ?? 0m;
                return new ItemPatrimonio(b, b.PrecoCompra?.Valor ?? atual, atual);
            })
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

        var dividas = _repositorioDespesa.Listar()
            .Where(d => d.Tipo == TipoDespesa.Financiamento && d.PagaEm is null && d.Valor.CodigoMoeda == MoedaTotais)
            .ToList();

        return new ResumoPatrimonio(
            itens.Sum(i => i.ValorCompra),
            itens.Sum(i => i.ValorAtual),
            itens,
            porTipo,
            porResponsavel,
            evolucao,
            semPreco,
            considerados.Count(b => b.CompradoEm is null && b.ValorInformado is null),
            emOutraMoeda,
            dividas.Sum(d => d.Valor.Valor),
            dividas.Count);
    }
}
