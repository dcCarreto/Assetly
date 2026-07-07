package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.repositorio.RepositorioBem;
import com.assetly.dominio.bem.Bem;

import java.util.List;
import java.util.Objects;

public final class ListarBens {

    private final RepositorioBem repositorioBem;

    public ListarBens(RepositorioBem repositorioBem) {
        this.repositorioBem = Objects.requireNonNull(repositorioBem, "repositório de bens é obrigatório");
    }

    public List<Bem> executar() {
        return List.copyOf(repositorioBem.listar());
    }
}
