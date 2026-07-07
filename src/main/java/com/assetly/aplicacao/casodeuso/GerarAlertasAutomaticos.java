package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.repositorio.RepositorioAlerta;
import com.assetly.aplicacao.repositorio.RepositorioBem;
import com.assetly.aplicacao.repositorio.RepositorioDocumento;
import com.assetly.aplicacao.repositorio.RepositorioGarantia;
import com.assetly.aplicacao.repositorio.RepositorioManutencao;
import com.assetly.dominio.alerta.Alerta;
import com.assetly.dominio.alerta.SeveridadeAlerta;
import com.assetly.dominio.alerta.TipoAlerta;
import com.assetly.dominio.bem.Bem;
import com.assetly.dominio.bem.StatusBem;
import com.assetly.dominio.documento.StatusDocumento;
import com.assetly.dominio.documento.TipoDocumento;
import com.assetly.dominio.garantia.Garantia;
import com.assetly.dominio.garantia.StatusGarantia;
import com.assetly.dominio.manutencao.Manutencao;
import com.assetly.dominio.manutencao.StatusManutencao;

import java.time.LocalDate;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;

public final class GerarAlertasAutomaticos {

    private static final int DIAS_MANUTENCAO_PROXIMA = 7;
    private static final int TAMANHO_DESCRICAO_ALERTA = 120;
    private static final DateTimeFormatter DATA = DateTimeFormatter.ofPattern("dd/MM/yyyy");

    private final RepositorioBem repositorioBem;
    private final RepositorioGarantia repositorioGarantia;
    private final RepositorioManutencao repositorioManutencao;
    private final RepositorioDocumento repositorioDocumento;
    private final RepositorioAlerta repositorioAlerta;

    public GerarAlertasAutomaticos(
            RepositorioBem repositorioBem,
            RepositorioGarantia repositorioGarantia,
            RepositorioManutencao repositorioManutencao,
            RepositorioDocumento repositorioDocumento,
            RepositorioAlerta repositorioAlerta
    ) {
        this.repositorioBem = Objects.requireNonNull(repositorioBem, "repositório de bens é obrigatório");
        this.repositorioGarantia = Objects.requireNonNull(repositorioGarantia, "repositório de garantias é obrigatório");
        this.repositorioManutencao = Objects.requireNonNull(repositorioManutencao, "repositório de manutenções é obrigatório");
        this.repositorioDocumento = Objects.requireNonNull(repositorioDocumento, "repositório de documentos é obrigatório");
        this.repositorioAlerta = Objects.requireNonNull(repositorioAlerta, "repositório de alertas é obrigatório");
    }

    public List<Alerta> executar(LocalDate dataReferencia) {
        Objects.requireNonNull(dataReferencia, "data de referência é obrigatória");
        var gerados = new ArrayList<Alerta>();

        repositorioGarantia.listar().forEach(garantia -> gerarAlertaGarantia(garantia, dataReferencia, gerados));
        repositorioManutencao.listar().forEach(manutencao -> gerarAlertaManutencao(manutencao, dataReferencia, gerados));
        repositorioBem.listar().stream()
                .filter(this::deveVerificarNotaFiscal)
                .forEach(bem -> gerarAlertaDocumentoAusente(bem, dataReferencia, gerados));

        return List.copyOf(gerados);
    }

    private void gerarAlertaGarantia(Garantia garantia, LocalDate dataReferencia, List<Alerta> gerados) {
        var status = garantia.statusEm(dataReferencia);
        if (status == StatusGarantia.VENCIDA) {
            registrarSeNovo(
                    garantia.bemId(),
                    TipoAlerta.GARANTIA_VENCIDA,
                    SeveridadeAlerta.CRITICO,
                    "Garantia vencida em " + DATA.format(garantia.periodo().terminaEm()) + ".",
                    dataReferencia,
                    garantia.periodo().terminaEm(),
                    gerados
            );
        } else if (status == StatusGarantia.PERTO_DO_VENCIMENTO) {
            registrarSeNovo(
                    garantia.bemId(),
                    TipoAlerta.GARANTIA_VENCENDO,
                    SeveridadeAlerta.AVISO,
                    "Garantia vence em " + DATA.format(garantia.periodo().terminaEm()) + ".",
                    dataReferencia,
                    garantia.periodo().terminaEm(),
                    gerados
            );
        }
    }

