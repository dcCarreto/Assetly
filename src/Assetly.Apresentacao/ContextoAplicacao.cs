using System.IO;
using Assetly.Aplicacao.CasoDeUso;
using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;
using Assetly.Apresentacao.Semeadura;
using Assetly.Infraestrutura.Armazenamento;
using Assetly.Infraestrutura.Banco;
using Assetly.Infraestrutura.Repositorio;
using Assetly.Infraestrutura.Seguranca;
using Assetly.Infraestrutura.Sincronizacao;

namespace Assetly.Apresentacao;

public sealed class ContextoAplicacao
{
    private readonly BancoDadosAssetly _bancoDados;
    private readonly IArmazenamentoDocumento _armazenamentoDocumento;
    private readonly IServicoBackup _servicoBackup = new ServicoBackupLocal();
    private readonly IProtecaoSenha _protecaoSenha = new ProtecaoSenhaWindows();
    private readonly string _diretorioSeguranca;

    public string RotuloAmbiente { get; }

    public bool AmbienteTeste { get; }

    /// <summary>Arquivo SQLite do ambiente atual (produção ou teste).</summary>
    public string ArquivoBanco { get; }

    /// <summary>Pasta privada onde os documentos do ambiente atual são copiados.</summary>
    public string DiretorioDocumentos { get; }

    /// <summary>Disparado depois de qualquer alteração de dados feita pela interface — a tela principal
    /// usa para atualizar na hora todos os módulos, contadores e o painel (e não só no próximo ciclo).</summary>
    public event Action? DadosAlterados;

    public IRepositorioBem RepositorioBem { get; }

    public IRepositorioGarantia RepositorioGarantia { get; }

    public IRepositorioManutencao RepositorioManutencao { get; }

    public IRepositorioDocumento RepositorioDocumento { get; }

    public IRepositorioAlerta RepositorioAlerta { get; }

    public IRepositorioSaude RepositorioSaude { get; }

    public IRepositorioGastoSaude RepositorioGastoSaude { get; }

    public IRepositorioVeiculo RepositorioVeiculo { get; }

    public IRepositorioAbastecimento RepositorioAbastecimento { get; }

    public IRepositorioImovel RepositorioImovel { get; }

    public IRepositorioDespesa RepositorioDespesa { get; }

    public IRepositorioConfiguracaoBackup RepositorioConfiguracaoBackup { get; }

    public IRepositorioConfiguracaoPreferencias RepositorioConfiguracaoPreferencias { get; }

    public IRepositorioConfiguracaoSincronizacao RepositorioConfiguracaoSincronizacao { get; }

    private ContextoAplicacao(
        BancoDadosAssetly bancoDados,
        string diretorioDocumentos,
        string diretorioSeguranca,
        string rotuloAmbiente,
        bool ambienteTeste,
        bool semearDadosFicticios)
    {
        _bancoDados = bancoDados ?? throw new ArgumentNullException(nameof(bancoDados), "banco de dados é obrigatório");
        _bancoDados.Inicializar();
        var fabricaConexao = _bancoDados.FabricaConexao;
        RepositorioBem = new RepositorioBemSqlite(fabricaConexao);
        RepositorioGarantia = new RepositorioGarantiaSqlite(fabricaConexao);
        RepositorioManutencao = new RepositorioManutencaoSqlite(fabricaConexao);
        RepositorioDocumento = new RepositorioDocumentoSqlite(fabricaConexao);
        RepositorioAlerta = new RepositorioAlertaSqlite(fabricaConexao);
        RepositorioSaude = new RepositorioSaudeSqlite(fabricaConexao);
        RepositorioGastoSaude = new RepositorioGastoSaudeSqlite(fabricaConexao);
        RepositorioVeiculo = new RepositorioVeiculoSqlite(fabricaConexao);
        RepositorioAbastecimento = new RepositorioAbastecimentoSqlite(fabricaConexao);
        RepositorioImovel = new RepositorioImovelSqlite(fabricaConexao);
        RepositorioDespesa = new RepositorioDespesaSqlite(fabricaConexao);
        RepositorioConfiguracaoBackup = new RepositorioConfiguracaoBackupSqlite(fabricaConexao);
        RepositorioConfiguracaoPreferencias = new RepositorioConfiguracaoPreferenciasSqlite(fabricaConexao);
        RepositorioConfiguracaoSincronizacao = new RepositorioConfiguracaoSincronizacaoSqlite(fabricaConexao);
        RotuloAmbiente = rotuloAmbiente ?? throw new ArgumentNullException(nameof(rotuloAmbiente), "rótulo do ambiente é obrigatório");
        AmbienteTeste = ambienteTeste;
        ArquivoBanco = fabricaConexao.ArquivoBanco;
        DiretorioDocumentos = diretorioDocumentos;
        _diretorioSeguranca = diretorioSeguranca;
        _armazenamentoDocumento = new ArmazenamentoDocumentoLocal(diretorioDocumentos);

        if (semearDadosFicticios)
        {
            DadosFicticiosAplicacao.PopularSeNecessario(this, DiretorioDocumentos);
        }
    }

