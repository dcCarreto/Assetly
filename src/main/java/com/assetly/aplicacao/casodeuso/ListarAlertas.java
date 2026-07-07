package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.repositorio.RepositorioAlerta;
import com.assetly.dominio.alerta.Alerta;

import java.util.List;
import java.util.Objects;

public final class ListarAlertas {

    private final RepositorioAlerta repositorioAlerta;

    public ListarAlertas(RepositorioAlerta repositorioAlerta) {
        this.repositorioAlerta = Objects.requireNonNull(repositorioAlerta, "repositório de alertas é obrigatório");
    }

    public List<Alerta> executar() {
        return List.copyOf(repositorioAlerta.listar());
    }
}
