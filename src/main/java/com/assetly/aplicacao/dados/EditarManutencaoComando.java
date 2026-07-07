package com.assetly.aplicacao.dados;

import com.assetly.dominio.manutencao.ManutencaoId;
import com.assetly.dominio.manutencao.TipoManutencao;

import java.time.LocalDate;

public record EditarManutencaoComando(
        ManutencaoId id,
        TipoManutencao tipo,
        String descricao,
        LocalDate agendadaPara
) {
}
