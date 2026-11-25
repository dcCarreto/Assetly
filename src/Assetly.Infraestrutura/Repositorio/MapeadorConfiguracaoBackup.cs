using Assetly.Dominio.Backup;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

internal static class MapeadorConfiguracaoBackup
{
    internal static ConfiguracaoBackup Mapear(SqliteDataReader leitor)
    {
        var indiceDestino = leitor.GetOrdinal("caminho_destino");
        var caminhoDestino = leitor.IsDBNull(indiceDestino) ? null : leitor.GetString(indiceDestino);
        var autoBackupHabilitado = leitor.GetInt64(leitor.GetOrdinal("auto_backup_habilitado")) != 0;
        var frequencia = ConversorSqlite.EnumDeTexto<FrequenciaBackup>(leitor.GetString(leitor.GetOrdinal("frequencia")));
        var ultimoBackupEm = ConversorSqlite.DataHora(leitor, "ultimo_backup_em");
        var criptografado = leitor.GetInt64(leitor.GetOrdinal("criptografado")) != 0;
        var senhaProtegida = ConversorSqlite.TextoColuna(leitor, "senha_protegida");
        var manterUltimos = (int)leitor.GetInt64(leitor.GetOrdinal("manter_ultimos"));
        return ConfiguracaoBackup.Restaurar(caminhoDestino, autoBackupHabilitado, frequencia, ultimoBackupEm, criptografado,
            senhaProtegida.Length == 0 ? null : senhaProtegida, manterUltimos);
    }
}
