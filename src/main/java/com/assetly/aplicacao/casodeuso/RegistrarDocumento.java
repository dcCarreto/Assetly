package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.dados.RegistrarDocumentoComando;
import com.assetly.aplicacao.repositorio.RepositorioBem;
import com.assetly.aplicacao.repositorio.RepositorioDocumento;
import com.assetly.dominio.documento.CaminhoDocumentoLocal;
import com.assetly.dominio.documento.Documento;
import com.assetly.dominio.documento.NomeDocumento;

import java.util.Objects;

public final class RegistrarDocumento {

    private final RepositorioBem repositorioBem;
    private final RepositorioDocumento repositorioDocumento;

    public RegistrarDocumento(RepositorioBem repositorioBem, RepositorioDocumento repositorioDocumento) {
        this.repositorioBem = Objects.requireNonNull(repositorioBem, "repositório de bens é obrigatório");
        this.repositorioDocumento = Objects.requireNonNull(repositorioDocumento, "repositório de documentos é obrigatório");
    }

    public Documento executar(RegistrarDocumentoComando comando) {
        Objects.requireNonNull(comando, "comando é obrigatório");
        ApoioCasosUso.exigirBem(repositorioBem, comando.bemId());

        var documento = Documento.registrar(
                comando.bemId(),
                comando.tipo(),
                new NomeDocumento(comando.nome()),
                CaminhoDocumentoLocal.de(comando.caminhoLocal()),
                comando.registradoEm()
        );
        repositorioDocumento.salvar(documento);
        return documento;
    }
}
