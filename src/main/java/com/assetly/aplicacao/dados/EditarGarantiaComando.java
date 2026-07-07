package com.assetly.aplicacao.dados;

import com.assetly.dominio.garantia.GarantiaId;
import com.assetly.dominio.garantia.TipoGarantia;

import java.time.LocalDate;

public record EditarGarantiaComando(
        GarantiaId id,
        TipoGarantia tipo,
        String fornecedor,
        LocalDate iniciaEm,
        LocalDate terminaEm,
        String contatoSuporte
) {
}
