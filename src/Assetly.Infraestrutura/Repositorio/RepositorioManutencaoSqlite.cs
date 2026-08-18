using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Manutencao;
using Assetly.Infraestrutura.Banco;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioManutencaoSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioManutencao
{
    private readonly FabricaConexaoSqlite _fabricaConexao = fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao), "fábrica de conexão é obrigatória");

    public Manutencao? BuscarPorId(ManutencaoId id)
    {
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = "SELECT * FROM manutencoes WHERE id = $id";
            comando.Parameters.AddWithValue("$id", id.Valor.ToString());
            using var resultado = comando.ExecuteReader();
            return resultado.Read() ? MapeadorManutencao.Mapear(resultado) : null;
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível buscar manutenção", excecao);
        }
    }

    public IReadOnlyList<Manutencao> Listar() =>
        Consultar("SELECT * FROM manutencoes ORDER BY agendada_para DESC, descricao", null);

    public IReadOnlyList<Manutencao> ListarPorBem(BemId bemId) =>
        Consultar("SELECT * FROM manutencoes WHERE bem_id = $bemId ORDER BY agendada_para DESC, descricao", bemId);

    public void Salvar(Manutencao manutencao)
    {
        const string sql = """
            INSERT INTO manutencoes (id, bem_id, tipo, descricao, agendada_para, concluida_em, custo, moeda, status)
            VALUES ($id, $bemId, $tipo, $descricao, $agendadaPara, $concluidaEm, $custo, $moeda, $status)
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
        try
        {
            using var conexao = _fabricaConexao.Abrir();
            using var comando = conexao.CreateCommand();
            comando.CommandText = sql;
            var custo = manutencao.Custo;
            comando.Parameters.AddWithValue("$id", manutencao.Id.Valor.ToString());
            comando.Parameters.AddWithValue("$bemId", manutencao.BemId.Valor.ToString());
            comando.Parameters.AddWithValue("$tipo", ConversorSqlite.EnumParaTexto(manutencao.Tipo));
            comando.Parameters.AddWithValue("$descricao", manutencao.Descricao);
            comando.Parameters.AddWithValue("$agendadaPara", ConversorSqlite.Texto(manutencao.AgendadaPara)!);
            comando.Parameters.AddWithValue("$concluidaEm", (object?)ConversorSqlite.Texto(manutencao.ConcluidaEm) ?? DBNull.Value);
            comando.Parameters.AddWithValue("$custo", (object?)ConversorSqlite.Valor(custo) ?? DBNull.Value);
            comando.Parameters.AddWithValue("$moeda", (object?)ConversorSqlite.Moeda(custo) ?? DBNull.Value);
            comando.Parameters.AddWithValue("$status", ConversorSqlite.EnumParaTexto(manutencao.Status));
            comando.ExecuteNonQuery();
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível salvar manutenção", excecao);
        }
    }

    private List<Manutencao> Consultar(string sql, BemId? bemId)
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
            var manutencoes = new List<Manutencao>();
            while (resultado.Read())
            {
                manutencoes.Add(MapeadorManutencao.Mapear(resultado));
            }

            return manutencoes;
        }
        catch (SqliteException excecao)
        {
            throw new ExcecaoBancoDados("Não foi possível listar manutenções", excecao);
        }
    }
}
