package com.assetly.dominio.garantia;

import java.time.LocalDate;
import java.util.Objects;

public record PeriodoGarantia(LocalDate iniciaEm, LocalDate terminaEm) {

    public PeriodoGarantia {
        iniciaEm = Objects.requireNonNull(iniciaEm, "data inicial da garantia é obrigatória");
        terminaEm = Objects.requireNonNull(terminaEm, "data final da garantia é obrigatória");
        if (terminaEm.isBefore(iniciaEm)) {
            throw new IllegalArgumentException("data final da garantia não pode ser anterior à data inicial");
        }
    }

    public boolean contem(LocalDate data) {
        Objects.requireNonNull(data, "data é obrigatória");
        return !data.isBefore(iniciaEm) && !data.isAfter(terminaEm);
    }

    public boolean estaVencidaEm(LocalDate data) {
        Objects.requireNonNull(data, "data é obrigatória");
        return data.isAfter(terminaEm);
    }

    public boolean venceEmAte(LocalDate data, int dias) {
        Objects.requireNonNull(data, "data é obrigatória");
        if (dias < 0) {
            throw new IllegalArgumentException("dias não pode ser negativo");
        }
        return contem(data) && !terminaEm.isAfter(data.plusDays(dias));
    }
}
