package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.repositorio.RepositorioAlerta;
import com.assetly.dominio.alerta.Alerta;
import com.assetly.dominio.alerta.AlertaId;

import java.util.Objects;

public final class ResolverAlerta {

    private final RepositorioAlerta repositorioAlerta;

    public ResolverAlerta(RepositorioAlerta repositorioAlerta) {
        this.repositorioAlerta = Objects.requireNonNull(repositorioAlerta, "repositório de alertas é obrigatório");
    }

    public Alerta executar(AlertaId id) {
        var alerta = ApoioCasosUso.exigirAlerta(repositorioAlerta, id);
        alerta.resolver();
        repositorioAlerta.salvar(alerta);
        return alerta;
    }
}
