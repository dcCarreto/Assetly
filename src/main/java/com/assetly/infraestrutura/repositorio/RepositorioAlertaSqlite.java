package com.assetly.infraestrutura.repositorio;

import com.assetly.aplicacao.repositorio.RepositorioAlerta;
import com.assetly.dominio.alerta.Alerta;
import com.assetly.dominio.alerta.AlertaId;
import com.assetly.dominio.bem.BemId;
import com.assetly.infraestrutura.banco.ExcecaoBancoDados;
import com.assetly.infraestrutura.banco.FabricaConexaoSqlite;

import java.sql.SQLException;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.Optional;

public final class RepositorioAlertaSqlite implements RepositorioAlerta {

    private final FabricaConexaoSqlite fabricaConexao;

    public RepositorioAlertaSqlite(FabricaConexaoSqlite fabricaConexao) {
        this.fabricaConexao = Objects.requireNonNull(fabricaConexao, "fábrica de conexão é obrigatória");
    }

    @Override
    public Optional<Alerta> buscarPorId(AlertaId id) {
        try (var conexao = fabricaConexao.abrir();
             var comando = conexao.prepareStatement("SELECT * FROM alertas WHERE id = ?")) {
            comando.setString(1, id.valor().toString());
            try (var resultado = comando.executeQuery()) {
                return resultado.next() ? Optional.of(MapeadorAlerta.mapear(resultado)) : Optional.empty();
            }
        } catch (SQLException excecao) {
            throw new ExcecaoBancoDados("Não foi possível buscar alerta", excecao);
        }
    }

    @Override
    public List<Alerta> listar() {
        return consultar("SELECT * FROM alertas ORDER BY criado_em DESC, mensagem", null);
    }

    @Override
    public List<Alerta> listarPorBem(BemId bemId) {
        return consultar("SELECT * FROM alertas WHERE bem_id = ? ORDER BY criado_em DESC, mensagem", bemId);
    }

    @Override
    public void salvar(Alerta alerta) {
        var sql = """
                INSERT INTO alertas (id, bem_id, tipo, severidade, mensagem, criado_em, prazo_em, status)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?)
                ON CONFLICT(id) DO UPDATE SET
                    bem_id = excluded.bem_id,
                    tipo = excluded.tipo,
                    severidade = excluded.severidade,
                    mensagem = excluded.mensagem,
                    criado_em = excluded.criado_em,
                    prazo_em = excluded.prazo_em,
                    status = excluded.status
                """;
        try (var conexao = fabricaConexao.abrir();
             var comando = conexao.prepareStatement(sql)) {
            comando.setString(1, alerta.id().valor().toString());
            comando.setString(2, alerta.bemId().valor().toString());
            comando.setString(3, alerta.tipo().name());
            comando.setString(4, alerta.severidade().name());
            comando.setString(5, alerta.mensagem());
            comando.setString(6, ConversorSqlite.texto(alerta.criadoEm()));
            comando.setString(7, alerta.prazoEm().map(ConversorSqlite::texto).orElse(null));
            comando.setString(8, alerta.status().name());
            comando.executeUpdate();
        } catch (SQLException excecao) {
            throw new ExcecaoBancoDados("Não foi possível salvar alerta", excecao);
        }
    }

    private List<Alerta> consultar(String sql, BemId bemId) {
        try (var conexao = fabricaConexao.abrir();
             var comando = conexao.prepareStatement(sql)) {
            if (bemId != null) {
                comando.setString(1, bemId.valor().toString());
            }
            try (var resultado = comando.executeQuery()) {
                var alertas = new ArrayList<Alerta>();
                while (resultado.next()) {
                    alertas.add(MapeadorAlerta.mapear(resultado));
                }
                return alertas;
            }
        } catch (SQLException excecao) {
            throw new ExcecaoBancoDados("Não foi possível listar alertas", excecao);
        }
    }
}
