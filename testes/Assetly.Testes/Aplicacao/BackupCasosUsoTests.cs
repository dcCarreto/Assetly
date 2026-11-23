using Assetly.Aplicacao.CasoDeUso;
using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;
using Assetly.Dominio.Backup;
using Shouldly;

namespace Assetly.Testes.Aplicacao;

public class BackupCasosUsoTests
{
    [Fact]
    public void DeveDefinirDestinoEHabilitarAutoBackup()
    {
        var repositorio = new RepositorioConfiguracaoBackupMemoria();

        new DefinirDestinoBackup(repositorio).Executar(new DefinirDestinoBackupComando(@"D:\Backups\Assetly"));
        var configuracao = new HabilitarAutoBackup(repositorio).Executar(new HabilitarAutoBackupComando(FrequenciaBackup.Semanal));

        configuracao.CaminhoDestino.ShouldBe(@"D:\Backups\Assetly");
        configuracao.AutoBackupHabilitado.ShouldBeTrue();
        configuracao.Frequencia.ShouldBe(FrequenciaBackup.Semanal);
        new ObterConfiguracaoBackup(repositorio).Executar().AutoBackupHabilitado.ShouldBeTrue();
    }

    [Fact]
    public void DeveFalharAoExecutarBackupSemDestinoConfigurado()
    {
        var repositorio = new RepositorioConfiguracaoBackupMemoria();
        var servico = new ServicoBackupFalso();
        var casoDeUso = new ExecutarBackupAgora(repositorio, servico, @"C:\banco.sqlite3", @"C:\documentos");

        Should.Throw<InvalidOperationException>(() => casoDeUso.Executar());
        servico.Chamadas.ShouldBe(0);
    }

    [Fact]
    public void DeveExecutarBackupERegistrarUltimoBackup()
    {
        var repositorio = new RepositorioConfiguracaoBackupMemoria();
        new DefinirDestinoBackup(repositorio).Executar(new DefinirDestinoBackupComando(@"D:\Backups\Assetly"));
        var servico = new ServicoBackupFalso();
        var casoDeUso = new ExecutarBackupAgora(repositorio, servico, @"C:\banco.sqlite3", @"C:\documentos");

        var caminhoGerado = casoDeUso.Executar();

        caminhoGerado.ShouldBe(servico.CaminhoRetornado);
        servico.Chamadas.ShouldBe(1);
        new ObterConfiguracaoBackup(repositorio).Executar().UltimoBackupEm.ShouldNotBeNull();
    }

    [Fact]
    public void VerificacaoAutomaticaNaoExecutaAntesDoIntervaloVencer()
    {
        var repositorio = new RepositorioConfiguracaoBackupMemoria();
        var agora = DateTimeOffset.Now;
        new DefinirDestinoBackup(repositorio).Executar(new DefinirDestinoBackupComando(@"D:\Backups\Assetly"));
        new HabilitarAutoBackup(repositorio).Executar(new HabilitarAutoBackupComando(FrequenciaBackup.Diaria));
        var servico = new ServicoBackupFalso();
        var casoDeUso = new VerificarEExecutarBackupAutomatico(repositorio, servico, @"C:\banco.sqlite3", @"C:\documentos");

        casoDeUso.Executar(agora).ShouldBeTrue();
        servico.Chamadas.ShouldBe(1);

        casoDeUso.Executar(agora.AddHours(2)).ShouldBeFalse();
        servico.Chamadas.ShouldBe(1);

        casoDeUso.Executar(agora.AddDays(2)).ShouldBeTrue();
        servico.Chamadas.ShouldBe(2);
    }

    private sealed class RepositorioConfiguracaoBackupMemoria : IRepositorioConfiguracaoBackup
    {
        private ConfiguracaoBackup _configuracao = ConfiguracaoBackup.Padrao();

        public ConfiguracaoBackup Obter() => _configuracao;

        public void Salvar(ConfiguracaoBackup configuracao) => _configuracao = configuracao;
    }

    private sealed class ServicoBackupFalso : IServicoBackup
    {
        internal int Chamadas { get; private set; }

        internal string CaminhoRetornado { get; } = @"D:\Backups\Assetly\Assetly-Backup-2026-01-01";

        internal string? UltimaSenha { get; private set; }

        public string ExecutarBackup(string arquivoBanco, string diretorioDocumentos, string diretorioDestino, string? senha = null)
        {
            Chamadas++;
            UltimaSenha = senha;
            return CaminhoRetornado;
        }

        public void RestaurarBackup(string caminhoInstantaneo, string arquivoBanco, string diretorioDocumentos, string? senha = null) =>
            throw new NotSupportedException();

        public void GerarArquivo(string arquivoBanco, string diretorioDocumentos, string arquivoDestino, string? senha) =>
            throw new NotSupportedException();
    }
}
