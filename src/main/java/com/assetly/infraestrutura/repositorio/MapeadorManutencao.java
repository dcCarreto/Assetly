package com.assetly.infraestrutura.repositorio;

import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.manutencao.Manutencao;
import com.assetly.dominio.manutencao.ManutencaoId;
import com.assetly.dominio.manutencao.StatusManutencao;
import com.assetly.dominio.manutencao.TipoManutencao;

import java.sql.ResultSet;
import java.sql.SQLException;
import java.util.UUID;

final class MapeadorManutencao {

    private MapeadorManutencao() {
    }

    static Manutencao mapear(ResultSet resultado) throws SQLException {
        return Manutencao.restaurar(
                new ManutencaoId(UUID.fromString(resultado.getString("id"))),
                new BemId(UUID.fromString(resultado.getString("bem_id"))),
                TipoManutencao.valueOf(resultado.getString("tipo")),
                resultado.getString("descricao"),
                ConversorSqlite.data(resultado, "agendada_para"),
                ConversorSqlite.data(resultado, "concluida_em"),
                ConversorSqlite.dinheiro(resultado, "custo", "moeda"),
                StatusManutencao.valueOf(resultado.getString("status"))
        );
    }
}
