package com.assetly.infraestrutura.repositorio;

import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.documento.CaminhoDocumentoLocal;
import com.assetly.dominio.documento.Documento;
import com.assetly.dominio.documento.DocumentoId;
import com.assetly.dominio.documento.NomeDocumento;
import com.assetly.dominio.documento.StatusDocumento;
import com.assetly.dominio.documento.TipoDocumento;

import java.sql.ResultSet;
import java.sql.SQLException;
import java.util.UUID;

final class MapeadorDocumento {

    private MapeadorDocumento() {
    }

    static Documento mapear(ResultSet resultado) throws SQLException {
        return Documento.restaurar(
                new DocumentoId(UUID.fromString(resultado.getString("id"))),
                new BemId(UUID.fromString(resultado.getString("bem_id"))),
                TipoDocumento.valueOf(resultado.getString("tipo")),
                new NomeDocumento(resultado.getString("nome")),
                CaminhoDocumentoLocal.de(resultado.getString("caminho_local")),
                ConversorSqlite.data(resultado, "registrado_em"),
                StatusDocumento.valueOf(resultado.getString("status"))
        );
    }
}
