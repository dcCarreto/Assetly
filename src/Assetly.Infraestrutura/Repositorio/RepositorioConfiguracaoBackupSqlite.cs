using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Backup;
using Assetly.Infraestrutura.Banco;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioConfiguracaoBackupSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioConfiguracaoBackup
{
    private readonly FabricaConexaoSqlite _fabricaConexao = fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao), "fábrica de conexão é obrigatória");

    public ConfiguracaoBackup Obter()
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = "SELECT * FROM configuracao_backup WHERE id = 1";
            using var resultado = comando.ExecuteReader();
            return resultado.Read() ? MapeadorConfiguracaoBackup.Mapear(resultado) : ConfiguracaoBackup.Padrao();
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível obter a configuração de backup", excecao);
        }
    }

    public void Salvar(ConfiguracaoBackup configuracao)
    {
        const string sql = """
            INSERT INTO configuracao_backup (id, caminho_destino, auto_backup_habilitado, frequencia, ultimo_backup_em, criptografado, senha_protegida)
            VALUES (1, $caminhoDestino, $autoBackupHabilitado, $frequencia, $ultimoBackupEm, $criptografado, $senhaProtegida)
            ON CONFLICT(id) DO UPDATE SET
                caminho_destino = excluded.caminho_destino,
                auto_backup_habilitado = excluded.auto_backup_habilitado,
                frequencia = excluded.frequencia,
                ultimo_backup_em = excluded.ultimo_backup_em,
                criptografado = excluded.criptografado,
                senha_protegida = excluded.senha_protegida
            """;
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = sql;
            comando.Parameters.AddWithValue("$caminhoDestino", (object?)configuracao.CaminhoDestino ?? DBNull.Value);
            comando.Parameters.AddWithValue("$autoBackupHabilitado", configuracao.AutoBackupHabilitado ? 1 : 0);
            comando.Parameters.AddWithValue("$frequencia", ConversorSqlite.EnumParaTexto(configuracao.Frequencia));
            comando.Parameters.AddWithValue("$ultimoBackupEm", (object?)ConversorSqlite.Texto(configuracao.UltimoBackupEm) ?? DBNull.Value);
            comando.Parameters.AddWithValue("$criptografado", configuracao.Criptografado ? 1 : 0);
            comando.Parameters.AddWithValue("$senhaProtegida", ConversorSqlite.Nulo(configuracao.SenhaProtegida));
            comando.ExecuteNonQuery();
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível salvar a configuração de backup", excecao);
        }
    }
}
