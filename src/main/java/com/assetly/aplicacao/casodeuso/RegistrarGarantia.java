package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.dados.RegistrarGarantiaComando;
import com.assetly.aplicacao.repositorio.RepositorioBem;
import com.assetly.aplicacao.repositorio.RepositorioGarantia;
import com.assetly.dominio.garantia.Garantia;

import java.util.Objects;

public final class RegistrarGarantia {

    private final RepositorioBem repositorioBem;
    private final RepositorioGarantia repositorioGarantia;

    public RegistrarGarantia(RepositorioBem repositorioBem, RepositorioGarantia repositorioGarantia) {
        this.repositorioBem = Objects.requireNonNull(repositorioBem, "repositório de bens é obrigatório");
        this.repositorioGarantia = Objects.requireNonNull(repositorioGarantia, "repositório de garantias é obrigatório");
    }

    public Garantia executar(RegistrarGarantiaComando comando) {
        Objects.requireNonNull(comando, "comando é obrigatório");
        ApoioCasosUso.exigirBem(repositorioBem, comando.bemId());

        var garantia = Garantia.criar(
                comando.bemId(),
                comando.tipo(),
                comando.fornecedor(),
                comando.iniciaEm(),
                comando.terminaEm()
        );
        garantia.atualizarContatoSuporte(comando.contatoSuporte());
        repositorioGarantia.salvar(garantia);
        return garantia;
    }
}
