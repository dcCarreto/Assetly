package com.assetly.infraestrutura.armazenamento;

import java.nio.file.Path;

public final class CaminhosArmazenamentoLocal {

    private static final String DIRETORIO_APLICACAO = ".assetly";
    private static final String DIRETORIO_BANCO = "banco";
    private static final String DIRETORIO_DOCUMENTOS = "documentos";
    private static final String ARQUIVO_BANCO = "assetly.sqlite3";

    private CaminhosArmazenamentoLocal() {
    }

    public static Path diretorioBase() {
        return Path.of(System.getProperty("user.home"), DIRETORIO_APLICACAO);
    }

    public static Path diretorioBanco() {
        return diretorioBase().resolve(DIRETORIO_BANCO);
    }

    public static Path arquivoBanco() {
        return diretorioBanco().resolve(ARQUIVO_BANCO);
    }

    public static Path diretorioDocumentos() {
        return diretorioBase().resolve(DIRETORIO_DOCUMENTOS);
    }
}
