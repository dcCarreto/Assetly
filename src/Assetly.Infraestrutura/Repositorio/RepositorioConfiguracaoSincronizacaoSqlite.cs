using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Backup;
using Assetly.Infraestrutura.Banco;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioConfiguracaoSincronizacaoSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioConfiguracaoSincronizacao
{
    private readonly ExecutorSqlite _executor = new(fabricaConexao);

    public ConfiguracaoSincronizacao Obter() =>
        _executor.Consultar("SELECT * FROM configuracao_sincronizacao WHERE id = 1", Mapear, "Não foi possível obter a configuração de sincronização")
            .FirstOrDefault() ?? ConfiguracaoSincronizacao.Padrao();

    public void Salvar(ConfiguracaoSincronizacao configuracao)
    {
        const string sql = """
            INSERT INTO configuracao_sincronizacao (id, pasta, ativa, ultima_impressao, ultima_sincronizacao_em)
            VALUES (1, $pasta, $ativa, $ultimaImpressao, $ultimaSincronizacaoEm)
            ON CONFLICT(id) DO UPDATE SET
                pasta = excluded.pasta, ativa = excluded.ativa,
                ultima_impressao = excluded.ultima_impressao, ultima_sincronizacao_em = excluded.ultima_sincronizacao_em
            """;
        _executor.Executar(sql, "Não foi possível salvar a configuração de sincronização",
            ("$pasta", ConversorSqlite.Nulo(configuracao.Pasta)),
            ("$ativa", configuracao.Ativa ? 1 : 0),
            ("$ultimaImpressao", ConversorSqlite.Nulo(configuracao.UltimaImpressao)),
            ("$ultimaSincronizacaoEm", ConversorSqlite.Nulo(ConversorSqlite.Texto(configuracao.UltimaSincronizacaoEm))));
    }

    private static ConfiguracaoSincronizacao Mapear(SqliteDataReader leitor)
    {
        string? Texto(string coluna) => ConversorSqlite.TextoColuna(leitor, coluna) is { Length: > 0 } valor ? valor : null;
        return ConfiguracaoSincronizacao.Restaurar(
            Texto("pasta"),
            leitor.GetInt64(leitor.GetOrdinal("ativa")) != 0,
            Texto("ultima_impressao"),
            ConversorSqlite.DataHora(leitor, "ultima_sincronizacao_em"));
    }
}
