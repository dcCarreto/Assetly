package com.assetly.aplicacao.dados;

import com.assetly.dominio.alerta.SeveridadeAlerta;
import com.assetly.dominio.alerta.TipoAlerta;
import com.assetly.dominio.bem.BemId;

import java.time.LocalDate;

public record CriarAlertaComando(
        BemId bemId,
        TipoAlerta tipo,
        SeveridadeAlerta severidade,
        String mensagem,
        LocalDate criadoEm,
        LocalDate prazoEm
) {
}
