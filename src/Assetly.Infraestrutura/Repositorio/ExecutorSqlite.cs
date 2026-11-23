using Assetly.Infraestrutura.Banco;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

/// <summary>Consulta/comando com tratamento de erro padronizado — usado pelos repositórios mais novos
/// (veículos, imóveis, abastecimentos, despesas) para não repetir o mesmo try/catch em cada método.</summary>
internal sealed class ExecutorSqlite(FabricaConexaoSqlite fabricaConexao)
{
    private readonly FabricaConexaoSqlite _fabricaConexao = fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao));

    internal List<T> Consultar<T>(string sql, Func<SqliteDataReader, T> mapear, string descricaoErro, params (string Nome, object Valor)[] parametros)
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = Criar(conexao, sql, parametros);
            using var leitor = comando.ExecuteReader();
            var itens = new List<T>();
            while (leitor.Read())
            {
                itens.Add(mapear(leitor));
            }

            return itens;
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados(descricaoErro, excecao);
        }
    }

    internal void Executar(string sql, string descricaoErro, params (string Nome, object Valor)[] parametros)
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = Criar(conexao, sql, parametros);
            comando.ExecuteNonQuery();
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados(descricaoErro, excecao);
        }
    }

    private static SqliteCommand Criar(SqliteConnection conexao, string sql, (string Nome, object Valor)[] parametros)
    {
        var comando = conexao.CreateCommand();
        comando.CommandText = sql;
        foreach (var (nome, valor) in parametros)
        {
            comando.Parameters.AddWithValue(nome, valor);
        }

        return comando;
    }
}
