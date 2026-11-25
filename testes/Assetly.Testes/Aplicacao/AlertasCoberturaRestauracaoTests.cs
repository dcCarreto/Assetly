using System.Globalization;
using Assetly.Aplicacao.CasoDeUso;
using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Excecao;
using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Backup;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Despesa;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Saude;
using Shouldly;

namespace Assetly.Testes.Aplicacao;

/// <summary>Motor de alertas (cobertura por garantia estendida/convênio renovado, origem do alerta) e restauração
/// que devolve o estado anterior quando falha no meio.</summary>
public class AlertasCoberturaRestauracaoTests
{
    private static readonly DateOnly Hoje = new(2026, 11, 25);

    private readonly RepositorioBemMemoria _bens = new();
    private readonly RepositorioGarantiaMemoria _garantias = new();
    private readonly RepositorioSaudeMemoria _convenios = new();
    private readonly RepositorioDespesaMemoria _despesas = new();
    private readonly RepositorioDocumentoMemoria _documentos = new();
    private readonly RepositorioAlertaMemoria _alertas = new();

    private GerarAlertasAutomaticos Gerador() =>
        new(_bens, _garantias, new RepositorioManutencaoMemoria(), _documentos, _alertas, _convenios,
            new RepositorioVeiculoMemoria(), new RepositorioImovelMemoria(), _despesas);

    private Bem NovoBem(string nome, TipoBem tipo = TipoBem.Eletronico)
    {
        var bem = Bem.Criar(new NomeBem(nome), tipo);
        _bens.Salvar(bem);
        return bem;
    }

    private IEnumerable<Alerta> Abertos(TipoAlerta tipo) => _alertas.Listar().Where(a => a.Tipo == tipo && a.EstaAcionavel());

    // ---------- Garantia estendida e convênio renovado ----------

    [Fact]
    public void GarantiaVencidaCobertaPorGarantiaEstendidaNaoGeraAlerta()
    {
        var camera = NovoBem("Câmera");
        _garantias.Salvar(Garantia.Criar(camera.Id, TipoGarantia.Fabricante, "Fábrica", Hoje.AddYears(-1), Hoje.AddDays(-5)));
        var estendida = Garantia.Criar(camera.Id, TipoGarantia.Estendida, "Loja", Hoje.AddDays(-4), Hoje.AddYears(1));
        _garantias.Salvar(estendida);

        Gerador().Executar(Hoje);
        Abertos(TipoAlerta.GarantiaVencida).ShouldBeEmpty();

        // Sem a estendida, a vencida volta a alertar — como aviso, não como crítico.
        _garantias.Excluir(estendida.Id);
        Gerador().Executar(Hoje);
        Abertos(TipoAlerta.GarantiaVencida).Single().Severidade.ShouldBe(SeveridadeAlerta.Aviso);
    }

    [Fact]
    public void GarantiaVencendoSemLacunaParaAProximaNaoGeraAlerta()
    {
        var notebook = NovoBem("Notebook");
        var fim = Hoje.AddDays(10);
        _garantias.Salvar(Garantia.Criar(notebook.Id, TipoGarantia.Fabricante, "Fábrica", Hoje.AddYears(-1), fim));
        _garantias.Salvar(Garantia.Criar(notebook.Id, TipoGarantia.Estendida, "Loja", fim.AddDays(1), fim.AddYears(2)));

        Gerador().Executar(Hoje);

        Abertos(TipoAlerta.GarantiaVencendo).ShouldBeEmpty();
    }

    [Fact]
    public void GarantiaQueComecaDepoisDeUmaLacunaNaoEscondeOVencimento()
    {
        var notebook = NovoBem("Notebook");
        var fim = Hoje.AddDays(10);
        _garantias.Salvar(Garantia.Criar(notebook.Id, TipoGarantia.Fabricante, "Fábrica", Hoje.AddYears(-1), fim));
        _garantias.Salvar(Garantia.Criar(notebook.Id, TipoGarantia.Estendida, "Loja", fim.AddDays(30), fim.AddYears(2)));

        Gerador().Executar(Hoje);

        Abertos(TipoAlerta.GarantiaVencendo).Count().ShouldBe(1);
    }

