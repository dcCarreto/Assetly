package com.assetly.dominio.documento;

import com.assetly.dominio.compartilhado.Validacao;

public record NomeDocumento(String valor) {

    public static final int TAMANHO_MAXIMO = 160;

    public NomeDocumento {
        valor = Validacao.exigirTexto(valor, "nome do documento", TAMANHO_MAXIMO);
    }
}
