package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.repositorio.RepositorioManutencao;
import com.assetly.dominio.manutencao.Manutencao;
import com.assetly.dominio.manutencao.ManutencaoId;

import java.util.Objects;

public final class CancelarManutencao {

    private final RepositorioManutencao repositorioManutencao;

    public CancelarManutencao(RepositorioManutencao repositorioManutencao) {
        this.repositorioManutencao = Objects.requireNonNull(repositorioManutencao, "repositório de manutenções é obrigatório");
    }

    public Manutencao executar(ManutencaoId id) {
        var manutencao = ApoioCasosUso.exigirManutencao(repositorioManutencao, id);
        manutencao.cancelar();
        repositorioManutencao.salvar(manutencao);
        return manutencao;
    }
}
