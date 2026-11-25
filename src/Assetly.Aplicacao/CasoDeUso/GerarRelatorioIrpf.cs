using System.Globalization;
using System.Text;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;

namespace Assetly.Aplicacao.CasoDeUso;

/// <summary>Total pago a um prestador por um paciente no ano (uma linha da ficha "Pagamentos Efetuados").</summary>
public sealed record PagamentoSaudeIrpf(string Paciente, string Prestador, string DocumentoPrestador, int Quantidade, decimal Total);

/// <summary>Um bem para a ficha "Bens e Direitos": valor de aquisição (não o de mercado) no fim de cada ano.</summary>
public sealed record BemIrpf(string Discriminacao, TipoBem Tipo, DateOnly? CompradoEm, decimal? SituacaoAnoAnterior, decimal SituacaoAno, bool Obrigatorio);

public sealed record RelatorioIrpf(int Ano, IReadOnlyList<PagamentoSaudeIrpf> Pagamentos, int GastosSemDocumento, decimal NaoDedutiveis,
    IReadOnlyList<BemIrpf> Bens)
{
    public decimal TotalDedutivel => Pagamentos.Sum(p => p.Total);

    /// <summary>Texto simples, para imprimir ou copiar os valores para o programa da Receita.</summary>
    public string ParaTexto()
    {
        var cultura = CultureInfo.GetCultureInfo("pt-BR");
        string R(decimal valor) => valor.ToString("C", cultura);
        var texto = new StringBuilder();
        texto.AppendLine($"ASSETLY — APOIO À DECLARAÇÃO DO IMPOSTO DE RENDA (ano-calendário {Ano})");
        texto.AppendLine($"Gerado em {DateTime.Now.ToString("dd/MM/yyyy HH:mm", cultura)}. Confira cada valor com os recibos antes de declarar.");
        texto.AppendLine();
        texto.AppendLine("PAGAMENTOS EFETUADOS — SAÚDE (dedutíveis: consultas, exames e procedimentos)");
        texto.AppendLine(new string('-', 78));
        if (Pagamentos.Count == 0)
        {
            texto.AppendLine("Nenhum gasto dedutível registrado no ano.");
        }

        foreach (var pagamento in Pagamentos)
        {
            var documento = pagamento.DocumentoPrestador.Length == 0 ? "CPF/CNPJ NÃO INFORMADO" : CpfCnpj.Formatar(pagamento.DocumentoPrestador);
            texto.AppendLine($"{pagamento.Prestador} ({documento})");
            texto.AppendLine($"    paciente: {pagamento.Paciente} · {pagamento.Quantidade} pagamento(s) · total {R(pagamento.Total)}");
        }

        texto.AppendLine($"Total dedutível: {R(TotalDedutivel)}");
        if (GastosSemDocumento > 0)
        {
            texto.AppendLine($"Atenção: {GastosSemDocumento} gasto(s) dedutível(is) sem CPF/CNPJ do prestador — sem ele a Receita não aceita a dedução.");
        }

        if (NaoDedutiveis > 0)
        {
            texto.AppendLine($"Não dedutíveis (farmácia e outros): {R(NaoDedutiveis)} — não entram na declaração.");
        }

        texto.AppendLine();
        texto.AppendLine("BENS E DIREITOS (valor de aquisição — a Receita não usa o valor de mercado)");
        texto.AppendLine(new string('-', 78));
        if (Bens.Count == 0)
        {
            texto.AppendLine("Nenhum bem com preço de compra em reais.");
        }

        foreach (var bem in Bens)
        {
            var anterior = bem.SituacaoAnoAnterior is { } valor ? R(valor) : R(0);
            texto.AppendLine($"{bem.Discriminacao}{(bem.Obrigatorio ? string.Empty : "  [opcional: abaixo de R$ 5.000]")}");
            texto.AppendLine($"    situação em 31/12/{Ano - 1}: {anterior} · em 31/12/{Ano}: {R(bem.SituacaoAno)}");
        }

        texto.AppendLine();
        texto.AppendLine("Bens descartados não aparecem (o Assetly não guarda a data em que saíram). Veículos, imóveis e bens acima de R$ 5.000 devem ser declarados.");
        return texto.ToString();
    }
}

