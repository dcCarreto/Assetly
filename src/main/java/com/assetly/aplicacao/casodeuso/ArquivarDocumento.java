package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.repositorio.RepositorioDocumento;
import com.assetly.dominio.documento.Documento;
import com.assetly.dominio.documento.DocumentoId;

import java.util.Objects;

public final class ArquivarDocumento {

    private final RepositorioDocumento repositorioDocumento;

    public ArquivarDocumento(RepositorioDocumento repositorioDocumento) {
        this.repositorioDocumento = Objects.requireNonNull(repositorioDocumento, "repositório de documentos é obrigatório");
    }

    public Documento executar(DocumentoId id) {
        var documento = ApoioCasosUso.exigirDocumento(repositorioDocumento, id);
        documento.arquivar();
        repositorioDocumento.salvar(documento);
        return documento;
    }
}
