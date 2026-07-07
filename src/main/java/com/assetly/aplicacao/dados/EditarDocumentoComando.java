package com.assetly.aplicacao.dados;

import com.assetly.dominio.documento.DocumentoId;
import com.assetly.dominio.documento.TipoDocumento;

import java.time.LocalDate;

public record EditarDocumentoComando(
        DocumentoId id,
        TipoDocumento tipo,
        String nome,
        String caminhoLocal,
        LocalDate registradoEm
) {
}
