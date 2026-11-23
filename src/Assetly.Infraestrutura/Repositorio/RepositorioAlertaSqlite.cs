using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Infraestrutura.Banco;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioAlertaSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioAlerta
{
    private readonly FabricaConexaoSqlite _fabricaConexao = fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao), "fábrica de conexão é obrigatória");

    public Alerta? BuscarPorId(AlertaId id)
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = "SELECT * FROM alertas WHERE id = $id";
            comando.Parameters.AddWithValue("$id", id.Valor.ToString());
            using var resultado = comando.ExecuteReader();
            return resultado.Read() ? MapeadorAlerta.Mapear(resultado) : null;
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível buscar alerta", excecao);
        }
    }

    public IReadOnlyList<Alerta> Listar() =>
        Consultar("SELECT * FROM alertas ORDER BY criado_em DESC, mensagem", null);

    public IReadOnlyList<Alerta> ListarPorBem(BemId bemId) =>
        Consultar("SELECT * FROM alertas WHERE bem_id = $bemId ORDER BY criado_em DESC, mensagem", bemId);

    public void Salvar(Alerta alerta)
    {
        const string sql = """
            INSERT INTO alertas (id, bem_id, tipo, severidade, mensagem, criado_em, prazo_em, status, automatico)
            VALUES ($id, $bemId, $tipo, $severidade, $mensagem, $criadoEm, $prazoEm, $status, $automatico)
            ON CONFLICT(id) DO UPDATE SET
                bem_id = excluded.bem_id,
                tipo = excluded.tipo,
                severidade = excluded.severidade,
                mensagem = excluded.mensagem,
                criado_em = excluded.criado_em,
                prazo_em = excluded.prazo_em,
                status = excluded.status,
                automatico = excluded.automatico
            """;
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = sql;
            comando.Parameters.AddWithValue("$id", alerta.Id.Valor.ToString());
            comando.Parameters.AddWithValue("$bemId", alerta.BemId.Valor.ToString());
            comando.Parameters.AddWithValue("$tipo", ConversorSqlite.EnumParaTexto(alerta.Tipo));
            comando.Parameters.AddWithValue("$severidade", ConversorSqlite.EnumParaTexto(alerta.Severidade));
            comando.Parameters.AddWithValue("$mensagem", alerta.Mensagem);
            comando.Parameters.AddWithValue("$criadoEm", ConversorSqlite.Texto(alerta.CriadoEm)!);
            comando.Parameters.AddWithValue("$prazoEm", (object?)ConversorSqlite.Texto(alerta.PrazoEm) ?? DBNull.Value);
            comando.Parameters.AddWithValue("$status", ConversorSqlite.EnumParaTexto(alerta.Status));
            comando.Parameters.AddWithValue("$automatico", alerta.Automatico ? 1 : 0);
            comando.ExecuteNonQuery();
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível salvar alerta", excecao);
        }
    }

    private List<Alerta> Consultar(string sql, BemId? bemId)
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
            var alertas = new List<Alerta>();
            while (resultado.Read())
            {
                alertas.Add(MapeadorAlerta.Mapear(resultado));
            }

            return alertas;
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível listar alertas", excecao);
        }
    }

    public void Excluir(AlertaId id)
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = "DELETE FROM alertas WHERE id = $id";
            comando.Parameters.AddWithValue("$id", id.Valor.ToString());
            comando.ExecuteNonQuery();
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível excluir alerta", excecao);
        }
    }
}
