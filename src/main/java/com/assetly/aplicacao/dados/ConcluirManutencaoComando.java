package com.assetly.aplicacao.dados;

import com.assetly.dominio.manutencao.ManutencaoId;

import java.math.BigDecimal;
import java.time.LocalDate;

public record ConcluirManutencaoComando(
        ManutencaoId id,
        LocalDate concluidaEm,
        BigDecimal custo,
        String codigoMoeda
) {
}
