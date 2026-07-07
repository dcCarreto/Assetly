package com.assetly.aplicacao.repositorio;

import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.documento.Documento;
import com.assetly.dominio.documento.DocumentoId;

import java.util.List;
import java.util.Optional;

public interface RepositorioDocumento {

    Optional<Documento> buscarPorId(DocumentoId id);

    List<Documento> listar();

    List<Documento> listarPorBem(BemId bemId);

    void salvar(Documento documento);
}
