using Assetly.Dominio.Compartilhado;

namespace Assetly.Dominio.Backup;

/// <summary>Configuração de backup local — uma por ambiente (produção e teste guardam a sua própria,
/// já que cada um já usa banco e pasta de documentos totalmente isolados).</summary>
public sealed class ConfiguracaoBackup
{
    public string? CaminhoDestino { get; private set; }
    public bool AutoBackupHabilitado { get; private set; }
    public FrequenciaBackup Frequencia { get; private set; }
    public DateTimeOffset? UltimoBackupEm { get; private set; }

    /// <summary>Backups protegidos por senha (arquivo único criptografado com AES-256).</summary>
    public bool Criptografado { get; private set; }

    /// <summary>Senha dos backups protegida pelo Windows (DPAPI, só este usuário deste computador a lê) —
    /// necessária para o backup automático rodar sem perguntar a senha.</summary>
    public string? SenhaProtegida { get; private set; }

    /// <summary>Quantos backups manter no destino: depois de cada backup, os mais antigos são apagados.
    /// 0 = manter todos.</summary>
    public int ManterUltimos { get; private set; }

    public const int ManterUltimosPadrao = 10;
    public const int ManterUltimosMaximo = 365;

    private ConfiguracaoBackup(string? caminhoDestino, bool autoBackupHabilitado, FrequenciaBackup frequencia, DateTimeOffset? ultimoBackupEm)
    {
        CaminhoDestino = caminhoDestino;
        AutoBackupHabilitado = autoBackupHabilitado;
        Frequencia = frequencia;
        UltimoBackupEm = ultimoBackupEm;
    }

    public static ConfiguracaoBackup Padrao() => new(null, false, FrequenciaBackup.Diaria, null) { ManterUltimos = ManterUltimosPadrao };

    public static ConfiguracaoBackup Restaurar(string? caminhoDestino, bool autoBackupHabilitado, FrequenciaBackup frequencia, DateTimeOffset? ultimoBackupEm,
        bool criptografado = false, string? senhaProtegida = null, int manterUltimos = 0) =>
        new(caminhoDestino, autoBackupHabilitado, frequencia, ultimoBackupEm)
        {
            Criptografado = criptografado && !string.IsNullOrEmpty(senhaProtegida),
            SenhaProtegida = senhaProtegida,
            ManterUltimos = Math.Clamp(manterUltimos, 0, ManterUltimosMaximo)
        };

    /// <summary>Define quantos backups manter no destino (0 = todos).</summary>
    public void DefinirRetencao(int manterUltimos)
    {
        if (manterUltimos is < 0 or > ManterUltimosMaximo)
        {
            throw new ArgumentException($"a quantidade de backups mantidos deve ser de 0 (todos) a {ManterUltimosMaximo}.");
        }

        ManterUltimos = manterUltimos;
    }

    public void AtivarCriptografia(string senhaProtegida)
    {
        SenhaProtegida = Validacao.ExigirTexto(senhaProtegida, "senha protegida", 4000);
        Criptografado = true;
    }

    public void DesativarCriptografia()
    {
        Criptografado = false;
        SenhaProtegida = null;
    }

    public void DefinirDestino(string caminhoDestino)
    {
        CaminhoDestino = Validacao.ExigirTexto(caminhoDestino, "destino do backup", 500);
    }

    public void HabilitarAutoBackup(FrequenciaBackup frequencia)
    {
        ExigirDestinoConfigurado();
        AutoBackupHabilitado = true;
        Frequencia = frequencia;
    }

    public void DesabilitarAutoBackup()
    {
        AutoBackupHabilitado = false;
    }

    public void RegistrarBackupConcluido(DateTimeOffset momento)
    {
        UltimoBackupEm = momento;
    }

    /// <summary>Um backup automático é devido quando está habilitado, o destino está configurado, e o
    /// intervalo da frequência escolhida já passou desde o último backup (ou nunca houve um).</summary>
    public bool DeveExecutarAutoBackup(DateTimeOffset agora)
    {
        if (!AutoBackupHabilitado || string.IsNullOrWhiteSpace(CaminhoDestino))
        {
            return false;
        }

        return UltimoBackupEm is null || agora - UltimoBackupEm.Value >= IntervaloDaFrequencia();
    }

    private TimeSpan IntervaloDaFrequencia() => Frequencia switch
    {
        FrequenciaBackup.Semanal => TimeSpan.FromDays(7),
        FrequenciaBackup.Mensal => TimeSpan.FromDays(30),
        _ => TimeSpan.FromDays(1)
    };

    private void ExigirDestinoConfigurado()
    {
        if (string.IsNullOrWhiteSpace(CaminhoDestino))
        {
            throw new InvalidOperationException("defina o destino do backup antes de habilitar o backup automático");
        }
    }
}
