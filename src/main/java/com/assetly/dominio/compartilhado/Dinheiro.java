package com.assetly.dominio.compartilhado;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.util.Currency;
import java.util.Objects;

public record Dinheiro(BigDecimal valor, Currency moeda) {

    public Dinheiro {
        valor = Validacao.exigirNaoNegativo(valor, "valor").setScale(2, RoundingMode.HALF_UP);
        moeda = Objects.requireNonNull(moeda, "moeda é obrigatória");
    }

    public static Dinheiro de(BigDecimal valor, String codigoMoeda) {
        return new Dinheiro(valor, Currency.getInstance(codigoMoeda));
    }

    public static Dinheiro zero(String codigoMoeda) {
        return de(BigDecimal.ZERO, codigoMoeda);
    }
}
