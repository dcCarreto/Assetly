using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Saude;
using Assetly.Infraestrutura.Banco;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioGastoSaudeSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioGastoSaude
{
    private readonly FabricaConexaoSqlite _fabricaConexao = fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao), "fábrica de conexão é obrigatória");

    public GastoSaude? BuscarPorId(GastoSaudeId id)
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = "SELECT * FROM gastos_saude WHERE id = $id";
            comando.Parameters.AddWithValue("$id", id.Valor.ToString());
            using var resultado = comando.ExecuteReader();
            return resultado.Read() ? MapeadorGastoSaude.Mapear(resultado) : null;
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível buscar gasto de saúde", excecao);
        }
    }

    public IReadOnlyList<GastoSaude> Listar() =>
        Consultar("SELECT * FROM gastos_saude ORDER BY data DESC, descricao", null);

    public IReadOnlyList<GastoSaude> ListarPorBem(BemId bemId) =>
        Consultar("SELECT * FROM gastos_saude WHERE bem_id = $bemId ORDER BY data DESC, descricao", bemId);

    public void Salvar(GastoSaude gastoSaude)
    {
        const string sql = """
            INSERT INTO gastos_saude (id, bem_id, tipo, descricao, data, valor, moeda)
            VALUES ($id, $bemId, $tipo, $descricao, $data, $valor, $moeda)
            ON CONFLICT(id) DO UPDATE SET
                bem_id = excluded.bem_id,
                tipo = excluded.tipo,
                descricao = excluded.descricao,
                data = excluded.data,
                valor = excluded.valor,
                moeda = excluded.moeda
            """;
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = sql;
            comando.Parameters.AddWithValue("$id", gastoSaude.Id.Valor.ToString());
            comando.Parameters.AddWithValue("$bemId", gastoSaude.BemId.Valor.ToString());
            comando.Parameters.AddWithValue("$tipo", ConversorSqlite.EnumParaTexto(gastoSaude.Tipo));
            comando.Parameters.AddWithValue("$descricao", gastoSaude.Descricao);
            comando.Parameters.AddWithValue("$data", ConversorSqlite.Texto(gastoSaude.Data)!);
            comando.Parameters.AddWithValue("$valor", ConversorSqlite.Valor(gastoSaude.Valor)!);
            comando.Parameters.AddWithValue("$moeda", ConversorSqlite.Moeda(gastoSaude.Valor)!);
            comando.ExecuteNonQuery();
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível salvar gasto de saúde", excecao);
        }
    }

    private List<GastoSaude> Consultar(string sql, BemId? bemId)
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = sql;
            if (bemId is not null)
            {
                comando.Parameters.AddWithValue("$bemId", bemId.Value.Valor.ToString());
            }

            using var resultado = comando.ExecuteReader();
            var registros = new List<GastoSaude>();
            while (resultado.Read())
            {
                registros.Add(MapeadorGastoSaude.Mapear(resultado));
            }

            return registros;
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível listar gastos de saúde", excecao);
        }
    }

    public void Excluir(GastoSaudeId id)
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = "DELETE FROM gastos_saude WHERE id = $id";
            comando.Parameters.AddWithValue("$id", id.Valor.ToString());
            comando.ExecuteNonQuery();
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível excluir gasto de saúde", excecao);
        }
    }
}
