package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.repositorio.RepositorioBem;
import com.assetly.aplicacao.repositorio.RepositorioGarantia;
import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.garantia.Garantia;

import java.util.List;
import java.util.Objects;

public final class ListarGarantiasDoBem {

    private final RepositorioBem repositorioBem;
    private final RepositorioGarantia repositorioGarantia;

    public ListarGarantiasDoBem(RepositorioBem repositorioBem, RepositorioGarantia repositorioGarantia) {
        this.repositorioBem = Objects.requireNonNull(repositorioBem, "repositório de bens é obrigatório");
        this.repositorioGarantia = Objects.requireNonNull(repositorioGarantia, "repositório de garantias é obrigatório");
    }

    public List<Garantia> executar(BemId bemId) {
        ApoioCasosUso.exigirBem(repositorioBem, bemId);
        return List.copyOf(repositorioGarantia.listarPorBem(bemId));
    }
}
