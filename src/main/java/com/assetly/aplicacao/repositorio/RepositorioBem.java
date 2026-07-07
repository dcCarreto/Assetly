package com.assetly.aplicacao.repositorio;

import com.assetly.dominio.bem.Bem;
import com.assetly.dominio.bem.BemId;

import java.util.List;
import java.util.Optional;

public interface RepositorioBem {

    Optional<Bem> buscarPorId(BemId id);

    List<Bem> listar();

    void salvar(Bem bem);
}
