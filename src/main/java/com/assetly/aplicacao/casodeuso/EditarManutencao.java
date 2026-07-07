package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.dados.EditarManutencaoComando;
import com.assetly.aplicacao.repositorio.RepositorioManutencao;
import com.assetly.dominio.manutencao.Manutencao;

import java.util.Objects;

public final class EditarManutencao {

    private final RepositorioManutencao repositorioManutencao;

    public EditarManutencao(RepositorioManutencao repositorioManutencao) {
        this.repositorioManutencao = Objects.requireNonNull(repositorioManutencao, "repositório de manutenções é obrigatório");
    }

    public Manutencao executar(EditarManutencaoComando comando) {
        Objects.requireNonNull(comando, "comando é obrigatório");
        var manutencao = ApoioCasosUso.exigirManutencao(repositorioManutencao, comando.id());

        manutencao.alterarTipo(comando.tipo());
        manutencao.atualizarDescricao(comando.descricao());
        manutencao.reagendar(comando.agendadaPara());

        repositorioManutencao.salvar(manutencao);
        return manutencao;
    }
}
