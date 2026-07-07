package com.assetly.aplicacao.dados;

import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.bem.StatusBem;

public record AlterarStatusBemComando(BemId id, StatusBem status) {
}
