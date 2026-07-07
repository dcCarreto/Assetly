package com.assetly.apresentacao;

import com.assetly.aplicacao.casodeuso.AgendarManutencao;
import com.assetly.aplicacao.casodeuso.AlterarStatusBem;
import com.assetly.aplicacao.casodeuso.ArquivarDocumento;
import com.assetly.aplicacao.casodeuso.BuscarBem;
import com.assetly.aplicacao.casodeuso.CadastrarBem;
import com.assetly.aplicacao.casodeuso.CancelarManutencao;
import com.assetly.aplicacao.casodeuso.ConcluirManutencao;
import com.assetly.aplicacao.casodeuso.CriarAlerta;
import com.assetly.aplicacao.casodeuso.EditarBem;
import com.assetly.aplicacao.casodeuso.EditarDocumento;
import com.assetly.aplicacao.casodeuso.EditarGarantia;
import com.assetly.aplicacao.casodeuso.EditarManutencao;
import com.assetly.aplicacao.casodeuso.GerarAlertasAutomaticos;
import com.assetly.aplicacao.casodeuso.ListarAlertas;
import com.assetly.aplicacao.casodeuso.ListarAlertasDoBem;
import com.assetly.aplicacao.casodeuso.ListarBens;
import com.assetly.aplicacao.casodeuso.ListarDocumentos;
import com.assetly.aplicacao.casodeuso.ListarDocumentosDoBem;
import com.assetly.aplicacao.casodeuso.ListarGarantias;
import com.assetly.aplicacao.casodeuso.ListarGarantiasDoBem;
import com.assetly.aplicacao.casodeuso.ListarManutencoes;
import com.assetly.aplicacao.casodeuso.ListarManutencoesDoBem;
import com.assetly.aplicacao.casodeuso.MarcarDocumentoAusente;
import com.assetly.aplicacao.casodeuso.RegistrarDocumento;
import com.assetly.aplicacao.casodeuso.RegistrarGarantia;
import com.assetly.aplicacao.casodeuso.ResolverAlerta;
import com.assetly.aplicacao.casodeuso.TomarCienciaAlerta;
import com.assetly.aplicacao.repositorio.RepositorioAlerta;
import com.assetly.aplicacao.repositorio.RepositorioBem;
import com.assetly.aplicacao.repositorio.RepositorioDocumento;
import com.assetly.aplicacao.repositorio.RepositorioGarantia;
import com.assetly.aplicacao.repositorio.RepositorioManutencao;
import com.assetly.aplicacao.servico.ArmazenamentoDocumento;
import com.assetly.infraestrutura.armazenamento.ArmazenamentoDocumentoLocal;
import com.assetly.infraestrutura.armazenamento.CaminhosArmazenamentoLocal;
import com.assetly.infraestrutura.banco.BancoDadosAssetly;
import com.assetly.infraestrutura.repositorio.RepositorioAlertaSqlite;
import com.assetly.infraestrutura.repositorio.RepositorioBemSqlite;
import com.assetly.infraestrutura.repositorio.RepositorioDocumentoSqlite;
import com.assetly.infraestrutura.repositorio.RepositorioGarantiaSqlite;
import com.assetly.infraestrutura.repositorio.RepositorioManutencaoSqlite;

import java.util.Objects;

public final class ContextoAplicacao {

    private final BancoDadosAssetly bancoDados;
    private final RepositorioBem repositorioBem;
    private final RepositorioGarantia repositorioGarantia;
    private final RepositorioManutencao repositorioManutencao;
    private final RepositorioDocumento repositorioDocumento;
    private final RepositorioAlerta repositorioAlerta;
    private final ArmazenamentoDocumento armazenamentoDocumento;
    private final String rotuloAmbiente;
    private final boolean ambienteTeste;

    private ContextoAplicacao(
            BancoDadosAssetly bancoDados,
            ArmazenamentoDocumento armazenamentoDocumento,
            String rotuloAmbiente,
            boolean ambienteTeste
    ) {
        this.bancoDados = Objects.requireNonNull(bancoDados, "banco de dados é obrigatório");
        this.bancoDados.inicializar();
        var fabricaConexao = bancoDados.fabricaConexao();
        this.repositorioBem = new RepositorioBemSqlite(fabricaConexao);
        this.repositorioGarantia = new RepositorioGarantiaSqlite(fabricaConexao);
        this.repositorioManutencao = new RepositorioManutencaoSqlite(fabricaConexao);
        this.repositorioDocumento = new RepositorioDocumentoSqlite(fabricaConexao);
        this.repositorioAlerta = new RepositorioAlertaSqlite(fabricaConexao);
        this.armazenamentoDocumento = Objects.requireNonNull(armazenamentoDocumento, "armazenamento de documentos é obrigatório");
        this.rotuloAmbiente = Objects.requireNonNull(rotuloAmbiente, "rótulo do ambiente é obrigatório");
        this.ambienteTeste = ambienteTeste;
        if (ambienteTeste) {
            DadosFicticiosAplicacao.popularSeNecessario(
                    repositorioBem,
                    repositorioGarantia,
                    repositorioManutencao,
                    repositorioDocumento,
                    repositorioAlerta,
                    CaminhosArmazenamentoLocal.diretorioDocumentosTeste()
            );
        }
    }

