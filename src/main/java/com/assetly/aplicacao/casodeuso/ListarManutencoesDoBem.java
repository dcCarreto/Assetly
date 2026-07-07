package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.repositorio.RepositorioBem;
import com.assetly.aplicacao.repositorio.RepositorioManutencao;
import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.manutencao.Manutencao;

import java.util.List;
import java.util.Objects;

public final class ListarManutencoesDoBem {

    private final RepositorioBem repositorioBem;
    private final RepositorioManutencao repositorioManutencao;

    public ListarManutencoesDoBem(RepositorioBem repositorioBem, RepositorioManutencao repositorioManutencao) {
        this.repositorioBem = Objects.requireNonNull(repositorioBem, "repositório de bens é obrigatório");
        this.repositorioManutencao = Objects.requireNonNull(repositorioManutencao, "repositório de manutenções é obrigatório");
    }

    public List<Manutencao> executar(BemId bemId) {
        ApoioCasosUso.exigirBem(repositorioBem, bemId);
        return List.copyOf(repositorioManutencao.listarPorBem(bemId));
    }
}
