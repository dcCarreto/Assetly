package com.assetly.infraestrutura.armazenamento;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

import java.nio.file.Files;
import java.nio.file.Path;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class ArmazenamentoDocumentoLocalTeste {

    @TempDir
    Path temporario;

    @Test
    void deveCopiarDocumentoParaDiretorioLocalComNomeSeguro() throws Exception {
        var origem = temporario.resolve("Nota Fiscal Ágil.pdf");
        Files.writeString(origem, "conteúdo");
        var diretorioDocumentos = temporario.resolve("assetly").resolve("documentos");
        var armazenamento = new ArmazenamentoDocumentoLocal(diretorioDocumentos);

        var destino = armazenamento.armazenar(origem);

        assertThat(destino.startsWith(diretorioDocumentos.toAbsolutePath().normalize())).isTrue();
        assertThat(destino.getFileName().toString()).endsWith("-nota-fiscal-agil.pdf");
        assertThat(Files.readString(destino)).isEqualTo("conteúdo");
    }

    @Test
    void deveManterCaminhoQuandoArquivoJaEstaNoDiretorioLocal() throws Exception {
        var diretorioDocumentos = temporario.resolve("documentos");
        Files.createDirectories(diretorioDocumentos);
        var documentoInterno = diretorioDocumentos.resolve("documento.pdf");
        Files.writeString(documentoInterno, "conteúdo");
        var armazenamento = new ArmazenamentoDocumentoLocal(diretorioDocumentos);

        var destino = armazenamento.armazenar(documentoInterno);

        assertThat(destino).isEqualTo(documentoInterno.toAbsolutePath().normalize());
    }

    @Test
    void deveFalharQuandoArquivoOrigemNaoExiste() {
        var armazenamento = new ArmazenamentoDocumentoLocal(temporario.resolve("documentos"));

        assertThatThrownBy(() -> armazenamento.armazenar(temporario.resolve("ausente.pdf")))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessageContaining("arquivo do documento não encontrado");
    }
}
