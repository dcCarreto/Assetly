package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.dados.EditarDocumentoComando;
import com.assetly.aplicacao.repositorio.RepositorioDocumento;
import com.assetly.dominio.documento.CaminhoDocumentoLocal;
import com.assetly.dominio.documento.Documento;
import com.assetly.dominio.documento.NomeDocumento;

import java.util.Objects;

public final class EditarDocumento {

    private final RepositorioDocumento repositorioDocumento;

    public EditarDocumento(RepositorioDocumento repositorioDocumento) {
        this.repositorioDocumento = Objects.requireNonNull(repositorioDocumento, "repositório de documentos é obrigatório");
    }

    public Documento executar(EditarDocumentoComando comando) {
        Objects.requireNonNull(comando, "comando é obrigatório");
        var documento = ApoioCasosUso.exigirDocumento(repositorioDocumento, comando.id());

        documento.renomear(new NomeDocumento(comando.nome()));
        documento.alterarTipo(comando.tipo());
        documento.substituirArquivo(CaminhoDocumentoLocal.de(comando.caminhoLocal()), comando.registradoEm());

        repositorioDocumento.salvar(documento);
        return documento;
    }
}
