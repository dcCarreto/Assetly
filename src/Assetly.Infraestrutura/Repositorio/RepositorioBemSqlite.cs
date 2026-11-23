using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Infraestrutura.Banco;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioBemSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioBem
{
    private readonly FabricaConexaoSqlite _fabricaConexao = fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao), "fábrica de conexão é obrigatória");

    public Bem? BuscarPorId(BemId id)
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = "SELECT * FROM bens WHERE id = $id";
            comando.Parameters.AddWithValue("$id", id.Valor.ToString());
            using var resultado = comando.ExecuteReader();
            return resultado.Read() ? MapeadorBem.Mapear(resultado) : null;
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível buscar bem", excecao);
        }
    }

    public IReadOnlyList<Bem> Listar()
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = "SELECT * FROM bens ORDER BY nome";
            using var resultado = comando.ExecuteReader();
            var bens = new List<Bem>();
            while (resultado.Read())
            {
                bens.Add(MapeadorBem.Mapear(resultado));
            }

            return bens;
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível listar bens", excecao);
        }
    }

    public void Salvar(Bem bem)
    {
        const string sql = """
            INSERT INTO bens (id, nome, tipo, status, comprado_em, preco_compra, moeda, observacoes, responsavel_id)
            VALUES ($id, $nome, $tipo, $status, $compradoEm, $precoCompra, $moeda, $observacoes, $responsavelId)
            ON CONFLICT(id) DO UPDATE SET
                nome = excluded.nome,
                tipo = excluded.tipo,
                status = excluded.status,
                comprado_em = excluded.comprado_em,
                preco_compra = excluded.preco_compra,
                moeda = excluded.moeda,
                observacoes = excluded.observacoes,
                responsavel_id = excluded.responsavel_id
            """;
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = sql;
            var preco = bem.PrecoCompra;
            comando.Parameters.AddWithValue("$id", bem.Id.Valor.ToString());
            comando.Parameters.AddWithValue("$nome", bem.Nome.Valor);
            comando.Parameters.AddWithValue("$tipo", ConversorSqlite.EnumParaTexto(bem.Tipo));
            comando.Parameters.AddWithValue("$status", ConversorSqlite.EnumParaTexto(bem.Status));
            comando.Parameters.AddWithValue("$compradoEm", (object?)ConversorSqlite.Texto(bem.CompradoEm) ?? DBNull.Value);
            comando.Parameters.AddWithValue("$precoCompra", (object?)ConversorSqlite.Valor(preco) ?? DBNull.Value);
            comando.Parameters.AddWithValue("$moeda", (object?)ConversorSqlite.Moeda(preco) ?? DBNull.Value);
            comando.Parameters.AddWithValue("$observacoes", bem.Observacoes);
            comando.Parameters.AddWithValue("$responsavelId", ConversorSqlite.Nulo(bem.ResponsavelId?.Valor.ToString()));
            comando.ExecuteNonQuery();
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível salvar bem", excecao);
        }
    }

    public void Excluir(BemId id)
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = "DELETE FROM bens WHERE id = $id";
            comando.Parameters.AddWithValue("$id", id.Valor.ToString());
            comando.ExecuteNonQuery();
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível excluir bem", excecao);
        }
    }
}
