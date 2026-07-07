package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.dados.AgendarManutencaoComando;
import com.assetly.aplicacao.repositorio.RepositorioBem;
import com.assetly.aplicacao.repositorio.RepositorioManutencao;
import com.assetly.dominio.manutencao.Manutencao;

import java.util.Objects;

public final class AgendarManutencao {

    private final RepositorioBem repositorioBem;
    private final RepositorioManutencao repositorioManutencao;

    public AgendarManutencao(RepositorioBem repositorioBem, RepositorioManutencao repositorioManutencao) {
        this.repositorioBem = Objects.requireNonNull(repositorioBem, "repositório de bens é obrigatório");
        this.repositorioManutencao = Objects.requireNonNull(repositorioManutencao, "repositório de manutenções é obrigatório");
    }

    public Manutencao executar(AgendarManutencaoComando comando) {
        Objects.requireNonNull(comando, "comando é obrigatório");
        ApoioCasosUso.exigirBem(repositorioBem, comando.bemId());

        var manutencao = Manutencao.agendar(
                comando.bemId(),
                comando.tipo(),
                comando.descricao(),
                comando.agendadaPara()
        );
        repositorioManutencao.salvar(manutencao);
        return manutencao;
    }
}
