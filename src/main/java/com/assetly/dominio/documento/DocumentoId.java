package com.assetly.dominio.documento;

import java.util.Objects;
import java.util.UUID;

public record DocumentoId(UUID valor) {

    public DocumentoId {
        valor = Objects.requireNonNull(valor, "id do documento é obrigatório");
    }

    public static DocumentoId novo() {
        return new DocumentoId(UUID.randomUUID());
    }
}
