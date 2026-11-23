using Assetly.Dominio.Backup;
using Shouldly;

namespace Assetly.Testes.Dominio;

public class ConfiguracaoBackupTests
{
    [Fact]
    public void DeveRejeitarHabilitarAutoBackupSemDestino()
    {
        var configuracao = ConfiguracaoBackup.Padrao();

        Should.Throw<InvalidOperationException>(() => configuracao.HabilitarAutoBackup(FrequenciaBackup.Diaria));
    }

    [Fact]
    public void DeveIndicarBackupDevidoQuandoNuncaExecutado()
    {
        var configuracao = ConfiguracaoBackup.Padrao();
        configuracao.DefinirDestino(@"D:\Backups\Assetly");
        configuracao.HabilitarAutoBackup(FrequenciaBackup.Diaria);

        configuracao.DeveExecutarAutoBackup(DateTimeOffset.Now).ShouldBeTrue();
    }

    [Fact]
    public void DeveRespeitarIntervaloDaFrequencia()
    {
        var agora = new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);
        var configuracao = ConfiguracaoBackup.Restaurar(@"D:\Backups\Assetly", true, FrequenciaBackup.Semanal, agora.AddDays(-3));

        configuracao.DeveExecutarAutoBackup(agora).ShouldBeFalse();
        configuracao.DeveExecutarAutoBackup(agora.AddDays(5)).ShouldBeTrue();
    }

    [Fact]
    public void NaoDeveExecutarAutoBackupQuandoDesabilitado()
    {
        var configuracao = ConfiguracaoBackup.Restaurar(@"D:\Backups\Assetly", false, FrequenciaBackup.Diaria, null);

        configuracao.DeveExecutarAutoBackup(DateTimeOffset.Now).ShouldBeFalse();
    }

    [Fact]
    public void DeveDesabilitarAutoBackup()
    {
        var configuracao = ConfiguracaoBackup.Padrao();
        configuracao.DefinirDestino(@"D:\Backups\Assetly");
        configuracao.HabilitarAutoBackup(FrequenciaBackup.Mensal);

        configuracao.DesabilitarAutoBackup();

        configuracao.AutoBackupHabilitado.ShouldBeFalse();
        configuracao.DeveExecutarAutoBackup(DateTimeOffset.Now).ShouldBeFalse();
    }
}