    public static ContextoAplicacao LocalPadrao() =>
        new(BancoDadosAssetly.LocalPadrao(), CaminhosArmazenamentoLocal.DiretorioDocumentos(), CaminhosArmazenamentoLocal.DiretorioSeguranca(),
            "Produção local", ambienteTeste: false, semearDadosFicticios: false);

    public static ContextoAplicacao TesteComDadosFicticios() =>
        new(BancoDadosAssetly.LocalTeste(), CaminhosArmazenamentoLocal.DiretorioDocumentosTeste(), CaminhosArmazenamentoLocal.DiretorioSegurancaTeste(),
            "Teste", ambienteTeste: true, semearDadosFicticios: true);

    /// <summary>Ambiente descartável numa pasta qualquer (testes de release): nunca toca nos dados do usuário.</summary>
    public static ContextoAplicacao Isolado(string pasta, bool semearDadosFicticios) =>
        new(new BancoDadosAssetly(new FabricaConexaoSqlite(Path.Combine(pasta, "banco", "assetly.sqlite3")), Path.Combine(pasta, "seguranca")),
            Path.Combine(pasta, "documentos"), Path.Combine(pasta, "seguranca"), "Autoteste", ambienteTeste: true, semearDadosFicticios);

    public FonteDados FonteDados() =>
        new(RepositorioBem, RepositorioGarantia, RepositorioManutencao, RepositorioDocumento, RepositorioAlerta, RepositorioSaude,
            RepositorioGastoSaude, RepositorioVeiculo, RepositorioAbastecimento, RepositorioImovel, RepositorioDespesa);

    public ExportarDados ExportarDados() => new(FonteDados());

    public CalcularPatrimonio CalcularPatrimonio() => new(RepositorioBem);

    public AtivarCriptografiaBackup AtivarCriptografiaBackup() => new(RepositorioConfiguracaoBackup, _protecaoSenha);

    public DesativarCriptografiaBackup DesativarCriptografiaBackup() => new(RepositorioConfiguracaoBackup);

    public ConfigurarSincronizacao ConfigurarSincronizacao() => new(RepositorioConfiguracaoSincronizacao);

    public SincronizarDados SincronizarDados() =>
        new(RepositorioConfiguracaoSincronizacao, RepositorioConfiguracaoBackup, ExportarDados(), new SincronizacaoPastaLocal(_servicoBackup),
            RestaurarBackup(), _protecaoSenha, ArquivoBanco, DiretorioDocumentos, Environment.MachineName);

    public void NotificarAlteracao() => DadosAlterados?.Invoke();

    public CadastrarBem CadastrarBem() => new(RepositorioBem);

    public EditarBem EditarBem() => new(RepositorioBem);

    public BuscarBem BuscarBem() => new(RepositorioBem);

    public ListarBens ListarBens() => new(RepositorioBem);

    public AlterarStatusBem AlterarStatusBem() => new(RepositorioBem);

    public ExcluirBem ExcluirBem() => new(RepositorioBem, RepositorioDocumento, _armazenamentoDocumento);

    public RegistrarGarantia RegistrarGarantia() => new(RepositorioBem, RepositorioGarantia);

    public EditarGarantia EditarGarantia() => new(RepositorioGarantia);

    public ExcluirGarantia ExcluirGarantia() => new(RepositorioGarantia);

    public ListarGarantias ListarGarantias() => new(RepositorioGarantia);

