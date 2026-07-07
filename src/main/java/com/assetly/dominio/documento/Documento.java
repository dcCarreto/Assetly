package com.assetly.dominio.documento;

import com.assetly.dominio.bem.BemId;

import java.time.LocalDate;
import java.util.Objects;

public final class Documento {

    private final DocumentoId id;
    private final BemId bemId;
    private TipoDocumento tipo;
    private NomeDocumento nome;
    private CaminhoDocumentoLocal caminhoLocal;
    private LocalDate registradoEm;
    private StatusDocumento status;

    private Documento(
            DocumentoId id,
            BemId bemId,
            TipoDocumento tipo,
            NomeDocumento nome,
            CaminhoDocumentoLocal caminhoLocal,
            LocalDate registradoEm
    ) {
        this.id = Objects.requireNonNull(id, "id do documento é obrigatório");
        this.bemId = Objects.requireNonNull(bemId, "id do bem é obrigatório");
        this.tipo = Objects.requireNonNull(tipo, "tipo do documento é obrigatório");
        this.nome = Objects.requireNonNull(nome, "nome do documento é obrigatório");
        this.caminhoLocal = Objects.requireNonNull(caminhoLocal, "caminho do documento é obrigatório");
        this.registradoEm = Objects.requireNonNull(registradoEm, "data de registro é obrigatória");
        this.status = StatusDocumento.DISPONIVEL;
    }

    public static Documento registrar(
            BemId bemId,
            TipoDocumento tipo,
            NomeDocumento nome,
            CaminhoDocumentoLocal caminhoLocal,
            LocalDate registradoEm
    ) {
        return new Documento(DocumentoId.novo(), bemId, tipo, nome, caminhoLocal, registradoEm);
    }

    public static Documento restaurar(
            DocumentoId id,
            BemId bemId,
            TipoDocumento tipo,
            NomeDocumento nome,
            CaminhoDocumentoLocal caminhoLocal,
            LocalDate registradoEm,
            StatusDocumento status
    ) {
        var documento = new Documento(id, bemId, tipo, nome, caminhoLocal, registradoEm);
        documento.status = Objects.requireNonNull(status, "status do documento é obrigatório");
        return documento;
    }

    public DocumentoId id() {
        return id;
    }

    public BemId bemId() {
        return bemId;
    }

    public TipoDocumento tipo() {
        return tipo;
    }

    public NomeDocumento nome() {
        return nome;
    }

    public CaminhoDocumentoLocal caminhoLocal() {
        return caminhoLocal;
    }

    public LocalDate registradoEm() {
        return registradoEm;
    }

    public StatusDocumento status() {
        return status;
    }

    public void renomear(NomeDocumento nome) {
        this.nome = Objects.requireNonNull(nome, "nome do documento é obrigatório");
    }

    public void alterarTipo(TipoDocumento tipo) {
        this.tipo = Objects.requireNonNull(tipo, "tipo do documento é obrigatório");
    }

    public void substituirArquivo(CaminhoDocumentoLocal caminhoLocal, LocalDate registradoEm) {
        this.caminhoLocal = Objects.requireNonNull(caminhoLocal, "caminho do documento é obrigatório");
        this.registradoEm = Objects.requireNonNull(registradoEm, "data de registro é obrigatória");
        this.status = StatusDocumento.DISPONIVEL;
    }

    public void marcarAusente() {
        this.status = StatusDocumento.AUSENTE;
    }

    public void arquivar() {
        this.status = StatusDocumento.ARQUIVADO;
    }
}
