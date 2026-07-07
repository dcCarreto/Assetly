package com.assetly.infraestrutura.banco;

public class ExcecaoBancoDados extends RuntimeException {

    public ExcecaoBancoDados(String mensagem, Throwable causa) {
        super(mensagem, causa);
    }
}
