using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Saude;
using Assetly.Infraestrutura.Banco;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioSaudeSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioSaude
{
    private readonly FabricaConexaoSqlite _fabricaConexao = fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao), "fábrica de conexão é obrigatória");

    public Saude? BuscarPorId(SaudeId id)
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = "SELECT * FROM saude WHERE id = $id";
            comando.Parameters.AddWithValue("$id", id.Valor.ToString());
            using var resultado = comando.ExecuteReader();
            return resultado.Read() ? MapeadorSaude.Mapear(resultado) : null;
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível buscar registro de saúde", excecao);
        }
    }

    public IReadOnlyList<Saude> Listar() =>
        Consultar("SELECT * FROM saude ORDER BY termina_em, fornecedor", null);

    public IReadOnlyList<Saude> ListarPorBem(BemId bemId) =>
        Consultar("SELECT * FROM saude WHERE bem_id = $bemId ORDER BY termina_em, fornecedor", bemId);

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
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = sql;
            comando.Parameters.AddWithValue("$id", saude.Id.Valor.ToString());
            comando.Parameters.AddWithValue("$bemId", saude.BemId.Valor.ToString());
            comando.Parameters.AddWithValue("$tipo", ConversorSqlite.EnumParaTexto(saude.Tipo));
            comando.Parameters.AddWithValue("$fornecedor", saude.Fornecedor);
            comando.Parameters.AddWithValue("$iniciaEm", ConversorSqlite.Texto(saude.Periodo.IniciaEm)!);
            comando.Parameters.AddWithValue("$terminaEm", ConversorSqlite.Texto(saude.Periodo.TerminaEm)!);
            comando.Parameters.AddWithValue("$contato", saude.Contato);
            comando.Parameters.AddWithValue("$dataCancelamento", (object?)ConversorSqlite.Texto(saude.DataCancelamento) ?? DBNull.Value);
            comando.ExecuteNonQuery();
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível salvar registro de saúde", excecao);
        }
    }

    private List<Saude> Consultar(string sql, BemId? bemId)
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
            var registros = new List<Saude>();
            while (resultado.Read())
            {
                registros.Add(MapeadorSaude.Mapear(resultado));
            }

            return registros;
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível listar registros de saúde", excecao);
        }
    }

    public void Excluir(SaudeId id)
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = "DELETE FROM saude WHERE id = $id";
            comando.Parameters.AddWithValue("$id", id.Valor.ToString());
            comando.ExecuteNonQuery();
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível excluir registro de saúde", excecao);
        }
    }
}
