using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Saude;
using Assetly.Infraestrutura.Banco;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioSaudeSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioSaude
{
    private const string Ordem = "ORDER BY termina_em, fornecedor";
    private readonly ExecutorSqlite _executor = new(fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao), "fábrica de conexão é obrigatória"));

    public Saude? BuscarPorId(SaudeId id) =>
        _executor.Consultar("SELECT * FROM saude WHERE id = $id", MapeadorSaude.Mapear, "Não foi possível buscar registro de saúde",
            ("$id", id.Valor.ToString())).FirstOrDefault();

    public IReadOnlyList<Saude> Listar() =>
        _executor.Consultar($"SELECT * FROM saude {Ordem}", MapeadorSaude.Mapear, "Não foi possível listar registros de saúde");

    public IReadOnlyList<Saude> ListarPorBem(BemId bemId) =>
        _executor.Consultar($"SELECT * FROM saude WHERE bem_id = $bemId {Ordem}", MapeadorSaude.Mapear, "Não foi possível listar registros de saúde",
            ("$bemId", bemId.Valor.ToString()));

    public void Salvar(Saude saude)
    {
        const string sql = """
            INSERT INTO saude (id, bem_id, tipo, fornecedor, inicia_em, termina_em, contato, data_cancelamento)
            VALUES ($id, $bemId, $tipo, $fornecedor, $iniciaEm, $terminaEm, $contato, $dataCancelamento)
            ON CONFLICT(id) DO UPDATE SET
                bem_id = excluded.bem_id,
                tipo = excluded.tipo,
                fornecedor = excluded.fornecedor,
                inicia_em = excluded.inicia_em,
                termina_em = excluded.termina_em,
                contato = excluded.contato,
                data_cancelamento = excluded.data_cancelamento
            """;
        _executor.Executar(sql, "Não foi possível salvar registro de saúde",
            ("$id", saude.Id.Valor.ToString()),
            ("$bemId", saude.BemId.Valor.ToString()),
            ("$tipo", ConversorSqlite.EnumParaTexto(saude.Tipo)),
            ("$fornecedor", saude.Fornecedor),
            ("$iniciaEm", ConversorSqlite.Texto(saude.Periodo.IniciaEm)!),
            ("$terminaEm", ConversorSqlite.Texto(saude.Periodo.TerminaEm)!),
            ("$contato", saude.Contato),
            ("$dataCancelamento", ConversorSqlite.Nulo(ConversorSqlite.Texto(saude.DataCancelamento))));
    }

    public void Excluir(SaudeId id) =>
        _executor.Executar("DELETE FROM saude WHERE id = $id", "Não foi possível excluir registro de saúde", ("$id", id.Valor.ToString()));
}