    [Fact]
    public void ConvenioRenovadoDoMesmoTipoNaoGeraAlertaDeVencido()
    {
        var titular = NovoBem("Ana", TipoBem.Pessoa);
        _convenios.Salvar(Saude.Criar(titular.Id, TipoSaude.Convenio, "Plano A", Hoje.AddYears(-1), Hoje.AddDays(-1), null));
        _convenios.Salvar(Saude.Criar(titular.Id, TipoSaude.Convenio, "Plano A", Hoje, Hoje.AddYears(1), null));
        // Outro tipo (farmácia) não renova o plano de saúde.
        _convenios.Salvar(Saude.Criar(titular.Id, TipoSaude.Farmacia, "Farmácia", Hoje.AddYears(-1), Hoje.AddDays(-2), null));
        _convenios.Salvar(Saude.Criar(titular.Id, TipoSaude.Ortodontia, "Odonto", Hoje, Hoje.AddYears(1), null));

        Gerador().Executar(Hoje);

        Abertos(TipoAlerta.ConvenioVencido).Single().Mensagem.ShouldContain("Farmácia");
    }

    // ---------- Origem do alerta ----------

    [Fact]
    public void DuasContasComAMesmaDescricaoGeramAlertasDistintos()
    {
        var carro = NovoBem("Carro", TipoBem.Veiculo);
        var registrar = new RegistrarDespesa(_bens, _despesas);
        var primeira = registrar.Executar(new RegistrarDespesaComando(carro.Id, TipoDespesa.Multa, "Multa", Hoje.AddDays(-3), 130m, "BRL", 1)).Single();
        registrar.Executar(new RegistrarDespesaComando(carro.Id, TipoDespesa.Multa, "Multa", Hoje.AddDays(-3), 195m, "BRL", 1));

        Gerador().Executar(Hoje);
        Abertos(TipoAlerta.ContaVencida).Count().ShouldBe(2);

        // Pagar uma não resolve o alerta da outra.
        new PagarDespesa(_despesas).Executar(primeira.Id, Hoje);
        Gerador().Executar(Hoje);
        var restante = Abertos(TipoAlerta.ContaVencida).Single();
        restante.OrigemId.ShouldNotBe(primeira.Id.Valor);
    }

    [Fact]
    public void AlertaGravadoAntesDaOrigemExistirNaoEhDuplicado()
    {
        var camera = NovoBem("Câmera");
        var garantia = Garantia.Criar(camera.Id, TipoGarantia.Fabricante, "Fábrica", Hoje.AddYears(-1), Hoje.AddDays(10));
        _garantias.Salvar(garantia);
        var antigo = Alerta.Restaurar(AlertaId.Novo(), camera.Id, TipoAlerta.GarantiaVencendo, SeveridadeAlerta.Aviso,
            $"Garantia vence em {Hoje.AddDays(10).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}.", Hoje.AddDays(-1), Hoje.AddDays(10), StatusAlerta.Ciente, automatico: true);
        _alertas.Salvar(antigo);

        Gerador().Executar(Hoje).ShouldNotContain(a => a.Tipo == TipoAlerta.GarantiaVencendo);

        _alertas.Listar().Single(a => a.Tipo == TipoAlerta.GarantiaVencendo).ShouldBeSameAs(antigo);
        antigo.Status.ShouldBe(StatusAlerta.Ciente);
    }

    // ---------- Restauração ----------

    [Fact]
    public void RestauracaoInterrompidaDevolveOEstadoAnterior()
    {
        var servico = new ServicoBackupRoteirizado { FalharNaPrimeiraRestauracao = true };
        var bancoPreparado = 0;
        var restaurar = CriarRestauracao(servico, () => bancoPreparado++);

        var excecao = Should.Throw<InvalidOperationException>(() => restaurar.Executar(@"E:\backup.assetly", "senha"));

        excecao.Message.ShouldContain("voltaram ao estado anterior");
        servico.Restauracoes.ShouldBe(new List<(string, string?)> { (@"E:\backup.assetly", "senha"), (ServicoBackupRoteirizado.CopiaSeguranca, null) });
        bancoPreparado.ShouldBe(1);
        servico.Retencao.ShouldBe((@"C:\dados\seguranca", RestaurarBackup.CopiasSegurancaRetidas));
    }

    [Fact]
    public void FalhaAntesDeTocarNosDadosNaoDisparaODesfazer()
    {
        var servico = new ServicoBackupRoteirizado { RecusarComoValidacao = true };
        var restaurar = CriarRestauracao(servico, () => { });

        Should.Throw<InvalidOperationException>(() => restaurar.Executar(@"E:\backup.assetly", "errada")).Message.ShouldContain("senha incorreta");

        servico.Restauracoes.Count.ShouldBe(1);
    }

