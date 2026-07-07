package com.assetly.dominio.bem;

import java.util.Objects;
import java.util.UUID;

public record BemId(UUID valor) {

    public BemId {
        valor = Objects.requireNonNull(valor, "id do bem é obrigatório");
    }

    public static BemId novo() {
        return new BemId(UUID.randomUUID());
    }
}
