using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;
using Assetly.Infraestrutura.Banco;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioDocumentoSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioDocumento
{
    private readonly FabricaConexaoSqlite _fabricaConexao = fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao), "fábrica de conexão é obrigatória");

    public Documento? BuscarPorId(DocumentoId id)
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = "SELECT * FROM documentos WHERE id = $id";
            comando.Parameters.AddWithValue("$id", id.Valor.ToString());
            using var resultado = comando.ExecuteReader();
            return resultado.Read() ? MapeadorDocumento.Mapear(resultado) : null;
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível buscar documento", excecao);
        }
    }

    public IReadOnlyList<Documento> Listar() =>
        Consultar("SELECT * FROM documentos ORDER BY registrado_em DESC, nome", null);

    public IReadOnlyList<Documento> ListarPorBem(BemId bemId) =>
        Consultar("SELECT * FROM documentos WHERE bem_id = $bemId ORDER BY registrado_em DESC, nome", bemId);

    public void Salvar(Documento documento)
    {
        const string sql = """
            INSERT INTO documentos (id, bem_id, tipo, nome, caminho_local, registrado_em, status, chave_acesso, url_consulta)
            VALUES ($id, $bemId, $tipo, $nome, $caminhoLocal, $registradoEm, $status, $chaveAcesso, $urlConsulta)
            ON CONFLICT(id) DO UPDATE SET
                bem_id = excluded.bem_id,
                tipo = excluded.tipo,
                nome = excluded.nome,
                caminho_local = excluded.caminho_local,
                registrado_em = excluded.registrado_em,
                status = excluded.status,
                chave_acesso = excluded.chave_acesso,
                url_consulta = excluded.url_consulta
            """;
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = sql;
            comando.Parameters.AddWithValue("$id", documento.Id.Valor.ToString());
            comando.Parameters.AddWithValue("$bemId", documento.BemId.Valor.ToString());
            comando.Parameters.AddWithValue("$tipo", ConversorSqlite.EnumParaTexto(documento.Tipo));
            comando.Parameters.AddWithValue("$nome", documento.Nome.Valor);
            comando.Parameters.AddWithValue("$caminhoLocal", documento.CaminhoLocal.Valor);
            comando.Parameters.AddWithValue("$registradoEm", ConversorSqlite.Texto(documento.RegistradoEm)!);
            comando.Parameters.AddWithValue("$status", ConversorSqlite.EnumParaTexto(documento.Status));
            comando.Parameters.AddWithValue("$chaveAcesso", documento.ChaveAcesso);
            comando.Parameters.AddWithValue("$urlConsulta", documento.UrlConsulta);
            comando.ExecuteNonQuery();
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível salvar documento", excecao);
        }
    }

    private List<Documento> Consultar(string sql, BemId? bemId)
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = sql;
            if (bemId is not null)
            {
                comando.Parameters.AddWithValue("$bemId", bemId.Value.Valor.ToString());
            }

            using var resultado = comando.ExecuteReader();
            var documentos = new List<Documento>();
            while (resultado.Read())
            {
                documentos.Add(MapeadorDocumento.Mapear(resultado));
            }

            return documentos;
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível listar documentos", excecao);
        }
    }

    public void Excluir(DocumentoId id)
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = "DELETE FROM documentos WHERE id = $id";
            comando.Parameters.AddWithValue("$id", id.Valor.ToString());
            comando.ExecuteNonQuery();
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível excluir documento", excecao);
        }
    }
}
