package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.dados.EditarGarantiaComando;
import com.assetly.aplicacao.repositorio.RepositorioGarantia;
import com.assetly.dominio.garantia.Garantia;

import java.util.Objects;

public final class EditarGarantia {

    private final RepositorioGarantia repositorioGarantia;

    public EditarGarantia(RepositorioGarantia repositorioGarantia) {
        this.repositorioGarantia = Objects.requireNonNull(repositorioGarantia, "repositório de garantias é obrigatório");
    }

    public Garantia executar(EditarGarantiaComando comando) {
        Objects.requireNonNull(comando, "comando é obrigatório");
        var garantia = ApoioCasosUso.exigirGarantia(repositorioGarantia, comando.id());

        garantia.alterarTipo(comando.tipo());
        garantia.alterarFornecedor(comando.fornecedor());
        garantia.alterarPeriodo(comando.iniciaEm(), comando.terminaEm());
        garantia.atualizarContatoSuporte(comando.contatoSuporte());

        repositorioGarantia.salvar(garantia);
        return garantia;
    }
}
