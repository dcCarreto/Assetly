package com.assetly.dominio.garantia;

import java.util.Objects;
import java.util.UUID;

public record GarantiaId(UUID valor) {

    public GarantiaId {
        valor = Objects.requireNonNull(valor, "id da garantia é obrigatório");
    }

    public static GarantiaId novo() {
        return new GarantiaId(UUID.randomUUID());
    }
}
