using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Manutencao;
using Assetly.Infraestrutura.Banco;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioManutencaoSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioManutencao
{
    private const string Ordem = "ORDER BY agendada_para DESC, descricao";
    private readonly ExecutorSqlite _executor = new(fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao), "fábrica de conexão é obrigatória"));

    public Manutencao? BuscarPorId(ManutencaoId id) =>
        _executor.Consultar("SELECT * FROM manutencoes WHERE id = $id", MapeadorManutencao.Mapear, "Não foi possível buscar manutenção",
            ("$id", id.Valor.ToString())).FirstOrDefault();

    public IReadOnlyList<Manutencao> Listar() =>
        _executor.Consultar($"SELECT * FROM manutencoes {Ordem}", MapeadorManutencao.Mapear, "Não foi possível listar manutenções");

    public IReadOnlyList<Manutencao> ListarPorBem(BemId bemId) =>
        _executor.Consultar($"SELECT * FROM manutencoes WHERE bem_id = $bemId {Ordem}", MapeadorManutencao.Mapear, "Não foi possível listar manutenções",
            ("$bemId", bemId.Valor.ToString()));

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
        var custo = manutencao.Custo;
        _executor.Executar(sql, "Não foi possível salvar manutenção",
            ("$id", manutencao.Id.Valor.ToString()),
            ("$bemId", manutencao.BemId.Valor.ToString()),
            ("$tipo", ConversorSqlite.EnumParaTexto(manutencao.Tipo)),
            ("$descricao", manutencao.Descricao),
            ("$agendadaPara", ConversorSqlite.Texto(manutencao.AgendadaPara)!),
            ("$concluidaEm", ConversorSqlite.Nulo(ConversorSqlite.Texto(manutencao.ConcluidaEm))),
            ("$custo", ConversorSqlite.Nulo(ConversorSqlite.Valor(custo))),
            ("$moeda", ConversorSqlite.Nulo(ConversorSqlite.Moeda(custo))),
            ("$status", ConversorSqlite.EnumParaTexto(manutencao.Status)));
    }

    public void Excluir(ManutencaoId id) =>
        _executor.Executar("DELETE FROM manutencoes WHERE id = $id", "Não foi possível excluir manutenção", ("$id", id.Valor.ToString()));
}
