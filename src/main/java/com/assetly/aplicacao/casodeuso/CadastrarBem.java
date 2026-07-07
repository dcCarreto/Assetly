package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.dados.CadastrarBemComando;
import com.assetly.aplicacao.repositorio.RepositorioBem;
import com.assetly.dominio.bem.Bem;
import com.assetly.dominio.bem.NomeBem;

import java.util.Objects;

public final class CadastrarBem {

    private final RepositorioBem repositorioBem;

    public CadastrarBem(RepositorioBem repositorioBem) {
        this.repositorioBem = Objects.requireNonNull(repositorioBem, "repositório de bens é obrigatório");
    }

    public Bem executar(CadastrarBemComando comando) {
        Objects.requireNonNull(comando, "comando é obrigatório");
        var nome = new NomeBem(comando.nome());
        var tipo = Objects.requireNonNull(comando.tipo(), "tipo do bem é obrigatório");

        var bem = comando.compradoEm() == null
                ? Bem.criar(nome, tipo)
                : Bem.criar(nome, tipo, comando.compradoEm(), ApoioCasosUso.dinheiroOpcional(
                        comando.precoCompra(),
                        comando.codigoMoeda()
                ));

        if (comando.compradoEm() == null && comando.precoCompra() != null) {
            throw new IllegalArgumentException("data de compra é obrigatória quando preço de compra é informado");
        }

        bem.atualizarObservacoes(comando.observacoes());
        repositorioBem.salvar(bem);
        return bem;
    }
}
