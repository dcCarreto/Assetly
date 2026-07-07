package com.assetly.dominio.garantia;

import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.compartilhado.Validacao;

import java.time.LocalDate;
import java.util.Objects;

public final class Garantia {

    public static final int DIAS_PADRAO_PERTO_DO_VENCIMENTO = 30;

    private final GarantiaId id;
    private final BemId bemId;
    private TipoGarantia tipo;
    private String fornecedor;
    private PeriodoGarantia periodo;
    private String contatoSuporte;

    private Garantia(
            GarantiaId id,
            BemId bemId,
            TipoGarantia tipo,
            String fornecedor,
            PeriodoGarantia periodo,
            String contatoSuporte
    ) {
        this.id = Objects.requireNonNull(id, "id da garantia é obrigatório");
        this.bemId = Objects.requireNonNull(bemId, "id do bem é obrigatório");
        this.tipo = Objects.requireNonNull(tipo, "tipo da garantia é obrigatório");
        this.fornecedor = Validacao.textoOpcional(fornecedor, "fornecedor da garantia", 120);
        this.periodo = Objects.requireNonNull(periodo, "período da garantia é obrigatório");
        this.contatoSuporte = Validacao.textoOpcional(contatoSuporte, "contato de suporte", 160);
    }

    public static Garantia criar(
            BemId bemId,
            TipoGarantia tipo,
            String fornecedor,
            LocalDate iniciaEm,
            LocalDate terminaEm
    ) {
        return new Garantia(
                GarantiaId.novo(),
                bemId,
                tipo,
                fornecedor,
                new PeriodoGarantia(iniciaEm, terminaEm),
                ""
        );
    }

    public static Garantia restaurar(
            GarantiaId id,
            BemId bemId,
            TipoGarantia tipo,
            String fornecedor,
            LocalDate iniciaEm,
            LocalDate terminaEm,
            String contatoSuporte
    ) {
        return new Garantia(
                id,
                bemId,
                tipo,
                fornecedor,
                new PeriodoGarantia(iniciaEm, terminaEm),
                contatoSuporte
        );
    }

    public GarantiaId id() {
        return id;
    }

    public BemId bemId() {
        return bemId;
    }

    public TipoGarantia tipo() {
        return tipo;
    }

    public String fornecedor() {
        return fornecedor;
    }

    public PeriodoGarantia periodo() {
        return periodo;
    }

    public String contatoSuporte() {
        return contatoSuporte;
    }

    public StatusGarantia statusEm(LocalDate data) {
        Objects.requireNonNull(data, "data é obrigatória");
        if (data.isBefore(periodo.iniciaEm())) {
            return StatusGarantia.NAO_INICIADA;
        }
        if (periodo.estaVencidaEm(data)) {
            return StatusGarantia.VENCIDA;
        }
        if (periodo.venceEmAte(data, DIAS_PADRAO_PERTO_DO_VENCIMENTO)) {
            return StatusGarantia.PERTO_DO_VENCIMENTO;
        }
        return StatusGarantia.ATIVA;
    }

    public void alterarFornecedor(String fornecedor) {
        this.fornecedor = Validacao.textoOpcional(fornecedor, "fornecedor da garantia", 120);
    }

    public void alterarPeriodo(LocalDate iniciaEm, LocalDate terminaEm) {
        this.periodo = new PeriodoGarantia(iniciaEm, terminaEm);
    }

    public void alterarTipo(TipoGarantia tipo) {
        this.tipo = Objects.requireNonNull(tipo, "tipo da garantia é obrigatório");
    }

    public void atualizarContatoSuporte(String contatoSuporte) {
        this.contatoSuporte = Validacao.textoOpcional(contatoSuporte, "contato de suporte", 160);
    }
}
