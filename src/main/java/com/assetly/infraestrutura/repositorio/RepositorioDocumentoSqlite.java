package com.assetly.infraestrutura.repositorio;

import com.assetly.aplicacao.repositorio.RepositorioDocumento;
import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.documento.Documento;
import com.assetly.dominio.documento.DocumentoId;
import com.assetly.infraestrutura.banco.ExcecaoBancoDados;
import com.assetly.infraestrutura.banco.FabricaConexaoSqlite;

import java.sql.SQLException;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;
import java.util.Optional;

public final class RepositorioDocumentoSqlite implements RepositorioDocumento {

    private final FabricaConexaoSqlite fabricaConexao;

    public RepositorioDocumentoSqlite(FabricaConexaoSqlite fabricaConexao) {
        this.fabricaConexao = Objects.requireNonNull(fabricaConexao, "fábrica de conexão é obrigatória");
    }

    @Override
    public Optional<Documento> buscarPorId(DocumentoId id) {
        try (var conexao = fabricaConexao.abrir();
             var comando = conexao.prepareStatement("SELECT * FROM documentos WHERE id = ?")) {
            comando.setString(1, id.valor().toString());
            try (var resultado = comando.executeQuery()) {
                return resultado.next() ? Optional.of(MapeadorDocumento.mapear(resultado)) : Optional.empty();
            }
        } catch (SQLException excecao) {
            throw new ExcecaoBancoDados("Não foi possível buscar documento", excecao);
        }
    }

    @Override
    public List<Documento> listar() {
        return consultar("SELECT * FROM documentos ORDER BY registrado_em DESC, nome", null);
    }

    @Override
    public List<Documento> listarPorBem(BemId bemId) {
        return consultar("SELECT * FROM documentos WHERE bem_id = ? ORDER BY registrado_em DESC, nome", bemId);
    }

    @Override
    public void salvar(Documento documento) {
        var sql = """
                INSERT INTO documentos (id, bem_id, tipo, nome, caminho_local, registrado_em, status)
                VALUES (?, ?, ?, ?, ?, ?, ?)
                ON CONFLICT(id) DO UPDATE SET
                    bem_id = excluded.bem_id,
                    tipo = excluded.tipo,
                    nome = excluded.nome,
                    caminho_local = excluded.caminho_local,
                    registrado_em = excluded.registrado_em,
                    status = excluded.status
                """;
        try (var conexao = fabricaConexao.abrir();
             var comando = conexao.prepareStatement(sql)) {
            comando.setString(1, documento.id().valor().toString());
            comando.setString(2, documento.bemId().valor().toString());
            comando.setString(3, documento.tipo().name());
            comando.setString(4, documento.nome().valor());
            comando.setString(5, documento.caminhoLocal().valor().toString());
            comando.setString(6, ConversorSqlite.texto(documento.registradoEm()));
            comando.setString(7, documento.status().name());
            comando.executeUpdate();
        } catch (SQLException excecao) {
            throw new ExcecaoBancoDados("Não foi possível salvar documento", excecao);
        }
    }

    private List<Documento> consultar(String sql, BemId bemId) {
        try (var conexao = fabricaConexao.abrir();
             var comando = conexao.prepareStatement(sql)) {
            if (bemId != null) {
                comando.setString(1, bemId.valor().toString());
            }
            try (var resultado = comando.executeQuery()) {
                var documentos = new ArrayList<Documento>();
                while (resultado.next()) {
                    documentos.add(MapeadorDocumento.mapear(resultado));
                }
                return documentos;
            }
        } catch (SQLException excecao) {
            throw new ExcecaoBancoDados("Não foi possível listar documentos", excecao);
        }
    }
}
