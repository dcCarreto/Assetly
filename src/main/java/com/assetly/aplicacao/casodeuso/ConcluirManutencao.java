package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.dados.ConcluirManutencaoComando;
import com.assetly.aplicacao.repositorio.RepositorioManutencao;
import com.assetly.dominio.manutencao.Manutencao;

import java.util.Objects;

public final class ConcluirManutencao {

    private final RepositorioManutencao repositorioManutencao;

    public ConcluirManutencao(RepositorioManutencao repositorioManutencao) {
        this.repositorioManutencao = Objects.requireNonNull(repositorioManutencao, "repositório de manutenções é obrigatório");
    }

    public Manutencao executar(ConcluirManutencaoComando comando) {
        Objects.requireNonNull(comando, "comando é obrigatório");
        var manutencao = ApoioCasosUso.exigirManutencao(repositorioManutencao, comando.id());
        manutencao.concluir(
                comando.concluidaEm(),
                ApoioCasosUso.dinheiroOpcional(comando.custo(), comando.codigoMoeda())
        );
        repositorioManutencao.salvar(manutencao);
        return manutencao;
    }
}
