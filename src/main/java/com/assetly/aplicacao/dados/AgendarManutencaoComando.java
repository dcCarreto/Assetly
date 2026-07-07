package com.assetly.aplicacao.dados;

import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.manutencao.TipoManutencao;

import java.time.LocalDate;

public record AgendarManutencaoComando(
        BemId bemId,
        TipoManutencao tipo,
        String descricao,
        LocalDate agendadaPara
) {
}