/// <summary>Monta, para um ano, os gastos de saúde dedutíveis por prestador e paciente e a lista de bens com valor de
/// aquisição — o que a declaração do IRPF pede e o Assetly já tem.</summary>
public sealed class GerarRelatorioIrpf(IRepositorioBem repositorioBem, IRepositorioGastoSaude repositorioGastoSaude)
{
    /// <summary>Bens móveis abaixo deste valor de aquisição não precisam ser declarados (veículos e imóveis sempre).</summary>
    public const decimal LimiteBemMovel = 5_000m;

    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem));
    private readonly IRepositorioGastoSaude _repositorioGastoSaude = repositorioGastoSaude ?? throw new ArgumentNullException(nameof(repositorioGastoSaude));

    public RelatorioIrpf Executar(int ano)
    {
        var bens = _repositorioBem.Listar();
        var nomes = bens.ToDictionary(b => b.Id, b => b.Nome.Valor);
        var gastosDoAno = _repositorioGastoSaude.Listar().Where(g => g.Data.Year == ano && g.Valor.CodigoMoeda == CalcularPatrimonio.MoedaTotais).ToList();
        var dedutiveis = gastosDoAno.Where(g => g.DedutivelNoIrpf).ToList();

        var pagamentos = dedutiveis
            .GroupBy(g => (Paciente: nomes.GetValueOrDefault(g.BemId, "—"), Prestador: g.Prestador.Length > 0 ? g.Prestador : g.Descricao, g.DocumentoPrestador))
            .Select(grupo => new PagamentoSaudeIrpf(grupo.Key.Paciente, grupo.Key.Prestador, grupo.Key.DocumentoPrestador, grupo.Count(), grupo.Sum(g => g.Valor.Valor)))
            .OrderBy(p => p.Paciente, StringComparer.CurrentCulture)
            .ThenByDescending(p => p.Total)
            .ToList();

        var fimDoAno = new DateOnly(ano, 12, 31);
        var bensIrpf = bens
            .Where(b => b.Tipo != TipoBem.Pessoa && b.Status != StatusBem.Descartado && b.PrecoCompra?.CodigoMoeda == CalcularPatrimonio.MoedaTotais
                        && (b.CompradoEm is null || b.CompradoEm <= fimDoAno))
            .Select(b =>
            {
                var preco = b.PrecoCompra!.Valor;
                var anterior = b.CompradoEm is null || b.CompradoEm.Value.Year < ano ? preco : (decimal?)null;
                return new BemIrpf(Discriminacao(b), b.Tipo, b.CompradoEm, anterior, preco,
                    b.Tipo is TipoBem.Veiculo or TipoBem.Imovel || preco >= LimiteBemMovel);
            })
            .OrderByDescending(b => b.SituacaoAno)
            .ToList();

        return new RelatorioIrpf(ano, pagamentos, dedutiveis.Count(g => g.DocumentoPrestador.Length == 0),
            gastosDoAno.Where(g => !g.DedutivelNoIrpf).Sum(g => g.Valor.Valor), bensIrpf);
    }

    private static string Discriminacao(Bem bem)
    {
        var partes = new List<string> { bem.Nome.Valor };
        var marcaModelo = $"{bem.Inventario.Marca} {bem.Inventario.Modelo}".Trim();
        if (marcaModelo.Length > 0)
        {
            partes.Add(marcaModelo);
        }

        if (bem.Inventario.NumeroSerie.Length > 0)
        {
            partes.Add($"nº de série {bem.Inventario.NumeroSerie}");
        }

        if (bem.CompradoEm is { } compra)
        {
            partes.Add($"adquirido em {compra.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}" + (bem.Inventario.Loja.Length > 0 ? $" de {bem.Inventario.Loja}" : string.Empty));
        }

        return string.Join(", ", partes);
    }
}
