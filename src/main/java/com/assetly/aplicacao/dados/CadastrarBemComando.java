package com.assetly.aplicacao.dados;

import com.assetly.dominio.bem.TipoBem;

import java.math.BigDecimal;
import java.time.LocalDate;

public record CadastrarBemComando(
        String nome,
        TipoBem tipo,
        LocalDate compradoEm,
        BigDecimal precoCompra,
        String codigoMoeda,
        String observacoes
) {
}
