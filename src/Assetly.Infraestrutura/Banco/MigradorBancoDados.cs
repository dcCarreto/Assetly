using System.Reflection;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Banco;

public sealed class MigradorBancoDados
{
    private static readonly IReadOnlyList<Migracao> Migracoes =
    [
        new Migracao(1, "V001__estrutura_inicial.sql", "Assetly.Infraestrutura.Banco.Migracoes.V001__estrutura_inicial.sql"),
        new Migracao(2, "V002__saude.sql", "Assetly.Infraestrutura.Banco.Migracoes.V002__saude.sql"),
        new Migracao(3, "V003__configuracao_backup.sql", "Assetly.Infraestrutura.Banco.Migracoes.V003__configuracao_backup.sql"),
        new Migracao(4, "V004__configuracao_preferencias.sql", "Assetly.Infraestrutura.Banco.Migracoes.V004__configuracao_preferencias.sql"),
        new Migracao(5, "V005__alertas_automaticos.sql", "Assetly.Infraestrutura.Banco.Migracoes.V005__alertas_automaticos.sql"),
        new Migracao(6, "V006__veiculos_imoveis.sql", "Assetly.Infraestrutura.Banco.Migracoes.V006__veiculos_imoveis.sql"),
        new Migracao(7, "V007__atualizacao_automatica.sql", "Assetly.Infraestrutura.Banco.Migracoes.V007__atualizacao_automatica.sql"),
        new Migracao(8, "V008__familia_notas_criptografia_sincronizacao.sql", "Assetly.Infraestrutura.Banco.Migracoes.V008__familia_notas_criptografia_sincronizacao.sql")
    ];

    private const string PrefixoCopiaSeguranca = "antes-da-atualizacao-";
    private const int CopiasSegurancaRetidas = 5;

    private readonly FabricaConexaoSqlite _fabricaConexao;
    private readonly string? _diretorioCopiaSeguranca;

    /// <summary>Versão de esquema mais recente que esta versão do programa conhece.</summary>
    public static int VersaoMaisRecente => Migracoes.Max(migracao => migracao.Versao);

    /// <param name="diretorioCopiaSeguranca">Onde guardar uma cópia do banco antes de aplicar migrações
    /// num banco já existente (ou seja, ao abrir pela primeira vez uma versão nova do programa).
    /// Nulo desliga a cópia.</param>
    public MigradorBancoDados(FabricaConexaoSqlite fabricaConexao, string? diretorioCopiaSeguranca = null)
    {
        _fabricaConexao = fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao), "fábrica de conexão é obrigatória");
        _diretorioCopiaSeguranca = diretorioCopiaSeguranca;
    }

    public void Migrar() => Migrar(VersaoMaisRecente);

    /// <summary>Aplica as migrações só até <paramref name="ateVersao"/> — usado nos testes para criar um banco
    /// idêntico ao de uma versão anterior do programa.</summary>
    internal void Migrar(int ateVersao)
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            CriarTabelaMigracoes(conexao);
            var aplicadas = VersoesAplicadas(conexao);
            if (aplicadas.Count > 0 && aplicadas.Max() > VersaoMaisRecente)
            {
                throw new ExcecaoBancoDados(
                    "Estes dados foram gravados por uma versão mais nova do Assetly. Atualize o programa para abri-los " +
                    "(nada foi alterado no banco).");
            }

            var pendentes = Migracoes.Where(migracao => !aplicadas.Contains(migracao.Versao) && migracao.Versao <= ateVersao).ToList();
            if (pendentes.Count > 0 && aplicadas.Count > 0 && _diretorioCopiaSeguranca is not null)
            {
                GuardarCopiaSeguranca(conexao, aplicadas.Max());
            }

            foreach (var migracao in pendentes)
            {
                Aplicar(conexao, migracao);
            }
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível migrar o banco de dados", excecao);
        }
    }

    /// <summary>Uma atualização do programa que mude a estrutura do banco nunca deve arriscar os dados: antes
    /// das migrações, o banco é copiado (API de backup do SQLite). Mantém as últimas cópias.</summary>
    private void GuardarCopiaSeguranca(SqliteConnection conexao, int versaoAtual)
    {
        Directory.CreateDirectory(_diretorioCopiaSeguranca!);
        var arquivo = Path.Combine(_diretorioCopiaSeguranca!, $"{PrefixoCopiaSeguranca}v{versaoAtual:D3}-{DateTime.Now:yyyy-MM-dd_HHmmss}.sqlite3");
        using (var destino = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = arquivo, Pooling = false }.ToString()))
        {
            destino.Open();
            conexao.BackupDatabase(destino);
        }

        foreach (var antiga in Directory.EnumerateFiles(_diretorioCopiaSeguranca!, $"{PrefixoCopiaSeguranca}*.sqlite3")
                     .OrderByDescending(caminho => caminho, StringComparer.Ordinal)
                     .Skip(CopiasSegurancaRetidas))
        {
            File.Delete(antiga);
        }
    }

    private static HashSet<int> VersoesAplicadas(SqliteConnection conexao)
    {
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT versao FROM schema_migrations";
        using var leitor = comando.ExecuteReader();
        var versoes = new HashSet<int>();
        while (leitor.Read())
        {
            versoes.Add(leitor.GetInt32(0));
        }

        return versoes;
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
