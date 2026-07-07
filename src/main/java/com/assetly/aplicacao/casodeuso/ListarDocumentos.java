package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.repositorio.RepositorioDocumento;
import com.assetly.dominio.documento.Documento;

import java.util.List;
import java.util.Objects;

public final class ListarDocumentos {

    private final RepositorioDocumento repositorioDocumento;

    public ListarDocumentos(RepositorioDocumento repositorioDocumento) {
        this.repositorioDocumento = Objects.requireNonNull(repositorioDocumento, "repositório de documentos é obrigatório");
    }

    public List<Documento> executar() {
        return List.copyOf(repositorioDocumento.listar());
    }
}
