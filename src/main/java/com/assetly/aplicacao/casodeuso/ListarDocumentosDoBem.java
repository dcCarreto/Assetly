package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.repositorio.RepositorioBem;
import com.assetly.aplicacao.repositorio.RepositorioDocumento;
import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.documento.Documento;

import java.util.List;
import java.util.Objects;

public final class ListarDocumentosDoBem {

    private final RepositorioBem repositorioBem;
    private final RepositorioDocumento repositorioDocumento;

    public ListarDocumentosDoBem(RepositorioBem repositorioBem, RepositorioDocumento repositorioDocumento) {
        this.repositorioBem = Objects.requireNonNull(repositorioBem, "repositório de bens é obrigatório");
        this.repositorioDocumento = Objects.requireNonNull(repositorioDocumento, "repositório de documentos é obrigatório");
    }

    public List<Documento> executar(BemId bemId) {
        ApoioCasosUso.exigirBem(repositorioBem, bemId);
        return List.copyOf(repositorioDocumento.listarPorBem(bemId));
    }
}
