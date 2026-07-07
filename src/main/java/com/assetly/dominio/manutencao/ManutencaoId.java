package com.assetly.dominio.manutencao;

import java.util.Objects;
import java.util.UUID;

public record ManutencaoId(UUID valor) {

    public ManutencaoId {
        valor = Objects.requireNonNull(valor, "id da manutenção é obrigatório");
    }

    public static ManutencaoId novo() {
        return new ManutencaoId(UUID.randomUUID());
    }
}
