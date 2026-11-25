using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Infraestrutura.Banco;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioAlertaSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioAlerta
{
    private const string Ordem = "ORDER BY criado_em DESC, mensagem";

    private const string SqlSalvar = """
        INSERT INTO alertas (id, bem_id, tipo, severidade, mensagem, criado_em, prazo_em, status, automatico, origem_id)
        VALUES ($id, $bemId, $tipo, $severidade, $mensagem, $criadoEm, $prazoEm, $status, $automatico, $origemId)
        ON CONFLICT(id) DO UPDATE SET
            bem_id = excluded.bem_id,
            tipo = excluded.tipo,
            severidade = excluded.severidade,
            mensagem = excluded.mensagem,
            criado_em = excluded.criado_em,
            prazo_em = excluded.prazo_em,
            status = excluded.status,
            automatico = excluded.automatico,
            origem_id = excluded.origem_id
        """;

    private readonly ExecutorSqlite _executor = new(fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao), "fábrica de conexão é obrigatória"));

    public Alerta? BuscarPorId(AlertaId id) =>
        _executor.Consultar("SELECT * FROM alertas WHERE id = $id", MapeadorAlerta.Mapear, "Não foi possível buscar alerta",
            ("$id", id.Valor.ToString())).FirstOrDefault();

    public IReadOnlyList<Alerta> Listar() =>
        _executor.Consultar($"SELECT * FROM alertas {Ordem}", MapeadorAlerta.Mapear, "Não foi possível listar alertas");

    public IReadOnlyList<Alerta> ListarPorBem(BemId bemId) =>
        _executor.Consultar($"SELECT * FROM alertas WHERE bem_id = $bemId {Ordem}", MapeadorAlerta.Mapear, "Não foi possível listar alertas",
            ("$bemId", bemId.Valor.ToString()));

    public void Salvar(Alerta alerta) =>
        _executor.Executar(SqlSalvar, "Não foi possível salvar alerta", Parametros(alerta));

    /// <summary>Usado pelo motor de alertas: todas as criações e resoluções de uma rodada numa transação só.</summary>
    public void SalvarTodos(IEnumerable<Alerta> alertas) =>
        _executor.ExecutarEmLote(SqlSalvar, "Não foi possível salvar os alertas", alertas.Select(Parametros));

    public void Excluir(AlertaId id) =>
        _executor.Executar("DELETE FROM alertas WHERE id = $id", "Não foi possível excluir alerta", ("$id", id.Valor.ToString()));

    public void ExcluirTodos(IEnumerable<AlertaId> ids) =>
        _executor.ExecutarEmLote("DELETE FROM alertas WHERE id = $id", "Não foi possível excluir os alertas",
            ids.Select(id => new (string, object)[] { ("$id", id.Valor.ToString()) }));

    private static (string Nome, object Valor)[] Parametros(Alerta alerta) =>
        [
            ("$id", alerta.Id.Valor.ToString()),
            ("$bemId", alerta.BemId.Valor.ToString()),
            ("$tipo", ConversorSqlite.EnumParaTexto(alerta.Tipo)),
            ("$severidade", ConversorSqlite.EnumParaTexto(alerta.Severidade)),
            ("$mensagem", alerta.Mensagem),
            ("$criadoEm", ConversorSqlite.Texto(alerta.CriadoEm)!),
            ("$prazoEm", ConversorSqlite.Nulo(ConversorSqlite.Texto(alerta.PrazoEm))),
            ("$status", ConversorSqlite.EnumParaTexto(alerta.Status)),
            ("$automatico", alerta.Automatico ? 1 : 0),
            ("$origemId", ConversorSqlite.Nulo(alerta.OrigemId?.ToString()))
        ];
}
