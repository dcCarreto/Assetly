package com.assetly.dominio.documento;

import java.nio.file.Path;
import java.util.Objects;

public record CaminhoDocumentoLocal(Path valor) {

    public CaminhoDocumentoLocal {
        valor = Objects.requireNonNull(valor, "caminho do documento é obrigatório").normalize();
        if (valor.toString().isBlank()) {
            throw new IllegalArgumentException("caminho do documento não pode ficar em branco");
        }
    }

    public static CaminhoDocumentoLocal de(String valor) {
        return new CaminhoDocumentoLocal(Path.of(valor));
    }
}
