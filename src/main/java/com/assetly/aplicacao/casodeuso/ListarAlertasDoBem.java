package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.repositorio.RepositorioAlerta;
import com.assetly.aplicacao.repositorio.RepositorioBem;
import com.assetly.dominio.alerta.Alerta;
import com.assetly.dominio.bem.BemId;

import java.util.List;
import java.util.Objects;

public final class ListarAlertasDoBem {

    private final RepositorioBem repositorioBem;
    private final RepositorioAlerta repositorioAlerta;

    public ListarAlertasDoBem(RepositorioBem repositorioBem, RepositorioAlerta repositorioAlerta) {
        this.repositorioBem = Objects.requireNonNull(repositorioBem, "repositório de bens é obrigatório");
        this.repositorioAlerta = Objects.requireNonNull(repositorioAlerta, "repositório de alertas é obrigatório");
    }

    public List<Alerta> executar(BemId bemId) {
        ApoioCasosUso.exigirBem(repositorioBem, bemId);
        return List.copyOf(repositorioAlerta.listarPorBem(bemId));
    }
}
