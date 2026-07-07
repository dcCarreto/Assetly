package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.repositorio.RepositorioDocumento;
import com.assetly.dominio.documento.Documento;
import com.assetly.dominio.documento.DocumentoId;

import java.util.Objects;

public final class MarcarDocumentoAusente {

    private final RepositorioDocumento repositorioDocumento;

    public MarcarDocumentoAusente(RepositorioDocumento repositorioDocumento) {
        this.repositorioDocumento = Objects.requireNonNull(repositorioDocumento, "repositório de documentos é obrigatório");
    }

    public Documento executar(DocumentoId id) {
        var documento = ApoioCasosUso.exigirDocumento(repositorioDocumento, id);
        documento.marcarAusente();
        repositorioDocumento.salvar(documento);
        return documento;
    }
}