    public ListarGarantiasDoBem ListarGarantiasDoBem() => new(RepositorioBem, RepositorioGarantia);

    public AgendarManutencao AgendarManutencao() => new(RepositorioBem, RepositorioManutencao);

    public EditarManutencao EditarManutencao() => new(RepositorioManutencao);

    public ConcluirManutencao ConcluirManutencao() => new(RepositorioManutencao);

    public CancelarManutencao CancelarManutencao() => new(RepositorioManutencao);

    public ExcluirManutencao ExcluirManutencao() => new(RepositorioManutencao);

    public ListarManutencoes ListarManutencoes() => new(RepositorioManutencao);

    public ListarManutencoesDoBem ListarManutencoesDoBem() => new(RepositorioBem, RepositorioManutencao);

    public RegistrarDocumento RegistrarDocumento() => new(RepositorioBem, RepositorioDocumento, _armazenamentoDocumento);

    public EditarDocumento EditarDocumento() => new(RepositorioDocumento, _armazenamentoDocumento);

    public MarcarDocumentoAusente MarcarDocumentoAusente() => new(RepositorioDocumento);

    public ArquivarDocumento ArquivarDocumento() => new(RepositorioDocumento);

    public ExcluirDocumento ExcluirDocumento() => new(RepositorioDocumento, _armazenamentoDocumento);

    public ListarDocumentos ListarDocumentos() => new(RepositorioDocumento);

    public ListarDocumentosDoBem ListarDocumentosDoBem() => new(RepositorioBem, RepositorioDocumento);

    public CriarAlerta CriarAlerta() => new(RepositorioBem, RepositorioAlerta);

    public GerarAlertasAutomaticos GerarAlertasAutomaticos() =>
        new(RepositorioBem, RepositorioGarantia, RepositorioManutencao, RepositorioDocumento, RepositorioAlerta, RepositorioSaude,
            RepositorioVeiculo, RepositorioImovel, RepositorioDespesa);

    public TomarCienciaAlerta TomarCienciaAlerta() => new(RepositorioAlerta);

    public ResolverAlerta ResolverAlerta() => new(RepositorioAlerta);

    public ReabrirAlerta ReabrirAlerta() => new(RepositorioAlerta);

    public ExcluirAlerta ExcluirAlerta() => new(RepositorioAlerta);

    public ListarAlertas ListarAlertas() => new(RepositorioAlerta);

    public ListarAlertasDoBem ListarAlertasDoBem() => new(RepositorioBem, RepositorioAlerta);

    public RegistrarSaude RegistrarSaude() => new(RepositorioBem, RepositorioSaude);

    public EditarSaude EditarSaude() => new(RepositorioSaude);

    public CancelarSaude CancelarSaude() => new(RepositorioSaude);

    public ExcluirSaude ExcluirSaude() => new(RepositorioSaude);

    public ListarSaude ListarSaude() => new(RepositorioSaude);

    public ListarSaudeDoBem ListarSaudeDoBem() => new(RepositorioBem, RepositorioSaude);

    public RegistrarGastoSaude RegistrarGastoSaude() => new(RepositorioBem, RepositorioGastoSaude);

    public EditarGastoSaude EditarGastoSaude() => new(RepositorioGastoSaude);

    public ExcluirGastoSaude ExcluirGastoSaude() => new(RepositorioGastoSaude);

    public ListarGastosSaude ListarGastosSaude() => new(RepositorioGastoSaude);

    public ListarGastosSaudeDoBem ListarGastosSaudeDoBem() => new(RepositorioBem, RepositorioGastoSaude);

    public CadastrarVeiculo CadastrarVeiculo() => new(RepositorioBem, RepositorioVeiculo);

    public EditarVeiculo EditarVeiculo() => new(RepositorioVeiculo);

    public AtualizarQuilometragem AtualizarQuilometragem() => new(RepositorioVeiculo);

    public ListarVeiculos ListarVeiculos() => new(RepositorioVeiculo);

    public RegistrarAbastecimento RegistrarAbastecimento() => new(RepositorioVeiculo, RepositorioAbastecimento);

    public EditarAbastecimento EditarAbastecimento() => new(RepositorioVeiculo, RepositorioAbastecimento);

    public ExcluirAbastecimento ExcluirAbastecimento() => new(RepositorioAbastecimento);

