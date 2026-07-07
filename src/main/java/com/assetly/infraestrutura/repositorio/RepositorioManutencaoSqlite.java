package com.assetly.infraestrutura.repositorio;

import com.assetly.aplicacao.repositorio.RepositorioManutencao;
import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.manutencao.Manutencao;
import com.assetly.dominio.manutencao.ManutencaoId;
import com.assetly.infraestrutura.banco.ExcecaoBancoDados;
import com.assetly.infraestrutura.banco.FabricaConexaoSqlite;

import java.sql.SQLException;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.Optional;

public final class RepositorioManutencaoSqlite implements RepositorioManutencao {

    private final FabricaConexaoSqlite fabricaConexao;

    public RepositorioManutencaoSqlite(FabricaConexaoSqlite fabricaConexao) {
        this.fabricaConexao = Objects.requireNonNull(fabricaConexao, "fábrica de conexão é obrigatória");
    }

    @Override
    public Optional<Manutencao> buscarPorId(ManutencaoId id) {
        try (var conexao = fabricaConexao.abrir();
             var comando = conexao.prepareStatement("SELECT * FROM manutencoes WHERE id = ?")) {
            comando.setString(1, id.valor().toString());
            try (var resultado = comando.executeQuery()) {
                return resultado.next() ? Optional.of(MapeadorManutencao.mapear(resultado)) : Optional.empty();
            }
        } catch (SQLException excecao) {
            throw new ExcecaoBancoDados("Não foi possível buscar manutenção", excecao);
        }
    }

    @Override
    public List<Manutencao> listar() {
        return consultar("SELECT * FROM manutencoes ORDER BY agendada_para DESC, descricao", null);
    }

    @Override
    public List<Manutencao> listarPorBem(BemId bemId) {
        return consultar("SELECT * FROM manutencoes WHERE bem_id = ? ORDER BY agendada_para DESC, descricao", bemId);
    }

    @Override
    public void salvar(Manutencao manutencao) {
        var sql = """
                INSERT INTO manutencoes (id, bem_id, tipo, descricao, agendada_para, concluida_em, custo, moeda, status)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
                ON CONFLICT(id) DO UPDATE SET
                    bem_id = excluded.bem_id,
                    tipo = excluded.tipo,
                    descricao = excluded.descricao,
                    agendada_para = excluded.agendada_para,
                    concluida_em = excluded.concluida_em,
                    custo = excluded.custo,
                    moeda = excluded.moeda,
                    status = excluded.status
                """;
        try (var conexao = fabricaConexao.abrir();
             var comando = conexao.prepareStatement(sql)) {
            var custo = manutencao.custo().orElse(null);
            comando.setString(1, manutencao.id().valor().toString());
            comando.setString(2, manutencao.bemId().valor().toString());
            comando.setString(3, manutencao.tipo().name());
            comando.setString(4, manutencao.descricao());
            comando.setString(5, ConversorSqlite.texto(manutencao.agendadaPara()));
            comando.setString(6, manutencao.concluidaEm().map(ConversorSqlite::texto).orElse(null));
            comando.setString(7, ConversorSqlite.valor(custo));
            comando.setString(8, ConversorSqlite.moeda(custo));
            comando.setString(9, manutencao.status().name());
            comando.executeUpdate();
        } catch (SQLException excecao) {
            throw new ExcecaoBancoDados("Não foi possível salvar manutenção", excecao);
        }
    }

    private List<Manutencao> consultar(String sql, BemId bemId) {
        try (var conexao = fabricaConexao.abrir();
             var comando = conexao.prepareStatement(sql)) {
            if (bemId != null) {
                comando.setString(1, bemId.valor().toString());
            }
            try (var resultado = comando.executeQuery()) {
                var manutencoes = new ArrayList<Manutencao>();
                while (resultado.next()) {
                    manutencoes.add(MapeadorManutencao.mapear(resultado));
                }
                return manutencoes;
            }
        } catch (SQLException excecao) {
            throw new ExcecaoBancoDados("Não foi possível listar manutenções", excecao);
        }
    }
}
