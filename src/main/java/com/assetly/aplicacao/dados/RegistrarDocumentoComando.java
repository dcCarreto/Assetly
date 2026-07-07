package com.assetly.aplicacao.dados;

import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.documento.TipoDocumento;

import java.time.LocalDate;

public record RegistrarDocumentoComando(
        BemId bemId,
        TipoDocumento tipo,
        String nome,
        String caminhoLocal,
        LocalDate registradoEm
) {
}
