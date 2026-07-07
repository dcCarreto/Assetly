package com.assetly.infraestrutura.repositorio;

import com.assetly.aplicacao.repositorio.RepositorioGarantia;
import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.garantia.Garantia;
import com.assetly.dominio.garantia.GarantiaId;
import com.assetly.infraestrutura.banco.ExcecaoBancoDados;
import com.assetly.infraestrutura.banco.FabricaConexaoSqlite;

import java.sql.SQLException;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.Optional;

public final class RepositorioGarantiaSqlite implements RepositorioGarantia {

    private final FabricaConexaoSqlite fabricaConexao;

    public RepositorioGarantiaSqlite(FabricaConexaoSqlite fabricaConexao) {
        this.fabricaConexao = Objects.requireNonNull(fabricaConexao, "fábrica de conexão é obrigatória");
    }

    @Override
    public Optional<Garantia> buscarPorId(GarantiaId id) {
        try (var conexao = fabricaConexao.abrir();
             var comando = conexao.prepareStatement("SELECT * FROM garantias WHERE id = ?")) {
            comando.setString(1, id.valor().toString());
            try (var resultado = comando.executeQuery()) {
                return resultado.next() ? Optional.of(MapeadorGarantia.mapear(resultado)) : Optional.empty();
            }
        } catch (SQLException excecao) {
            throw new ExcecaoBancoDados("Não foi possível buscar garantia", excecao);
        }
    }

    @Override
    public List<Garantia> listar() {
        return consultar("SELECT * FROM garantias ORDER BY termina_em, fornecedor", null);
    }

    @Override
    public List<Garantia> listarPorBem(BemId bemId) {
        return consultar("SELECT * FROM garantias WHERE bem_id = ? ORDER BY termina_em, fornecedor", bemId);
    }

    @Override
    public void salvar(Garantia garantia) {
        var sql = """
                INSERT INTO garantias (id, bem_id, tipo, fornecedor, inicia_em, termina_em, contato_suporte)
                VALUES (?, ?, ?, ?, ?, ?, ?)
                ON CONFLICT(id) DO UPDATE SET
                    bem_id = excluded.bem_id,
                    tipo = excluded.tipo,
                    fornecedor = excluded.fornecedor,
                    inicia_em = excluded.inicia_em,
                    termina_em = excluded.termina_em,
                    contato_suporte = excluded.contato_suporte
                """;
        try (var conexao = fabricaConexao.abrir();
             var comando = conexao.prepareStatement(sql)) {
            comando.setString(1, garantia.id().valor().toString());
            comando.setString(2, garantia.bemId().valor().toString());
            comando.setString(3, garantia.tipo().name());
            comando.setString(4, garantia.fornecedor());
            comando.setString(5, ConversorSqlite.texto(garantia.periodo().iniciaEm()));
            comando.setString(6, ConversorSqlite.texto(garantia.periodo().terminaEm()));
            comando.setString(7, garantia.contatoSuporte());
            comando.executeUpdate();
        } catch (SQLException excecao) {
            throw new ExcecaoBancoDados("Não foi possível salvar garantia", excecao);
        }
    }

    private List<Garantia> consultar(String sql, BemId bemId) {
        try (var conexao = fabricaConexao.abrir();
             var comando = conexao.prepareStatement(sql)) {
            if (bemId != null) {
                comando.setString(1, bemId.valor().toString());
            }
            try (var resultado = comando.executeQuery()) {
                var garantias = new ArrayList<Garantia>();
                while (resultado.next()) {
                    garantias.add(MapeadorGarantia.mapear(resultado));
                }
                return garantias;
            }
        } catch (SQLException excecao) {
            throw new ExcecaoBancoDados("Não foi possível listar garantias", excecao);
        }
    }
}
