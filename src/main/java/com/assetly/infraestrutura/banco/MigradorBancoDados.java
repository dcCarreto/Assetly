package com.assetly.infraestrutura.banco;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.sql.Connection;
import java.sql.SQLException;
import java.sql.Statement;
import java.time.Instant;
import java.util.List;
import java.util.Objects;
import java.util.stream.Collectors;

public final class MigradorBancoDados {

    private static final List<Migracao> MIGRACOES = List.of(
            new Migracao(1, "V001__estrutura_inicial.sql", "/banco/migracoes/V001__estrutura_inicial.sql")
    );

    private final FabricaConexaoSqlite fabricaConexao;

    public MigradorBancoDados(FabricaConexaoSqlite fabricaConexao) {
        this.fabricaConexao = Objects.requireNonNull(fabricaConexao, "fábrica de conexão é obrigatória");
    }

    public void migrar() {
        try (var conexao = fabricaConexao.abrir()) {
            criarTabelaMigracoes(conexao);
            for (var migracao : MIGRACOES) {
                if (!foiAplicada(conexao, migracao.versao())) {
                    aplicar(conexao, migracao);
                }
            }
        } catch (SQLException excecao) {
            throw new ExcecaoBancoDados("Não foi possível migrar o banco de dados", excecao);
        }
    }

    private void criarTabelaMigracoes(Connection conexao) throws SQLException {
        try (var comando = conexao.createStatement()) {
            comando.execute("""
                    CREATE TABLE IF NOT EXISTS schema_migrations (
                        versao INTEGER PRIMARY KEY,
                        nome TEXT NOT NULL,
                        aplicada_em TEXT NOT NULL
                    )
                    """);
        }
    }

    private boolean foiAplicada(Connection conexao, int versao) throws SQLException {
        try (var comando = conexao.prepareStatement("SELECT 1 FROM schema_migrations WHERE versao = ?")) {
            comando.setInt(1, versao);
            try (var resultado = comando.executeQuery()) {
                return resultado.next();
            }
        }
    }

    private void aplicar(Connection conexao, Migracao migracao) {
        try {
            var autoCommitOriginal = conexao.getAutoCommit();
            conexao.setAutoCommit(false);
            executarScript(conexao, carregarScript(migracao.recurso()));
            registrarMigracao(conexao, migracao);
            conexao.commit();
            conexao.setAutoCommit(autoCommitOriginal);
        } catch (Exception excecao) {
            tentarRollback(conexao);
            throw new ExcecaoBancoDados("Não foi possível aplicar a migração " + migracao.nome(), excecao);
        }
    }

    private String carregarScript(String recurso) throws IOException {
        try (var entrada = MigradorBancoDados.class.getResourceAsStream(recurso)) {
            if (entrada == null) {
                throw new IOException("Recurso não encontrado: " + recurso);
            }
            try (var leitor = new BufferedReader(new InputStreamReader(entrada, StandardCharsets.UTF_8))) {
                return leitor.lines()
                        .filter(linha -> !linha.stripLeading().startsWith("--"))
                        .collect(Collectors.joining(System.lineSeparator()));
            }
        }
    }

    private void executarScript(Connection conexao, String script) throws SQLException {
        try (Statement comando = conexao.createStatement()) {
            for (var instrucao : script.split(";")) {
                var sql = instrucao.trim();
                if (!sql.isEmpty()) {
                    comando.execute(sql);
                }
            }
        }
    }

    private void registrarMigracao(Connection conexao, Migracao migracao) throws SQLException {
        try (var comando = conexao.prepareStatement("""
                INSERT INTO schema_migrations (versao, nome, aplicada_em)
                VALUES (?, ?, ?)
                """)) {
            comando.setInt(1, migracao.versao());
            comando.setString(2, migracao.nome());
            comando.setString(3, Instant.now().toString());
            comando.executeUpdate();
        }
    }

    private void tentarRollback(Connection conexao) {
        try {
            conexao.rollback();
        } catch (SQLException ignored) {
            // A exceção original é mais útil para diagnóstico.
        }
    }

    private record Migracao(int versao, String nome, String recurso) {
    }
}
