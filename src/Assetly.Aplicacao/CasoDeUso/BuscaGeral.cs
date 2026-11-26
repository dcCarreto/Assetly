using System.Globalization;
using System.Text;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Compartilhado;

namespace Assetly.Aplicacao.CasoDeUso;

public enum AreaBusca
{
    Bem,
    Garantia,
    Manutencao,
    Documento,
    Alerta,
    Convenio,
    GastoSaude,
    Veiculo,
    Imovel,
    Despesa,
    Abastecimento
}

/// <summary>Um registro encontrado: onde ele mora, o que mostrar e a chave para a tela selecioná-lo.
/// <see cref="BemId"/>: o bem dono (a tela decide, por ele, se uma despesa é de Automóveis ou de Imóveis).</summary>
public sealed record ResultadoBusca(AreaBusca Area, string Titulo, string Detalhe, object Chave, Dominio.Bem.BemId BemId);

/// <summary>Busca em todos os cadastros de uma vez (Ctrl+K): cada palavra digitada precisa aparecer no registro, sem
/// diferenciar maiúsculas nem acentos ("geladeira cozinha" acha a geladeira cuja localização é "Cozinha").</summary>
public sealed class BuscaGeral(FonteDados dados)
{
    public const int MaximoResultados = 60;

    private readonly FonteDados _dados = dados ?? throw new ArgumentNullException(nameof(dados));

    public IReadOnlyList<ResultadoBusca> Executar(string? consulta) => Indexar().Buscar(consulta);

    /// <summary>Lê os cadastros uma vez (ao abrir a busca); cada tecla digitada só filtra o índice em memória.</summary>
    public IndiceBusca Indexar()
    {
        var bens = _dados.Bens.Listar();
        var nomes = bens.ToDictionary(b => b.Id, b => b.Nome.Valor);
        string Nome(Dominio.Bem.BemId id) => nomes.GetValueOrDefault(id, "—");

        IEnumerable<(ResultadoBusca Resultado, string Texto)> Candidatos()
        {
            foreach (var b in bens)
            {
                var i = b.Inventario;
                yield return (new ResultadoBusca(AreaBusca.Bem, b.Nome.Valor, Juntar(i.Marca, i.Modelo, i.Localizacao), b.Id, b.Id),
                    $"{b.Nome.Valor} {i.Marca} {i.Modelo} {i.NumeroSerie} {i.Loja} {i.Localizacao} {b.Observacoes}");
            }

            foreach (var g in _dados.Garantias.Listar())
            {
                yield return (new ResultadoBusca(AreaBusca.Garantia, $"Garantia {g.Fornecedor}", Nome(g.BemId), g.Id, g.BemId),
                    $"garantia {g.Fornecedor} {g.ContatoSuporte} {Nome(g.BemId)}");
            }

            foreach (var m in _dados.Manutencoes.Listar())
            {
                yield return (new ResultadoBusca(AreaBusca.Manutencao, m.Descricao, $"{Nome(m.BemId)} · manutenção", m.Id, m.BemId),
                    $"manutencao {m.Descricao} {Nome(m.BemId)}");
            }

            foreach (var d in _dados.Documentos.Listar())
            {
                yield return (new ResultadoBusca(AreaBusca.Documento, d.Nome.Valor, $"{Nome(d.BemId)} · documento", d.Id, d.BemId),
                    $"documento {d.Nome.Valor} {d.ChaveAcesso} {Path.GetFileName(d.CaminhoLocal.Valor)} {Nome(d.BemId)}");
            }

            foreach (var a in _dados.Alertas.Listar())
            {
                yield return (new ResultadoBusca(AreaBusca.Alerta, a.Mensagem, $"{Nome(a.BemId)} · alerta", a.Id, a.BemId),
                    $"alerta {a.Mensagem} {Nome(a.BemId)}");
            }

            foreach (var s in _dados.Convenios.Listar())
            {
                yield return (new ResultadoBusca(AreaBusca.Convenio, s.Fornecedor, $"{Nome(s.BemId)} · convênio", s.Id, s.BemId),
                    $"convenio {s.Fornecedor} {s.Contato} {Nome(s.BemId)}");
            }

            foreach (var g in _dados.GastosSaude.Listar())
            {
                yield return (new ResultadoBusca(AreaBusca.GastoSaude, g.Descricao, Juntar(Nome(g.BemId), g.Prestador, "gasto de saúde"), g.Id, g.BemId),
                    $"saude {g.Descricao} {g.Prestador} {g.DocumentoPrestador} {Nome(g.BemId)}");
            }

            foreach (var v in _dados.Veiculos.Listar())
            {
                yield return (new ResultadoBusca(AreaBusca.Veiculo, Nome(v.BemId), Juntar(v.Placa, v.Marca, v.Modelo), v.BemId, v.BemId),
                    $"veiculo {Nome(v.BemId)} {v.Placa} {v.Marca} {v.Modelo} {v.Renavam} {v.Chassi}");
            }

            foreach (var i in _dados.Imoveis.Listar())
            {
                yield return (new ResultadoBusca(AreaBusca.Imovel, Nome(i.BemId), i.EnderecoCompleto, i.BemId, i.BemId),
                    $"imovel {Nome(i.BemId)} {i.EnderecoCompleto} {i.Matricula} {i.InscricaoIptu} {i.Inquilino}");
            }

            foreach (var d in _dados.Despesas.Listar())
            {
                yield return (new ResultadoBusca(AreaBusca.Despesa, d.Descricao, $"{Nome(d.BemId)} · vence {d.Vencimento.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}", d.Id, d.BemId),
                    $"despesa conta {d.Descricao} {Nome(d.BemId)}");
            }

            foreach (var a in _dados.Abastecimentos.Listar())
            {
                yield return (new ResultadoBusca(AreaBusca.Abastecimento, $"Abastecimento {a.Data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}",
                        Juntar(Nome(a.BemId), a.Posto), a.Id, a.BemId),
                    $"abastecimento {a.Posto} {Nome(a.BemId)}");
            }
        }

        return new IndiceBusca(Candidatos().Select(c => (c.Resultado, Normalizar(c.Texto))).ToList());
    }

    private static string Juntar(params string?[] partes) => string.Join(" · ", partes.Where(p => !string.IsNullOrWhiteSpace(p)));

    /// <summary>Minúsculas e sem acentos, mantendo os espaços entre as palavras.</summary>
    internal static string Normalizar(string texto)
    {
        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var saida = new StringBuilder(decomposto.Length);
        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                saida.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
            }
        }

        return saida.ToString();
    }
}

public sealed class IndiceBusca
{
    private readonly IReadOnlyList<(ResultadoBusca Resultado, string Texto)> _itens;

    internal IndiceBusca(IReadOnlyList<(ResultadoBusca Resultado, string Texto)> itens) => _itens = itens;

    public IReadOnlyList<ResultadoBusca> Buscar(string? consulta)
    {
        var palavras = BuscaGeral.Normalizar(consulta ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return palavras.Length == 0
            ? []
            : _itens.Where(item => palavras.All(item.Texto.Contains)).Select(item => item.Resultado).Take(BuscaGeral.MaximoResultados).ToList();
    }
}
