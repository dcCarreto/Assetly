package com.assetly.dominio.compartilhado;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.Objects;

public final class Validacao {

    private Validacao() {
    }

    public static String exigirTexto(String valor, String nomeCampo, int tamanhoMaximo) {
        Objects.requireNonNull(valor, nomeCampo + " é obrigatório");
        var normalizado = valor.trim();
        if (normalizado.isEmpty()) {
            throw new IllegalArgumentException(nomeCampo + " não pode ficar em branco");
        }
        if (normalizado.length() > tamanhoMaximo) {
            throw new IllegalArgumentException(nomeCampo + " não pode exceder " + tamanhoMaximo + " caracteres");
        }
        return normalizado;
    }

    public static String textoOpcional(String valor, String nomeCampo, int tamanhoMaximo) {
        if (valor == null || valor.isBlank()) {
            return "";
        }
        return exigirTexto(valor, nomeCampo, tamanhoMaximo);
    }

    public static LocalDate exigirDataNaoFutura(LocalDate valor, String nomeCampo, LocalDate hoje) {
        Objects.requireNonNull(valor, nomeCampo + " é obrigatória");
        Objects.requireNonNull(hoje, "data de referência é obrigatória");
        if (valor.isAfter(hoje)) {
            throw new IllegalArgumentException(nomeCampo + " não pode estar no futuro");
        }
        return valor;
    }

    public static BigDecimal exigirNaoNegativo(BigDecimal valor, String nomeCampo) {
        Objects.requireNonNull(valor, nomeCampo + " é obrigatório");
        if (valor.signum() < 0) {
            throw new IllegalArgumentException(nomeCampo + " não pode ser negativo");
        }
        return valor;
    }
}
