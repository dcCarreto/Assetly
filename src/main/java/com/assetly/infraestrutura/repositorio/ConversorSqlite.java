package com.assetly.infraestrutura.repositorio;

import com.assetly.dominio.compartilhado.Dinheiro;

import java.math.BigDecimal;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.time.LocalDate;

final class ConversorSqlite {

    private ConversorSqlite() {
    }

    static LocalDate data(ResultSet resultado, String coluna) throws SQLException {
        var valor = resultado.getString(coluna);
        return valor == null ? null : LocalDate.parse(valor);
    }

    static String texto(LocalDate data) {
        return data == null ? null : data.toString();
    }

    static Dinheiro dinheiro(ResultSet resultado, String colunaValor, String colunaMoeda) throws SQLException {
        var valor = resultado.getString(colunaValor);
        if (valor == null) {
            return null;
        }
        return Dinheiro.de(new BigDecimal(valor), resultado.getString(colunaMoeda));
    }

    static String valor(Dinheiro dinheiro) {
        return dinheiro == null ? null : dinheiro.valor().toPlainString();
    }

    static String moeda(Dinheiro dinheiro) {
        return dinheiro == null ? null : dinheiro.moeda().getCurrencyCode();
    }
}
