package com.assetly.infraestrutura.banco;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.SQLException;
import java.sql.Statement;
import java.util.Objects;

public final class FabricaConexaoSqlite {

    private final Path arquivoBanco;

    public FabricaConexaoSqlite(Path arquivoBanco) {
        this.arquivoBanco = Objects.requireNonNull(arquivoBanco, "arquivo do banco é obrigatório").toAbsolutePath();
    }

    public Connection abrir() {
        try {
            var diretorio = arquivoBanco.getParent();
            if (diretorio != null) {
                Files.createDirectories(diretorio);
            }
            Class.forName("org.sqlite.JDBC");
            var conexao = DriverManager.getConnection("jdbc:sqlite:" + arquivoBanco);
            configurar(conexao);
            return conexao;
        } catch (ClassNotFoundException | IOException | SQLException excecao) {
            throw new ExcecaoBancoDados("Não foi possível abrir o banco SQLite", excecao);
        }
    }

    public Path arquivoBanco() {
        return arquivoBanco;
    }

    private void configurar(Connection conexao) throws SQLException {
        try (Statement comando = conexao.createStatement()) {
            comando.execute("PRAGMA foreign_keys = ON");
            comando.execute("PRAGMA busy_timeout = 5000");
        }
    }
}
