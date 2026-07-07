package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.dados.EditarBemComando;
import com.assetly.aplicacao.repositorio.RepositorioBem;
import com.assetly.dominio.bem.Bem;
import com.assetly.dominio.bem.NomeBem;

import java.util.Objects;

public final class EditarBem {

    private final RepositorioBem repositorioBem;

    public EditarBem(RepositorioBem repositorioBem) {
        this.repositorioBem = Objects.requireNonNull(repositorioBem, "repositório de bens é obrigatório");
    }

    public Bem executar(EditarBemComando comando) {
        Objects.requireNonNull(comando, "comando é obrigatório");
        var bem = ApoioCasosUso.exigirBem(repositorioBem, comando.id());

        bem.renomear(new NomeBem(comando.nome()));
        bem.alterarTipo(comando.tipo());

        if (comando.compradoEm() == null && comando.precoCompra() != null) {
            throw new IllegalArgumentException("data de compra é obrigatória quando preço de compra é informado");
        }
        if (comando.compradoEm() != null) {
            bem.atualizarCompra(
                    comando.compradoEm(),
                    ApoioCasosUso.dinheiroOpcional(comando.precoCompra(), comando.codigoMoeda())
            );
        }

        bem.atualizarObservacoes(comando.observacoes());
        repositorioBem.salvar(bem);
        return bem;
    }
}
