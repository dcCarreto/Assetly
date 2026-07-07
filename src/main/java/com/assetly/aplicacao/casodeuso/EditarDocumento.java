package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.dados.EditarDocumentoComando;
import com.assetly.aplicacao.repositorio.RepositorioDocumento;
import com.assetly.aplicacao.servico.ArmazenamentoDocumento;
import com.assetly.dominio.documento.CaminhoDocumentoLocal;
import com.assetly.dominio.documento.Documento;
import com.assetly.dominio.documento.NomeDocumento;

import java.nio.file.Path;
import java.util.Objects;

public final class EditarDocumento {

    private final RepositorioDocumento repositorioDocumento;
    private final ArmazenamentoDocumento armazenamentoDocumento;

    public EditarDocumento(RepositorioDocumento repositorioDocumento) {
        this(repositorioDocumento, origem -> origem);
    }

    public EditarDocumento(RepositorioDocumento repositorioDocumento, ArmazenamentoDocumento armazenamentoDocumento) {
        this.repositorioDocumento = Objects.requireNonNull(repositorioDocumento, "repositório de documentos é obrigatório");
        this.armazenamentoDocumento = Objects.requireNonNull(armazenamentoDocumento, "armazenamento de documentos é obrigatório");
    }

    public Documento executar(EditarDocumentoComando comando) {
        Objects.requireNonNull(comando, "comando é obrigatório");
        var documento = ApoioCasosUso.exigirDocumento(repositorioDocumento, comando.id());
        var caminhoArmazenado = armazenamentoDocumento.armazenar(Path.of(comando.caminhoLocal()));

        documento.renomear(new NomeDocumento(comando.nome()));
        documento.alterarTipo(comando.tipo());
        documento.substituirArquivo(new CaminhoDocumentoLocal(caminhoArmazenado), comando.registradoEm());

        repositorioDocumento.salvar(documento);
        return documento;
    }
}
