package com.assetly.dominio.garantia;

import com.assetly.dominio.bem.BemId;
import org.junit.jupiter.api.Test;

import java.time.LocalDate;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class GarantiaTeste {

    @Test
    void deveRejeitarPeriodoTerminandoAntesDoInicio() {
        var iniciaEm = LocalDate.of(2026, 1, 10);
        var terminaEm = LocalDate.of(2026, 1, 9);

        assertThatThrownBy(() -> new PeriodoGarantia(iniciaEm, terminaEm))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessageContaining("data final");
    }

    @Test
    void deveCalcularStatusDaGarantia() {
        var garantia = Garantia.criar(
                BemId.novo(),
                TipoGarantia.FABRICANTE,
                "Acme",
                LocalDate.of(2026, 1, 1),
                LocalDate.of(2026, 12, 31)
        );

        assertThat(garantia.statusEm(LocalDate.of(2025, 12, 31))).isEqualTo(StatusGarantia.NAO_INICIADA);
        assertThat(garantia.statusEm(LocalDate.of(2026, 6, 1))).isEqualTo(StatusGarantia.ATIVA);
        assertThat(garantia.statusEm(LocalDate.of(2026, 12, 15))).isEqualTo(StatusGarantia.PERTO_DO_VENCIMENTO);
        assertThat(garantia.statusEm(LocalDate.of(2027, 1, 1))).isEqualTo(StatusGarantia.VENCIDA);
    }
}
