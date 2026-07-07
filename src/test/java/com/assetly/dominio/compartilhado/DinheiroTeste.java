package com.assetly.dominio.compartilhado;

import org.junit.jupiter.api.Test;

import java.math.BigDecimal;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class DinheiroTeste {

    @Test
    void deveNormalizarEscalaDoValor() {
        var dinheiro = Dinheiro.de(new BigDecimal("1999.995"), "USD");

        assertThat(dinheiro.valor()).isEqualByComparingTo("2000.00");
        assertThat(dinheiro.moeda().getCurrencyCode()).isEqualTo("USD");
    }

    @Test
    void deveRejeitarValorNegativo() {
        assertThatThrownBy(() -> Dinheiro.de(new BigDecimal("-1.00"), "USD"))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessageContaining("valor");
    }
}