    public static ContextoAplicacao localPadrao() {
        return new ContextoAplicacao(
                BancoDadosAssetly.localPadrao(),
                ArmazenamentoDocumentoLocal.padrao(),
                "Produção local",
                false
        );
    }

    public static ContextoAplicacao testeComDadosFicticios() {
        return new ContextoAplicacao(
                BancoDadosAssetly.localTeste(),
                new ArmazenamentoDocumentoLocal(CaminhosArmazenamentoLocal.diretorioDocumentosTeste()),
                "Teste",
                true
        );
    }

    public String rotuloAmbiente() {
        return rotuloAmbiente;
    }

    public boolean ambienteTeste() {
        return ambienteTeste;
    }

    public RepositorioBem repositorioBem() {
        return repositorioBem;
    }

    public RepositorioGarantia repositorioGarantia() {
        return repositorioGarantia;
    }

    public RepositorioManutencao repositorioManutencao() {
        return repositorioManutencao;
    }

    public RepositorioDocumento repositorioDocumento() {
        return repositorioDocumento;
    }

    public RepositorioAlerta repositorioAlerta() {
        return repositorioAlerta;
    }

    public CadastrarBem cadastrarBem() {
        return new CadastrarBem(repositorioBem);
    }

    public EditarBem editarBem() {
        return new EditarBem(repositorioBem);
    }

    public BuscarBem buscarBem() {
        return new BuscarBem(repositorioBem);
    }

    public ListarBens listarBens() {
        return new ListarBens(repositorioBem);
    }

    public AlterarStatusBem alterarStatusBem() {
        return new AlterarStatusBem(repositorioBem);
    }

    public RegistrarGarantia registrarGarantia() {
        return new RegistrarGarantia(repositorioBem, repositorioGarantia);
    }

    public EditarGarantia editarGarantia() {
        return new EditarGarantia(repositorioGarantia);
    }

    public ListarGarantias listarGarantias() {
        return new ListarGarantias(repositorioGarantia);
    }

    public ListarGarantiasDoBem listarGarantiasDoBem() {
        return new ListarGarantiasDoBem(repositorioBem, repositorioGarantia);
    }

    public AgendarManutencao agendarManutencao() {
        return new AgendarManutencao(repositorioBem, repositorioManutencao);
    }

    public EditarManutencao editarManutencao() {
        return new EditarManutencao(repositorioManutencao);
    }

    public ConcluirManutencao concluirManutencao() {
        return new ConcluirManutencao(repositorioManutencao);
    }

    public CancelarManutencao cancelarManutencao() {
        return new CancelarManutencao(repositorioManutencao);
    }

    public ListarManutencoes listarManutencoes() {
        return new ListarManutencoes(repositorioManutencao);
    }

    public ListarManutencoesDoBem listarManutencoesDoBem() {
        return new ListarManutencoesDoBem(repositorioBem, repositorioManutencao);
    }

    public RegistrarDocumento registrarDocumento() {
        return new RegistrarDocumento(repositorioBem, repositorioDocumento, armazenamentoDocumento);
    }

    public EditarDocumento editarDocumento() {
        return new EditarDocumento(repositorioDocumento, armazenamentoDocumento);
    }

    public MarcarDocumentoAusente marcarDocumentoAusente() {
        return new MarcarDocumentoAusente(repositorioDocumento);
    }

    public ArquivarDocumento arquivarDocumento() {
        return new ArquivarDocumento(repositorioDocumento);
    }

    public ListarDocumentos listarDocumentos() {
        return new ListarDocumentos(repositorioDocumento);
    }

    public ListarDocumentosDoBem listarDocumentosDoBem() {
        return new ListarDocumentosDoBem(repositorioBem, repositorioDocumento);
    }

    public CriarAlerta criarAlerta() {
        return new CriarAlerta(repositorioBem, repositorioAlerta);
    }

    public GerarAlertasAutomaticos gerarAlertasAutomaticos() {
        return new GerarAlertasAutomaticos(
                repositorioBem,
                repositorioGarantia,
                repositorioManutencao,
                repositorioDocumento,
                repositorioAlerta
        );
    }

    public TomarCienciaAlerta tomarCienciaAlerta() {
        return new TomarCienciaAlerta(repositorioAlerta);
    }

    public ResolverAlerta resolverAlerta() {
        return new ResolverAlerta(repositorioAlerta);
    }

    public ListarAlertas listarAlertas() {
        return new ListarAlertas(repositorioAlerta);
    }

    public ListarAlertasDoBem listarAlertasDoBem() {
        return new ListarAlertasDoBem(repositorioBem, repositorioAlerta);
    }
}
