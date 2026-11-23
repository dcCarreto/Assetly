using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Banco;

public sealed class FabricaConexaoSqlite
{
    private readonly string _textoConexao;

    public string ArquivoBanco { get; }

    public FabricaConexaoSqlite(string arquivoBanco)
    {
        ArgumentNullException.ThrowIfNull(arquivoBanco);
        ArquivoBanco = Path.GetFullPath(arquivoBanco);
        // Construído pelo builder (e não por interpolação) para que caminhos com ';' ou '=' — possíveis
        // em nomes de usuário/pastas do Windows — não quebrem a string de conexão.
        _textoConexao = new SqliteConnectionStringBuilder { DataSource = ArquivoBanco }.ToString();
    }

    public SqliteConnection Abrir()
    {
        try
        {
            var diretorio = Path.GetDirectoryName(ArquivoBanco);
            if (!string.IsNullOrEmpty(diretorio))
            {
                Directory.CreateDirectory(diretorio);
            }

            var conexao = new SqliteConnection(_textoConexao);
            conexao.Open();
            Configurar(conexao);
            return conexao;
        }
        catch (Exception excecao) when (excecao is IOException or SqliteException or UnauthorizedAccessException)
        {
            throw new ExcecaoBancoDados("Não foi possível abrir o banco SQLite", excecao);
        }
    }

    private static void Configurar(SqliteConnection conexao)
    {
        using var comando = conexao.CreateCommand();
        comando.CommandText = "PRAGMA foreign_keys = ON";
        comando.ExecuteNonQuery();
        comando.CommandText = "PRAGMA busy_timeout = 5000";
        comando.ExecuteNonQuery();
    }
}
