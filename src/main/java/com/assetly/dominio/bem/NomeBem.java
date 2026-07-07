package com.assetly.dominio.bem;

import com.assetly.dominio.compartilhado.Validacao;

public record NomeBem(String valor) {

    public static final int TAMANHO_MAXIMO = 120;

    public NomeBem {
        valor = Validacao.exigirTexto(valor, "nome do bem", TAMANHO_MAXIMO);
    }
}
