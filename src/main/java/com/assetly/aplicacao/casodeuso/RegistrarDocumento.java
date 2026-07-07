package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.dados.RegistrarDocumentoComando;
import com.assetly.aplicacao.repositorio.RepositorioBem;
import com.assetly.aplicacao.repositorio.RepositorioDocumento;
import com.assetly.aplicacao.servico.ArmazenamentoDocumento;
import com.assetly.dominio.documento.CaminhoDocumentoLocal;
import com.assetly.dominio.documento.Documento;
import com.assetly.dominio.documento.NomeDocumento;

import java.nio.file.Path;
import java.util.Objects;

public final class RegistrarDocumento {

    private final RepositorioBem repositorioBem;
    private final RepositorioDocumento repositorioDocumento;
    private final ArmazenamentoDocumento armazenamentoDocumento;

    public RegistrarDocumento(RepositorioBem repositorioBem, RepositorioDocumento repositorioDocumento) {
        this(repositorioBem, repositorioDocumento, origem -> origem);
    }

    public RegistrarDocumento(
            RepositorioBem repositorioBem,
            RepositorioDocumento repositorioDocumento,
            ArmazenamentoDocumento armazenamentoDocumento
    ) {
        this.repositorioBem = Objects.requireNonNull(repositorioBem, "repositório de bens é obrigatório");
        this.repositorioDocumento = Objects.requireNonNull(repositorioDocumento, "repositório de documentos é obrigatório");
        this.armazenamentoDocumento = Objects.requireNonNull(armazenamentoDocumento, "armazenamento de documentos é obrigatório");
    }

    public Documento executar(RegistrarDocumentoComando comando) {
        Objects.requireNonNull(comando, "comando é obrigatório");
        ApoioCasosUso.exigirBem(repositorioBem, comando.bemId());
        var caminhoArmazenado = armazenamentoDocumento.armazenar(Path.of(comando.caminhoLocal()));

        var documento = Documento.registrar(
                comando.bemId(),
                comando.tipo(),
                new NomeDocumento(comando.nome()),
                new CaminhoDocumentoLocal(caminhoArmazenado),
                comando.registradoEm()
        );
        repositorioDocumento.salvar(documento);
        return documento;
    }
}
