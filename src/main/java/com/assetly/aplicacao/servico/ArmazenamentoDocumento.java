package com.assetly.aplicacao.servico;

import java.nio.file.Path;

public interface ArmazenamentoDocumento {

    Path armazenar(Path arquivoOrigem);
}
