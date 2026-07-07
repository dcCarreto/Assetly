package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.repositorio.RepositorioBem;
import com.assetly.dominio.bem.Bem;
import com.assetly.dominio.bem.BemId;

import java.util.Objects;

public final class BuscarBem {

    private final RepositorioBem repositorioBem;

    public BuscarBem(RepositorioBem repositorioBem) {
        this.repositorioBem = Objects.requireNonNull(repositorioBem, "repositório de bens é obrigatório");
    }

    public Bem executar(BemId id) {
        return ApoioCasosUso.exigirBem(repositorioBem, id);
    }
}
