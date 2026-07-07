package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.repositorio.RepositorioGarantia;
import com.assetly.dominio.garantia.Garantia;

import java.util.List;
import java.util.Objects;

public final class ListarGarantias {

    private final RepositorioGarantia repositorioGarantia;

    public ListarGarantias(RepositorioGarantia repositorioGarantia) {
        this.repositorioGarantia = Objects.requireNonNull(repositorioGarantia, "repositório de garantias é obrigatório");
    }

    public List<Garantia> executar() {
        return List.copyOf(repositorioGarantia.listar());
    }
}
