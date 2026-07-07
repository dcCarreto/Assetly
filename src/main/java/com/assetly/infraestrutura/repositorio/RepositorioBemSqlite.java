package com.assetly.infraestrutura.repositorio;

import com.assetly.aplicacao.repositorio.RepositorioBem;
import com.assetly.dominio.bem.Bem;
import com.assetly.dominio.bem.BemId;
import com.assetly.infraestrutura.banco.ExcecaoBancoDados;
import com.assetly.infraestrutura.banco.FabricaConexaoSqlite;

import java.sql.SQLException;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.Optional;

public final class RepositorioBemSqlite implements RepositorioBem {

    private final FabricaConexaoSqlite fabricaConexao;

    public RepositorioBemSqlite(FabricaConexaoSqlite fabricaConexao) {
        this.fabricaConexao = Objects.requireNonNull(fabricaConexao, "fábrica de conexão é obrigatória");
    }

    @Override
    public Optional<Bem> buscarPorId(BemId id) {
        try (var conexao = fabricaConexao.abrir();
             var comando = conexao.prepareStatement("SELECT * FROM bens WHERE id = ?")) {
            comando.setString(1, id.valor().toString());
            try (var resultado = comando.executeQuery()) {
                return resultado.next() ? Optional.of(MapeadorBem.mapear(resultado)) : Optional.empty();
            }
        } catch (SQLException excecao) {
            throw new ExcecaoBancoDados("Não foi possível buscar bem", excecao);
        }
    }

    @Override
    public List<Bem> listar() {
        try (var conexao = fabricaConexao.abrir();
             var comando = conexao.prepareStatement("SELECT * FROM bens ORDER BY nome")) {
            try (var resultado = comando.executeQuery()) {
                var bens = new ArrayList<Bem>();
                while (resultado.next()) {
                    bens.add(MapeadorBem.mapear(resultado));
                }
                return bens;
            }
        } catch (SQLException excecao) {
            throw new ExcecaoBancoDados("Não foi possível listar bens", excecao);
        }
    }

    @Override
    public void salvar(Bem bem) {
        var sql = """
                INSERT INTO bens (id, nome, tipo, status, comprado_em, preco_compra, moeda, observacoes)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?)
                ON CONFLICT(id) DO UPDATE SET
                    nome = excluded.nome,
                    tipo = excluded.tipo,
                    status = excluded.status,
                    comprado_em = excluded.comprado_em,
                    preco_compra = excluded.preco_compra,
                    moeda = excluded.moeda,
                    observacoes = excluded.observacoes
                """;
        try (var conexao = fabricaConexao.abrir();
             var comando = conexao.prepareStatement(sql)) {
            var preco = bem.precoCompra().orElse(null);
            comando.setString(1, bem.id().valor().toString());
            comando.setString(2, bem.nome().valor());
            comando.setString(3, bem.tipo().name());
            comando.setString(4, bem.status().name());
            comando.setString(5, bem.compradoEm().map(ConversorSqlite::texto).orElse(null));
            comando.setString(6, ConversorSqlite.valor(preco));
            comando.setString(7, ConversorSqlite.moeda(preco));
            comando.setString(8, bem.observacoes());
            comando.executeUpdate();
        } catch (SQLException excecao) {
            throw new ExcecaoBancoDados("Não foi possível salvar bem", excecao);
        }
    }
}
