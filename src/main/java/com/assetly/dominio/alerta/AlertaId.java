package com.assetly.dominio.alerta;

import java.util.Objects;
import java.util.UUID;

public record AlertaId(UUID valor) {

    public AlertaId {
        valor = Objects.requireNonNull(valor, "id do alerta é obrigatório");
    }

    public static AlertaId novo() {
        return new AlertaId(UUID.randomUUID());
    }
}
