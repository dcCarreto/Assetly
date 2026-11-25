using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Saude;
using Assetly.Infraestrutura.Banco;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioGastoSaudeSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioGastoSaude
{
    private const string Ordem = "ORDER BY data DESC, descricao";
    private readonly ExecutorSqlite _executor = new(fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao), "fábrica de conexão é obrigatória"));

    public GastoSaude? BuscarPorId(GastoSaudeId id) =>
        _executor.Consultar("SELECT * FROM gastos_saude WHERE id = $id", MapeadorGastoSaude.Mapear, "Não foi possível buscar gasto de saúde",
            ("$id", id.Valor.ToString())).FirstOrDefault();

    public IReadOnlyList<GastoSaude> Listar() =>
        _executor.Consultar($"SELECT * FROM gastos_saude {Ordem}", MapeadorGastoSaude.Mapear, "Não foi possível listar gastos de saúde");

    public IReadOnlyList<GastoSaude> ListarPorBem(BemId bemId) =>
        _executor.Consultar($"SELECT * FROM gastos_saude WHERE bem_id = $bemId {Ordem}", MapeadorGastoSaude.Mapear, "Não foi possível listar gastos de saúde",
            ("$bemId", bemId.Valor.ToString()));

    public void Salvar(GastoSaude gastoSaude)
    {
        const string sql = """
            INSERT INTO gastos_saude (id, bem_id, tipo, descricao, data, valor, moeda, prestador, documento_prestador)
            VALUES ($id, $bemId, $tipo, $descricao, $data, $valor, $moeda, $prestador, $documentoPrestador)
            ON CONFLICT(id) DO UPDATE SET
                bem_id = excluded.bem_id,
                tipo = excluded.tipo,
                descricao = excluded.descricao,
                data = excluded.data,
                valor = excluded.valor,
                moeda = excluded.moeda,
                prestador = excluded.prestador,
                documento_prestador = excluded.documento_prestador
            """;
        _executor.Executar(sql, "Não foi possível salvar gasto de saúde",
            ("$id", gastoSaude.Id.Valor.ToString()),
            ("$bemId", gastoSaude.BemId.Valor.ToString()),
            ("$tipo", ConversorSqlite.EnumParaTexto(gastoSaude.Tipo)),
            ("$descricao", gastoSaude.Descricao),
            ("$data", ConversorSqlite.Texto(gastoSaude.Data)!),
            ("$valor", ConversorSqlite.Valor(gastoSaude.Valor)!),
            ("$moeda", ConversorSqlite.Moeda(gastoSaude.Valor)!),
            ("$prestador", gastoSaude.Prestador),
            ("$documentoPrestador", gastoSaude.DocumentoPrestador));
    }

    public void Excluir(GastoSaudeId id) =>
        _executor.Executar("DELETE FROM gastos_saude WHERE id = $id", "Não foi possível excluir gasto de saúde", ("$id", id.Valor.ToString()));
}
