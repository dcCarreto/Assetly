package com.assetly.infraestrutura.repositorio;

import com.assetly.dominio.alerta.Alerta;
import com.assetly.dominio.alerta.AlertaId;
import com.assetly.dominio.alerta.SeveridadeAlerta;
import com.assetly.dominio.alerta.StatusAlerta;
import com.assetly.dominio.alerta.TipoAlerta;
import com.assetly.dominio.bem.BemId;

import java.sql.ResultSet;
import java.sql.SQLException;
import java.util.UUID;

final class MapeadorAlerta {

    private MapeadorAlerta() {
    }

    static Alerta mapear(ResultSet resultado) throws SQLException {
        return Alerta.restaurar(
                new AlertaId(UUID.fromString(resultado.getString("id"))),
                new BemId(UUID.fromString(resultado.getString("bem_id"))),
                TipoAlerta.valueOf(resultado.getString("tipo")),
                SeveridadeAlerta.valueOf(resultado.getString("severidade")),
                resultado.getString("mensagem"),
                ConversorSqlite.data(resultado, "criado_em"),
                ConversorSqlite.data(resultado, "prazo_em"),
                StatusAlerta.valueOf(resultado.getString("status"))
        );
    }
}