    public ListarAbastecimentos ListarAbastecimentos() => new(RepositorioAbastecimento);

    public CadastrarImovel CadastrarImovel() => new(RepositorioBem, RepositorioImovel);

    public EditarImovel EditarImovel() => new(RepositorioImovel);

    public ListarImoveis ListarImoveis() => new(RepositorioImovel);

    public RegistrarDespesa RegistrarDespesa() => new(RepositorioBem, RepositorioDespesa);

    public EditarDespesa EditarDespesa() => new(RepositorioDespesa);

    public PagarDespesa PagarDespesa() => new(RepositorioDespesa);

    public DesfazerPagamentoDespesa DesfazerPagamentoDespesa() => new(RepositorioDespesa);

    public ExcluirDespesa ExcluirDespesa() => new(RepositorioDespesa);

    public ListarDespesas ListarDespesas() => new(RepositorioDespesa);

    public ObterConfiguracaoBackup ObterConfiguracaoBackup() => new(RepositorioConfiguracaoBackup);

    public DefinirDestinoBackup DefinirDestinoBackup() => new(RepositorioConfiguracaoBackup);

    public HabilitarAutoBackup HabilitarAutoBackup() => new(RepositorioConfiguracaoBackup);

    public DesabilitarAutoBackup DesabilitarAutoBackup() => new(RepositorioConfiguracaoBackup);

    public ExecutarBackupAgora ExecutarBackupAgora() => new(RepositorioConfiguracaoBackup, _servicoBackup, ArquivoBanco, DiretorioDocumentos, _protecaoSenha);

    public VerificarEExecutarBackupAutomatico VerificarEExecutarBackupAutomatico() =>
        new(RepositorioConfiguracaoBackup, _servicoBackup, ArquivoBanco, DiretorioDocumentos, _protecaoSenha);

    public RestaurarBackup RestaurarBackup() =>
        new(RepositorioConfiguracaoBackup, RepositorioDocumento, _servicoBackup, _armazenamentoDocumento, _bancoDados.Inicializar,
            ArquivoBanco, DiretorioDocumentos, _diretorioSeguranca, RepositorioConfiguracaoSincronizacao);

    /// <summary>Senha dos backups guardada (protegida pelo Windows) ou nulo se a proteção estiver desligada.</summary>
    public string? SenhaBackupSalva() => SenhaBackup.Obter(RepositorioConfiguracaoBackup.Obter(), _protecaoSenha);

    /// <summary>Gera uma cópia completa (banco + documentos) num único arquivo .assetly, com a senha dos backups se houver.</summary>
    public void GerarArquivoBackup(string destino) => _servicoBackup.GerarArquivo(ArquivoBanco, DiretorioDocumentos, destino, SenhaBackupSalva());

    public bool DestinoBackupConflitaComDados(string destino) =>
        ServicoBackupLocal.DestinoConflitaComDados(destino, ArquivoBanco, DiretorioDocumentos);

    public ObterPreferencias ObterPreferencias() => new(RepositorioConfiguracaoPreferencias);

    public SincronizarModulosPreferencias SincronizarModulosPreferencias() => new(RepositorioConfiguracaoPreferencias);

    public DefinirTema DefinirTema() => new(RepositorioConfiguracaoPreferencias);

    public DefinirCorDestaque DefinirCorDestaque() => new(RepositorioConfiguracaoPreferencias);

    public DefinirDensidadeTabela DefinirDensidadeTabela() => new(RepositorioConfiguracaoPreferencias);

    public DefinirIntervaloAtualizacao DefinirIntervaloAtualizacao() => new(RepositorioConfiguracaoPreferencias);

    public MoverModuloParaCima MoverModuloParaCima() => new(RepositorioConfiguracaoPreferencias);

    public MoverModuloParaBaixo MoverModuloParaBaixo() => new(RepositorioConfiguracaoPreferencias);

    public OcultarModulo OcultarModulo() => new(RepositorioConfiguracaoPreferencias);

    public MostrarModulo MostrarModulo() => new(RepositorioConfiguracaoPreferencias);

    public DefinirModuloInicial DefinirModuloInicial() => new(RepositorioConfiguracaoPreferencias);

    public DefinirAtualizacaoAutomatica DefinirAtualizacaoAutomatica() => new(RepositorioConfiguracaoPreferencias);
}
