package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.dados.CriarAlertaComando;
import com.assetly.aplicacao.repositorio.RepositorioAlerta;
import com.assetly.aplicacao.repositorio.RepositorioBem;
import com.assetly.dominio.alerta.Alerta;

import java.util.Objects;

public final class CriarAlerta {

    private final RepositorioBem repositorioBem;
    private final RepositorioAlerta repositorioAlerta;

    public CriarAlerta(RepositorioBem repositorioBem, RepositorioAlerta repositorioAlerta) {
        this.repositorioBem = Objects.requireNonNull(repositorioBem, "repositório de bens é obrigatório");
        this.repositorioAlerta = Objects.requireNonNull(repositorioAlerta, "repositório de alertas é obrigatório");
    }

    public Alerta executar(CriarAlertaComando comando) {
        Objects.requireNonNull(comando, "comando é obrigatório");
        ApoioCasosUso.exigirBem(repositorioBem, comando.bemId());

        var alerta = Alerta.criar(
                comando.bemId(),
                comando.tipo(),
                comando.severidade(),
                comando.mensagem(),
                comando.criadoEm(),
                comando.prazoEm()
        );
        repositorioAlerta.salvar(alerta);
        return alerta;
    }
}
