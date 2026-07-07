package com.assetly.infraestrutura.repositorio;

import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.garantia.Garantia;
import com.assetly.dominio.garantia.GarantiaId;
import com.assetly.dominio.garantia.TipoGarantia;

import java.sql.ResultSet;
import java.sql.SQLException;
import java.util.UUID;

final class MapeadorGarantia {

    private MapeadorGarantia() {
    }

    static Garantia mapear(ResultSet resultado) throws SQLException {
        return Garantia.restaurar(
                new GarantiaId(UUID.fromString(resultado.getString("id"))),
                new BemId(UUID.fromString(resultado.getString("bem_id"))),
                TipoGarantia.valueOf(resultado.getString("tipo")),
                resultado.getString("fornecedor"),
                ConversorSqlite.data(resultado, "inicia_em"),
                ConversorSqlite.data(resultado, "termina_em"),
                resultado.getString("contato_suporte")
        );
    }
}
