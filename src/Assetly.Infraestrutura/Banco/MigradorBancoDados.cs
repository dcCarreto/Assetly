using System.Reflection;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Banco;

public sealed class MigradorBancoDados
{
    private static readonly IReadOnlyList<Migracao> Migracoes =
    [
        new Migracao(1, "V001__estrutura_inicial.sql", "Assetly.Infraestrutura.Banco.Migracoes.V001__estrutura_inicial.sql")
    ];

    private readonly FabricaConexaoSqlite _fabricaConexao;

    public MigradorBancoDados(FabricaConexaoSqlite fabricaConexao)
    {
        _fabricaConexao = fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao), "fábrica de conexão é obrigatória");
    }

    public void Migrar()
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            CriarTabelaMigracoes(conexao);
            foreach (var migracao in Migracoes)
            {
                if (!FoiAplicada(conexao, migracao.Versao))
                {
                    Aplicar(conexao, migracao);
                }
            }
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível migrar o banco de dados", excecao);
        }
    }

    private static void CriarTabelaMigracoes(SqliteConnection conexao)
    {
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            CREATE TABLE IF NOT EXISTS schema_migrations (
                versao INTEGER PRIMARY KEY,
                nome TEXT NOT NULL,
                aplicada_em TEXT NOT NULL
            )
            """;
        comando.ExecuteNonQuery();
    }

    private static bool FoiAplicada(SqliteConnection conexao, int versao)
    {
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT 1 FROM schema_migrations WHERE versao = $versao";
        comando.Parameters.AddWithValue("$versao", versao);
        using var resultado = comando.ExecuteReader();
        return resultado.Read();
    }

    private static void Aplicar(SqliteConnection conexao, Migracao migracao)
    {
        using var transacao = conexao.BeginTransaction();
        try
        {
            ExecutarScript(conexao, CarregarScript(migracao.Recurso));
            RegistrarMigracao(conexao, migracao);
            transacao.Commit();
        }
        catch (Exception excecao)
        {
            transacao.Rollback();
            throw new ExcecaoBancoDados($"Não foi possível aplicar a migração {migracao.Nome}", excecao);
        }
    }

    private static string CarregarScript(string recurso)
    {
        using var entrada = Assembly.GetExecutingAssembly().GetManifestResourceStream(recurso)
            ?? throw new IOException($"Recurso não encontrado: {recurso}");
        using var leitor = new StreamReader(entrada);
        var linhas = leitor.ReadToEnd()
            .Split('\n')
            .Where(linha => !linha.TrimStart().StartsWith("--", StringComparison.Ordinal));
        return string.Join(Environment.NewLine, linhas);
    }

    private static void ExecutarScript(SqliteConnection conexao, string script)
    {
        using var comando = conexao.CreateCommand();
        foreach (var instrucao in script.Split(';'))
        {
            var sql = instrucao.Trim();
            if (sql.Length > 0)
            {
                comando.CommandText = sql;
                comando.ExecuteNonQuery();
            }
        }
    }

    private static void RegistrarMigracao(SqliteConnection conexao, Migracao migracao)
    {
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            INSERT INTO schema_migrations (versao, nome, aplicada_em)
            VALUES ($versao, $nome, $aplicadaEm)
            """;
        comando.Parameters.AddWithValue("$versao", migracao.Versao);
        comando.Parameters.AddWithValue("$nome", migracao.Nome);
        comando.Parameters.AddWithValue("$aplicadaEm", DateTimeOffset.UtcNow.ToString("O"));
        comando.ExecuteNonQuery();
    }

    private sealed record Migracao(int Versao, string Nome, string Recurso);
}
