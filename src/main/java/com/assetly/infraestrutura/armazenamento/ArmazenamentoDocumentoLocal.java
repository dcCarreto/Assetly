package com.assetly.infraestrutura.armazenamento;

import com.assetly.aplicacao.servico.ArmazenamentoDocumento;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.text.Normalizer;
import java.util.Locale;
import java.util.Objects;
import java.util.UUID;

public final class ArmazenamentoDocumentoLocal implements ArmazenamentoDocumento {

    private static final String NOME_PADRAO = "documento";

    private final Path diretorioDocumentos;

    public ArmazenamentoDocumentoLocal(Path diretorioDocumentos) {
        this.diretorioDocumentos = Objects.requireNonNull(diretorioDocumentos, "diretório de documentos é obrigatório")
                .toAbsolutePath()
                .normalize();
    }

    public static ArmazenamentoDocumentoLocal padrao() {
        return new ArmazenamentoDocumentoLocal(CaminhosArmazenamentoLocal.diretorioDocumentos());
    }

    @Override
    public Path armazenar(Path arquivoOrigem) {
        Objects.requireNonNull(arquivoOrigem, "arquivo de origem é obrigatório");
        var origem = arquivoOrigem.toAbsolutePath().normalize();
        if (!Files.isRegularFile(origem)) {
            throw new IllegalArgumentException("arquivo do documento não encontrado: " + origem);
        }
        if (origem.startsWith(diretorioDocumentos)) {
            return origem;
        }

        try {
            Files.createDirectories(diretorioDocumentos);
            var destino = diretorioDocumentos.resolve(nomeDestino(origem));
            return Files.copy(origem, destino).toAbsolutePath().normalize();
        } catch (IOException excecao) {
            throw new IllegalStateException("não foi possível armazenar o documento localmente", excecao);
        }
    }

    private String nomeDestino(Path origem) {
        var nomeOriginal = origem.getFileName() == null ? NOME_PADRAO : origem.getFileName().toString();
        var nomeSeguro = normalizarNomeArquivo(nomeOriginal);
        return UUID.randomUUID() + "-" + nomeSeguro;
    }

    private String normalizarNomeArquivo(String nome) {
        var semAcentos = Normalizer.normalize(nome, Normalizer.Form.NFD)
                .replaceAll("\\p{M}", "");
        var seguro = semAcentos.toLowerCase(Locale.ROOT)
                .replaceAll("[^a-z0-9._-]", "-")
                .replaceAll("-+", "-")
                .replaceAll("(^[-.]+)|([- .]+$)", "");
        return seguro.isBlank() ? NOME_PADRAO : seguro;
    }
}
