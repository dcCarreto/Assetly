using Assetly.Dominio.Backup;

namespace Assetly.Aplicacao.CasoDeUso;

/// <summary>Aviso de backup mostrado no painel. <see cref="Critico"/>: os dados não estão protegidos agora.</summary>
public sealed record AvisoBackup(string Mensagem, bool Critico);

/// <summary>Insiste no backup fora do módulo Backup: o valor do Assetly está nos dados do usuário.</summary>
public static class LembreteBackup
{
    public const int DiasSemBackup = 30;

    /// <param name="temDados">Instalação vazia não precisa de lembrete.</param>
    /// <param name="falhaAutomatica">Mensagem da última falha do backup automático nesta sessão, se houver.</param>
    public static AvisoBackup? Avaliar(ConfiguracaoBackup configuracao, DateTimeOffset agora, bool temDados, string? falhaAutomatica)
    {
        ArgumentNullException.ThrowIfNull(configuracao);
        if (falhaAutomatica is not null && configuracao.AutoBackupHabilitado)
        {
            return new AvisoBackup($"O backup automático está falhando: {falhaAutomatica}", true);
        }

        if (!temDados)
        {
            return null;
        }

        if (configuracao.UltimoBackupEm is not { } ultimo)
        {
            return new AvisoBackup("Você ainda não fez nenhum backup. Se este computador falhar, seus cadastros e documentos se perdem.", true);
        }

        var dias = (int)(agora - ultimo).TotalDays;
        return dias > DiasSemBackup
            ? new AvisoBackup($"O último backup foi há {dias} dias. Faça um novo ou ative o backup automático.", false)
            : null;
    }
}
