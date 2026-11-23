using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Garantia;
using Assetly.Infraestrutura.Banco;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioGarantiaSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioGarantia
{
    private readonly FabricaConexaoSqlite _fabricaConexao = fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao), "fábrica de conexão é obrigatória");

    public Garantia? BuscarPorId(GarantiaId id)
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = "SELECT * FROM garantias WHERE id = $id";
            comando.Parameters.AddWithValue("$id", id.Valor.ToString());
            using var resultado = comando.ExecuteReader();
            return resultado.Read() ? MapeadorGarantia.Mapear(resultado) : null;
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível buscar garantia", excecao);
        }
    }

    public IReadOnlyList<Garantia> Listar() =>
        Consultar("SELECT * FROM garantias ORDER BY termina_em, fornecedor", null);

    public IReadOnlyList<Garantia> ListarPorBem(BemId bemId) =>
        Consultar("SELECT * FROM garantias WHERE bem_id = $bemId ORDER BY termina_em, fornecedor", bemId);

    public void Salvar(Garantia garantia)
    {
        const string sql = """
            INSERT INTO garantias (id, bem_id, tipo, fornecedor, inicia_em, termina_em, contato_suporte)
            VALUES ($id, $bemId, $tipo, $fornecedor, $iniciaEm, $terminaEm, $contatoSuporte)
            ON CONFLICT(id) DO UPDATE SET
                bem_id = excluded.bem_id,
                tipo = excluded.tipo,
                fornecedor = excluded.fornecedor,
                inicia_em = excluded.inicia_em,
                termina_em = excluded.termina_em,
                contato_suporte = excluded.contato_suporte
            """;
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = sql;
            comando.Parameters.AddWithValue("$id", garantia.Id.Valor.ToString());
            comando.Parameters.AddWithValue("$bemId", garantia.BemId.Valor.ToString());
            comando.Parameters.AddWithValue("$tipo", ConversorSqlite.EnumParaTexto(garantia.Tipo));
            comando.Parameters.AddWithValue("$fornecedor", garantia.Fornecedor);
            comando.Parameters.AddWithValue("$iniciaEm", ConversorSqlite.Texto(garantia.Periodo.IniciaEm)!);
            comando.Parameters.AddWithValue("$terminaEm", ConversorSqlite.Texto(garantia.Periodo.TerminaEm)!);
            comando.Parameters.AddWithValue("$contatoSuporte", garantia.ContatoSuporte);
            comando.ExecuteNonQuery();
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível salvar garantia", excecao);
        }
    }

    private List<Garantia> Consultar(string sql, BemId? bemId)
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
            var garantias = new List<Garantia>();
            while (resultado.Read())
            {
                garantias.Add(MapeadorGarantia.Mapear(resultado));
            }

            return garantias;
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível listar garantias", excecao);
        }
    }

    public void Excluir(GarantiaId id)
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = "DELETE FROM garantias WHERE id = $id";
            comando.Parameters.AddWithValue("$id", id.Valor.ToString());
            comando.ExecuteNonQuery();
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível excluir garantia", excecao);
        }
    }
}
