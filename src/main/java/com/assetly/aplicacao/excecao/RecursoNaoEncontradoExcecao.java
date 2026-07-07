package com.assetly.aplicacao.excecao;

public class RecursoNaoEncontradoExcecao extends RuntimeException {

    public RecursoNaoEncontradoExcecao(String mensagem) {
        super(mensagem);
    }
}
