package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.repositorio.RepositorioManutencao;
import com.assetly.dominio.manutencao.Manutencao;

import java.util.List;
import java.util.Objects;

public final class ListarManutencoes {

    private final RepositorioManutencao repositorioManutencao;

    public ListarManutencoes(RepositorioManutencao repositorioManutencao) {
        this.repositorioManutencao = Objects.requireNonNull(repositorioManutencao, "repositório de manutenções é obrigatório");
    }

    public List<Manutencao> executar() {
        return List.copyOf(repositorioManutencao.listar());
    }
}
