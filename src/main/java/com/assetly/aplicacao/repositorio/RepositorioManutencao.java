package com.assetly.aplicacao.repositorio;

import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.manutencao.Manutencao;
import com.assetly.dominio.manutencao.ManutencaoId;

import java.util.List;
import java.util.Optional;

public interface RepositorioManutencao {

    Optional<Manutencao> buscarPorId(ManutencaoId id);

    List<Manutencao> listar();

    List<Manutencao> listarPorBem(BemId bemId);

    void salvar(Manutencao manutencao);
}
