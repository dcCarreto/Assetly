package com.assetly.infraestrutura.repositorio;

import com.assetly.dominio.bem.Bem;
import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.bem.NomeBem;
import com.assetly.dominio.bem.StatusBem;
import com.assetly.dominio.bem.TipoBem;

import java.sql.ResultSet;
import java.sql.SQLException;
import java.util.UUID;

final class MapeadorBem {

    private MapeadorBem() {
    }

    static Bem mapear(ResultSet resultado) throws SQLException {
        return Bem.restaurar(
                new BemId(UUID.fromString(resultado.getString("id"))),
                new NomeBem(resultado.getString("nome")),
                TipoBem.valueOf(resultado.getString("tipo")),
                StatusBem.valueOf(resultado.getString("status")),
                ConversorSqlite.data(resultado, "comprado_em"),
                ConversorSqlite.dinheiro(resultado, "preco_compra", "moeda"),
                resultado.getString("observacoes")
        );
    }
}
