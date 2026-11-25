using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Despesa;
using Assetly.Infraestrutura.Banco;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioDespesaSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioDespesa
{
    private const string Ordem = "ORDER BY vencimento, descricao";
    private readonly ExecutorSqlite _executor = new(fabricaConexao);

    public Despesa? BuscarPorId(DespesaId id) =>
        _executor.Consultar("SELECT * FROM despesas WHERE id = $id", Mapear, "Não foi possível buscar despesa",
            ("$id", id.Valor.ToString())).FirstOrDefault();

    public IReadOnlyList<Despesa> Listar() =>
        _executor.Consultar($"SELECT * FROM despesas {Ordem}", Mapear, "Não foi possível listar despesas");

    public IReadOnlyList<Despesa> ListarPorBem(BemId bemId) =>
        _executor.Consultar($"SELECT * FROM despesas WHERE bem_id = $bemId {Ordem}", Mapear, "Não foi possível listar despesas",
            ("$bemId", bemId.Valor.ToString()));

    private const string SqlSalvar = """
        INSERT INTO despesas (id, bem_id, tipo, descricao, vencimento, valor, moeda, paga_em)
        VALUES ($id, $bemId, $tipo, $descricao, $vencimento, $valor, $moeda, $pagaEm)
        ON CONFLICT(id) DO UPDATE SET
            tipo = excluded.tipo, descricao = excluded.descricao, vencimento = excluded.vencimento,
            valor = excluded.valor, moeda = excluded.moeda, paga_em = excluded.paga_em
        """;

    public void Salvar(Despesa despesa) =>
        _executor.Executar(SqlSalvar, "Não foi possível salvar despesa", Parametros(despesa));

    public void SalvarTodos(IEnumerable<Despesa> despesas) =>
        _executor.ExecutarEmLote(SqlSalvar, "Não foi possível salvar as despesas", despesas.Select(Parametros));

    private static (string Nome, object Valor)[] Parametros(Despesa despesa) =>
        [
            ("$id", despesa.Id.Valor.ToString()),
            ("$bemId", despesa.BemId.Valor.ToString()),
            ("$tipo", ConversorSqlite.EnumParaTexto(despesa.Tipo)),
            ("$descricao", despesa.Descricao),
            ("$vencimento", ConversorSqlite.Texto(despesa.Vencimento)!),
            ("$valor", ConversorSqlite.Valor(despesa.Valor)!),
            ("$moeda", ConversorSqlite.Moeda(despesa.Valor)!),
            ("$pagaEm", ConversorSqlite.Nulo(ConversorSqlite.Texto(despesa.PagaEm)))
        ];

    public void Excluir(DespesaId id) =>
        _executor.Executar("DELETE FROM despesas WHERE id = $id", "Não foi possível excluir despesa", ("$id", id.Valor.ToString()));

    private static Despesa Mapear(SqliteDataReader leitor) =>
        Despesa.Restaurar(
            new DespesaId(Guid.Parse(leitor.GetString(leitor.GetOrdinal("id")))),
            new BemId(Guid.Parse(leitor.GetString(leitor.GetOrdinal("bem_id")))),
            ConversorSqlite.EnumDeTexto<TipoDespesa>(leitor.GetString(leitor.GetOrdinal("tipo"))),
            leitor.GetString(leitor.GetOrdinal("descricao")),
            ConversorSqlite.Data(leitor, "vencimento")!.Value,
            ConversorSqlite.LerDinheiro(leitor, "valor", "moeda")!,
            ConversorSqlite.Data(leitor, "paga_em"));
}
