package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.dados.AlterarStatusBemComando;
import com.assetly.aplicacao.repositorio.RepositorioBem;
import com.assetly.dominio.bem.Bem;
import com.assetly.dominio.bem.StatusBem;

import java.util.Objects;

public final class AlterarStatusBem {

    private final RepositorioBem repositorioBem;

    public AlterarStatusBem(RepositorioBem repositorioBem) {
        this.repositorioBem = Objects.requireNonNull(repositorioBem, "repositório de bens é obrigatório");
    }

    public Bem executar(AlterarStatusBemComando comando) {
        Objects.requireNonNull(comando, "comando é obrigatório");
        var status = Objects.requireNonNull(comando.status(), "status do bem é obrigatório");
        var bem = ApoioCasosUso.exigirBem(repositorioBem, comando.id());

        if (status == StatusBem.ATIVO) {
            bem.ativar();
        } else if (status == StatusBem.EM_MANUTENCAO) {
            bem.marcarEmManutencao();
        } else if (status == StatusBem.DESCARTADO) {
            bem.descartar();
        } else if (status == StatusBem.ARQUIVADO) {
            bem.arquivar();
        }

        repositorioBem.salvar(bem);
        return bem;
    }
}
