using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Backup;
using Assetly.Infraestrutura.Banco;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioConfiguracaoBackupSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioConfiguracaoBackup
{
    private readonly ExecutorSqlite _executor = new(fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao), "fábrica de conexão é obrigatória"));

    public ConfiguracaoBackup Obter() =>
        _executor.Consultar("SELECT * FROM configuracao_backup WHERE id = 1", MapeadorConfiguracaoBackup.Mapear,
            "Não foi possível obter a configuração de backup").FirstOrDefault() ?? ConfiguracaoBackup.Padrao();

    public void Salvar(ConfiguracaoBackup configuracao)
    {
        const string sql = """
            INSERT INTO configuracao_backup (id, caminho_destino, auto_backup_habilitado, frequencia, ultimo_backup_em, criptografado, senha_protegida, manter_ultimos)
            VALUES (1, $caminhoDestino, $autoBackupHabilitado, $frequencia, $ultimoBackupEm, $criptografado, $senhaProtegida, $manterUltimos)
            ON CONFLICT(id) DO UPDATE SET
                caminho_destino = excluded.caminho_destino,
                auto_backup_habilitado = excluded.auto_backup_habilitado,
                frequencia = excluded.frequencia,
                ultimo_backup_em = excluded.ultimo_backup_em,
                criptografado = excluded.criptografado,
                senha_protegida = excluded.senha_protegida,
                manter_ultimos = excluded.manter_ultimos
            """;
        _executor.Executar(sql, "Não foi possível salvar a configuração de backup",
            ("$caminhoDestino", ConversorSqlite.Nulo(configuracao.CaminhoDestino)),
            ("$autoBackupHabilitado", configuracao.AutoBackupHabilitado ? 1 : 0),
            ("$frequencia", ConversorSqlite.EnumParaTexto(configuracao.Frequencia)),
            ("$ultimoBackupEm", ConversorSqlite.Nulo(ConversorSqlite.Texto(configuracao.UltimoBackupEm))),
            ("$criptografado", configuracao.Criptografado ? 1 : 0),
            ("$senhaProtegida", ConversorSqlite.Nulo(configuracao.SenhaProtegida)),
            ("$manterUltimos", configuracao.ManterUltimos));
    }
}
