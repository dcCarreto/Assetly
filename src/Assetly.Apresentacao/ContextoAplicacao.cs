using Assetly.Aplicacao.CasoDeUso;
using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;
using Assetly.Apresentacao.Semeadura;
using Assetly.Infraestrutura.Armazenamento;
using Assetly.Infraestrutura.Banco;
using Assetly.Infraestrutura.Repositorio;

namespace Assetly.Apresentacao;

public sealed class ContextoAplicacao
{
    private readonly BancoDadosAssetly _bancoDados;
    private readonly IArmazenamentoDocumento _armazenamentoDocumento;

    public string RotuloAmbiente { get; }

    public bool AmbienteTeste { get; }

    public IRepositorioBem RepositorioBem { get; }

    public IRepositorioGarantia RepositorioGarantia { get; }

    public IRepositorioManutencao RepositorioManutencao { get; }

    public IRepositorioDocumento RepositorioDocumento { get; }

    public IRepositorioAlerta RepositorioAlerta { get; }

    public IRepositorioSaude RepositorioSaude { get; }

    public IRepositorioGastoSaude RepositorioGastoSaude { get; }

    private ContextoAplicacao(
        BancoDadosAssetly bancoDados,
        IArmazenamentoDocumento armazenamentoDocumento,
        string rotuloAmbiente,
        bool ambienteTeste)
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
        _armazenamentoDocumento = armazenamentoDocumento ?? throw new ArgumentNullException(nameof(armazenamentoDocumento), "armazenamento de documentos é obrigatório");
        RotuloAmbiente = rotuloAmbiente ?? throw new ArgumentNullException(nameof(rotuloAmbiente), "rótulo do ambiente é obrigatório");
        AmbienteTeste = ambienteTeste;

        if (ambienteTeste)
        {
            DadosFicticiosAplicacao.PopularSeNecessario(
                RepositorioBem,
                RepositorioGarantia,
                RepositorioManutencao,
                RepositorioDocumento,
                RepositorioAlerta,
                CaminhosArmazenamentoLocal.DiretorioDocumentosTeste());
        }
    }

    public static ContextoAplicacao LocalPadrao() =>
        new(BancoDadosAssetly.LocalPadrao(), ArmazenamentoDocumentoLocal.Padrao(), "Produção local", false);

    public static ContextoAplicacao TesteComDadosFicticios() =>
        new(
            BancoDadosAssetly.LocalTeste(),
            new ArmazenamentoDocumentoLocal(CaminhosArmazenamentoLocal.DiretorioDocumentosTeste()),
            "Teste",
            true);

    public CadastrarBem CadastrarBem() => new(RepositorioBem);

    public EditarBem EditarBem() => new(RepositorioBem);

    public BuscarBem BuscarBem() => new(RepositorioBem);

    public ListarBens ListarBens() => new(RepositorioBem);

    public AlterarStatusBem AlterarStatusBem() => new(RepositorioBem);

    public RegistrarGarantia RegistrarGarantia() => new(RepositorioBem, RepositorioGarantia);

    public EditarGarantia EditarGarantia() => new(RepositorioGarantia);

    public ListarGarantias ListarGarantias() => new(RepositorioGarantia);

    public ListarGarantiasDoBem ListarGarantiasDoBem() => new(RepositorioBem, RepositorioGarantia);

    public AgendarManutencao AgendarManutencao() => new(RepositorioBem, RepositorioManutencao);

    public EditarManutencao EditarManutencao() => new(RepositorioManutencao);

    public ConcluirManutencao ConcluirManutencao() => new(RepositorioManutencao);

    public CancelarManutencao CancelarManutencao() => new(RepositorioManutencao);

    public ListarManutencoes ListarManutencoes() => new(RepositorioManutencao);

    public ListarManutencoesDoBem ListarManutencoesDoBem() => new(RepositorioBem, RepositorioManutencao);

    public RegistrarDocumento RegistrarDocumento() => new(RepositorioBem, RepositorioDocumento, _armazenamentoDocumento);

    public EditarDocumento EditarDocumento() => new(RepositorioDocumento, _armazenamentoDocumento);

    public MarcarDocumentoAusente MarcarDocumentoAusente() => new(RepositorioDocumento);

    public ArquivarDocumento ArquivarDocumento() => new(RepositorioDocumento);

    public ListarDocumentos ListarDocumentos() => new(RepositorioDocumento);

    public ListarDocumentosDoBem ListarDocumentosDoBem() => new(RepositorioBem, RepositorioDocumento);

    public CriarAlerta CriarAlerta() => new(RepositorioBem, RepositorioAlerta);

    public GerarAlertasAutomaticos GerarAlertasAutomaticos() =>
        new(RepositorioBem, RepositorioGarantia, RepositorioManutencao, RepositorioDocumento, RepositorioAlerta);

    public TomarCienciaAlerta TomarCienciaAlerta() => new(RepositorioAlerta);

    public ResolverAlerta ResolverAlerta() => new(RepositorioAlerta);

    public ListarAlertas ListarAlertas() => new(RepositorioAlerta);

    public ListarAlertasDoBem ListarAlertasDoBem() => new(RepositorioBem, RepositorioAlerta);

    public RegistrarSaude RegistrarSaude() => new(RepositorioBem, RepositorioSaude);

    public EditarSaude EditarSaude() => new(RepositorioSaude);

    public CancelarSaude CancelarSaude() => new(RepositorioSaude);

    public ListarSaude ListarSaude() => new(RepositorioSaude);

    public ListarSaudeDoBem ListarSaudeDoBem() => new(RepositorioBem, RepositorioSaude);

    public RegistrarGastoSaude RegistrarGastoSaude() => new(RepositorioBem, RepositorioGastoSaude);

    public EditarGastoSaude EditarGastoSaude() => new(RepositorioGastoSaude);

    public ListarGastosSaude ListarGastosSaude() => new(RepositorioGastoSaude);

    public ListarGastosSaudeDoBem ListarGastosSaudeDoBem() => new(RepositorioBem, RepositorioGastoSaude);
}