    private void gerarAlertaManutencao(Manutencao manutencao, LocalDate dataReferencia, List<Alerta> gerados) {
        var status = manutencao.statusEm(dataReferencia);
        if (status == StatusManutencao.ATRASADA) {
            registrarSeNovo(
                    manutencao.bemId(),
                    TipoAlerta.MANUTENCAO_ATRASADA,
                    SeveridadeAlerta.CRITICO,
                    "Manutenção atrasada desde " + DATA.format(manutencao.agendadaPara()) + ": " + descricaoAlerta(manutencao) + ".",
                    dataReferencia,
                    manutencao.agendadaPara(),
                    gerados
            );
        } else if (status == StatusManutencao.AGENDADA && !manutencao.agendadaPara().isAfter(dataReferencia.plusDays(DIAS_MANUTENCAO_PROXIMA))) {
            registrarSeNovo(
                    manutencao.bemId(),
                    TipoAlerta.MANUTENCAO_PROXIMA,
                    SeveridadeAlerta.INFORMATIVO,
                    "Manutenção próxima em " + DATA.format(manutencao.agendadaPara()) + ": " + descricaoAlerta(manutencao) + ".",
                    dataReferencia,
                    manutencao.agendadaPara(),
                    gerados
            );
        }
    }

    private void gerarAlertaDocumentoAusente(Bem bem, LocalDate dataReferencia, List<Alerta> gerados) {
        var possuiNotaFiscal = repositorioDocumento.listarPorBem(bem.id()).stream()
                .anyMatch(documento -> documento.tipo() == TipoDocumento.NOTA_FISCAL
                        && documento.status() == StatusDocumento.DISPONIVEL);
        if (!possuiNotaFiscal) {
            registrarSeNovo(
                    bem.id(),
                    TipoAlerta.DOCUMENTO_AUSENTE,
                    SeveridadeAlerta.AVISO,
                    "Nota fiscal ausente.",
                    dataReferencia,
                    null,
                    gerados
            );
        }
    }

    private boolean deveVerificarNotaFiscal(Bem bem) {
        return bem.status() == StatusBem.ATIVO || bem.status() == StatusBem.EM_MANUTENCAO;
    }

    private String descricaoAlerta(Manutencao manutencao) {
        var descricao = manutencao.descricao();
        if (descricao.length() <= TAMANHO_DESCRICAO_ALERTA) {
            return descricao;
        }
        return descricao.substring(0, TAMANHO_DESCRICAO_ALERTA - 3) + "...";
    }

    private void registrarSeNovo(
            com.assetly.dominio.bem.BemId bemId,
            TipoAlerta tipo,
            SeveridadeAlerta severidade,
            String mensagem,
            LocalDate criadoEm,
            LocalDate prazoEm,
            List<Alerta> gerados
    ) {
        if (alertaJaRegistrado(bemId, tipo, mensagem)) {
            return;
        }
        var alerta = Alerta.criar(bemId, tipo, severidade, mensagem, criadoEm, prazoEm);
        repositorioAlerta.salvar(alerta);
        gerados.add(alerta);
    }

    private boolean alertaJaRegistrado(com.assetly.dominio.bem.BemId bemId, TipoAlerta tipo, String mensagem) {
        return repositorioAlerta.listarPorBem(bemId).stream()
                .anyMatch(alerta -> alerta.tipo() == tipo && alerta.mensagem().equals(mensagem));
    }
}
