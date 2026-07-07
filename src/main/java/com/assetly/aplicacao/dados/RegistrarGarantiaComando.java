package com.assetly.aplicacao.dados;

import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.garantia.TipoGarantia;

import java.time.LocalDate;

public record RegistrarGarantiaComando(
        BemId bemId,
        TipoGarantia tipo,
        String fornecedor,
        LocalDate iniciaEm,
        LocalDate terminaEm,
        String contatoSuporte
) {
}