    [Fact]
    public void CopiaDeSegurancaUsaASenhaDosBackups()
    {
        var servico = new ServicoBackupRoteirizado();
        var configuracao = new RepositorioConfiguracaoBackupMemoria();
        var protecao = new ProtecaoTransparente();
        new AtivarCriptografiaBackup(configuracao, protecao).Executar("senha-dos-backups", "senha-dos-backups");

        new RestaurarBackup(configuracao, _documentos, servico, new ArmazenamentoQueRegistra(), () => { },
            @"C:\dados\banco\assetly.sqlite3", @"C:\dados\documentos", @"C:\dados\seguranca", protecaoSenha: protecao).Executar(@"E:\backup");

        servico.SenhaDaCopia.ShouldBe("senha-dos-backups");
    }

    [Fact]
    public void RestauracaoApagaArquivosQueNenhumDocumentoUsa()
    {
        var armazenamento = new ArmazenamentoQueRegistra();
        var restaurar = new RestaurarBackup(new RepositorioConfiguracaoBackupMemoria(), _documentos, new ServicoBackupRoteirizado(), armazenamento,
            () => { }, @"C:\dados\banco\assetly.sqlite3", @"C:\dados\documentos", @"C:\dados\seguranca");

        restaurar.Executar(@"E:\backup");

        armazenamento.LimpezasComReferencias.Count.ShouldBe(1);
    }

    private RestaurarBackup CriarRestauracao(IServicoBackup servico, Action prepararBanco) =>
        new(new RepositorioConfiguracaoBackupMemoria(), _documentos, servico, new ArmazenamentoQueRegistra(), prepararBanco,
            @"C:\dados\banco\assetly.sqlite3", @"C:\dados\documentos", @"C:\dados\seguranca");

    private sealed class ServicoBackupRoteirizado : IServicoBackup
    {
        public ResultadoTesteBackup TestarBackup(string caminhoBackup, string diretorioDocumentos, string? senha) => throw new NotSupportedException();

        internal const string CopiaSeguranca = @"C:\dados\seguranca\Assetly-Backup-2026-11-25_120000";

        internal bool FalharNaPrimeiraRestauracao { get; init; }

        internal bool RecusarComoValidacao { get; init; }

        internal List<(string Caminho, string? Senha)> Restauracoes { get; } = [];

        internal (string Diretorio, int Quantidade) Retencao { get; private set; }

        internal string? SenhaDaCopia { get; private set; }

        public string ExecutarBackup(string arquivoBanco, string diretorioDocumentos, string diretorioDestino, string? senha = null)
        {
            SenhaDaCopia = senha;
            return CopiaSeguranca;
        }

        public void RestaurarBackup(string caminhoBackup, string arquivoBanco, string diretorioDocumentos, string? senha = null)
        {
            Restauracoes.Add((caminhoBackup, senha));
            if (RecusarComoValidacao)
            {
                throw new InvalidOperationException("senha incorreta (ou arquivo de backup alterado).");
            }

            if (FalharNaPrimeiraRestauracao && Restauracoes.Count == 1)
            {
                throw new RestauracaoIncompletaExcecao("a restauração foi interrompida no meio: disco cheio", new IOException("disco cheio"));
            }
        }

        public void ManterUltimos(string diretorio, int quantidade) => Retencao = (diretorio, quantidade);

        public void GerarArquivo(string arquivoBanco, string diretorioDocumentos, string arquivoDestino, string? senha) =>
            throw new NotSupportedException();
    }

    private sealed class ArmazenamentoQueRegistra : IArmazenamentoDocumento
    {
        internal List<IReadOnlyCollection<string>> LimpezasComReferencias { get; } = [];

        public string Armazenar(string arquivoOrigem) => arquivoOrigem;

        public int RemoverNaoReferenciados(IReadOnlyCollection<string> caminhosReferenciados)
        {
            LimpezasComReferencias.Add(caminhosReferenciados);
            return 0;
        }
    }

    private sealed class ProtecaoTransparente : IProtecaoSenha
    {
        public string Proteger(string segredo) => "p:" + segredo;

        public string Revelar(string protegido) => protegido[2..];
    }

    private sealed class RepositorioConfiguracaoBackupMemoria : IRepositorioConfiguracaoBackup
    {
        private ConfiguracaoBackup _configuracao = ConfiguracaoBackup.Padrao();

        public ConfiguracaoBackup Obter() => _configuracao;

        public void Salvar(ConfiguracaoBackup configuracao) => _configuracao = configuracao;
    }
}
