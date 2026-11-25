using Assetly.Aplicacao.CasoDeUso;
using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Backup;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Garantia;
using Shouldly;

namespace Assetly.Testes.Aplicacao;

/// <summary>Retenção de backups no destino e limpeza de alertas automáticos antigos.</summary>
public class RetencaoLimpezaTests
{
    private static readonly DateOnly Hoje = new(2026, 11, 25);

    // ---------- Retenção no destino ----------

    [Fact]
    public void ConfiguracaoNovaMantemOsUltimos10ERecusaValorInvalido()
    {
        var configuracao = ConfiguracaoBackup.Padrao();
        configuracao.ManterUltimos.ShouldBe(ConfiguracaoBackup.ManterUltimosPadrao);

        configuracao.DefinirRetencao(0);
        configuracao.ManterUltimos.ShouldBe(0);
        Should.Throw<ArgumentException>(() => configuracao.DefinirRetencao(-1));
        Should.Throw<ArgumentException>(() => configuracao.DefinirRetencao(ConfiguracaoBackup.ManterUltimosMaximo + 1));
    }

    [Fact]
    public void BackupManualEAutomaticoApagamOsAntigosDepoisDeConcluir()
    {
        var repositorio = new RepositorioConfiguracaoBackupMemoria();
        new DefinirDestinoBackup(repositorio).Executar(new DefinirDestinoBackupComando(@"E:\Backups"));
        new DefinirRetencaoBackup(repositorio).Executar(5);
        var servico = new ServicoQueRegistraRetencao();

        new ExecutarBackupAgora(repositorio, servico, @"C:\banco.sqlite3", @"C:\documentos").Executar();
        servico.Retencoes.ShouldBe([(@"E:\Backups", 5)]);

        new HabilitarAutoBackup(repositorio).Executar(new HabilitarAutoBackupComando(FrequenciaBackup.Diaria));
        new VerificarEExecutarBackupAutomatico(repositorio, servico, @"C:\banco.sqlite3", @"C:\documentos").Executar(DateTimeOffset.Now.AddDays(2));
        servico.Retencoes.Count.ShouldBe(2);
    }

    [Fact]
    public void ComRetencaoZeroNenhumBackupEhApagado()
    {
        var repositorio = new RepositorioConfiguracaoBackupMemoria();
        new DefinirDestinoBackup(repositorio).Executar(new DefinirDestinoBackupComando(@"E:\Backups"));
        new DefinirRetencaoBackup(repositorio).Executar(0);
        var servico = new ServicoQueRegistraRetencao();

        new ExecutarBackupAgora(repositorio, servico, @"C:\banco.sqlite3", @"C:\documentos").Executar();

        servico.Retencoes.ShouldBeEmpty();
    }

    [Fact]
    public void BackupQueFalhaNaoApagaNada()
    {
        var repositorio = new RepositorioConfiguracaoBackupMemoria();
        new DefinirDestinoBackup(repositorio).Executar(new DefinirDestinoBackupComando(@"E:\Backups"));
        var servico = new ServicoQueRegistraRetencao { Falhar = true };

        Should.Throw<InvalidOperationException>(() => new ExecutarBackupAgora(repositorio, servico, @"C:\banco.sqlite3", @"C:\documentos").Executar());

        servico.Retencoes.ShouldBeEmpty();
    }

    // ---------- Limpeza de alertas ----------

    [Fact]
    public void AlertasAutomaticosResolvidosAntigosSemSituacaoAtualSaoApagados()
    {
        var bens = new RepositorioBemMemoria();
        var garantias = new RepositorioGarantiaMemoria();
        var alertas = new RepositorioAlertaMemoria();
        var bem = Bem.Criar(new NomeBem("Notebook"), TipoBem.Pessoa); // pessoa: sem alerta de nota fiscal
        bens.Salvar(bem);
        // Garantia vencendo agora: a situação existe, o alerta (mesmo resolvido pelo usuário) precisa ficar.
        var garantia = Garantia.Criar(bem.Id, TipoGarantia.Fabricante, "Fábrica", Hoje.AddYears(-1), Hoje.AddDays(10));
        garantias.Salvar(garantia);

        Alerta Resolvido(string mensagem, DateOnly prazo, bool automatico = true, Guid? origem = null)
        {
            var alerta = Alerta.Restaurar(AlertaId.Novo(), bem.Id, TipoAlerta.ContaVencida, SeveridadeAlerta.Aviso, mensagem,
                prazo, prazo, StatusAlerta.Resolvido, automatico, origem);
            alertas.Salvar(alerta);
            return alerta;
        }

        var antigo = Resolvido("Conta vencida em 01/01/2026: Energia.", Hoje.AddDays(-GerarAlertasAutomaticos.DiasRetencaoResolvidos - 1));
        var recente = Resolvido("Conta vencida em 01/11/2026: Energia.", Hoje.AddDays(-10));
        var manual = Resolvido("Lembrete antigo criado à mão.", Hoje.AddYears(-1), automatico: false);
        var aindaExigido = Alerta.Restaurar(AlertaId.Novo(), bem.Id, TipoAlerta.GarantiaVencendo, SeveridadeAlerta.Aviso,
            $"Garantia vence em {Hoje.AddDays(10).ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture)}.",
            Hoje.AddYears(-1), Hoje.AddYears(-1), StatusAlerta.Resolvido, automatico: true, garantia.Id.Valor);
        alertas.Salvar(aindaExigido);

        new GerarAlertasAutomaticos(bens, garantias, new RepositorioManutencaoMemoria(), new RepositorioDocumentoMemoria(), alertas).Executar(Hoje);

        var restantes = alertas.Listar().Select(a => a.Id).ToList();
        restantes.ShouldNotContain(antigo.Id);
        restantes.ShouldContain(recente.Id);
        restantes.ShouldContain(manual.Id);
        restantes.ShouldContain(aindaExigido.Id);
        // O alerta resolvido pelo usuário não volta a abrir.
        alertas.Listar().Count(a => a.Tipo == TipoAlerta.GarantiaVencendo).ShouldBe(1);
    }

    private sealed class ServicoQueRegistraRetencao : IServicoBackup
    {
        internal bool Falhar { get; init; }

        internal List<(string, int)> Retencoes { get; } = [];

        public string ExecutarBackup(string arquivoBanco, string diretorioDocumentos, string diretorioDestino, string? senha = null) =>
            Falhar ? throw new InvalidOperationException("pendrive desconectado") : Path.Combine(diretorioDestino, "Assetly-Backup-novo");

        public void RestaurarBackup(string caminhoBackup, string arquivoBanco, string diretorioDocumentos, string? senha = null) =>
            throw new NotSupportedException();

        public void GerarArquivo(string arquivoBanco, string diretorioDocumentos, string arquivoDestino, string? senha) =>
            throw new NotSupportedException();

        public void ManterUltimos(string diretorio, int quantidade) => Retencoes.Add((diretorio, quantidade));
    }

    private sealed class RepositorioConfiguracaoBackupMemoria : IRepositorioConfiguracaoBackup
    {
        private ConfiguracaoBackup _configuracao = ConfiguracaoBackup.Padrao();

        public ConfiguracaoBackup Obter() => _configuracao;

        public void Salvar(ConfiguracaoBackup configuracao) => _configuracao = configuracao;
    }
}
