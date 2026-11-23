using System.Globalization;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Veiculo;
using Assetly.Infraestrutura.Banco;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioAbastecimentoSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioAbastecimento
{
    private const string Ordem = "ORDER BY data DESC, quilometragem DESC";
    private readonly ExecutorSqlite _executor = new(fabricaConexao);

    public Abastecimento? BuscarPorId(AbastecimentoId id) =>
        _executor.Consultar("SELECT * FROM abastecimentos WHERE id = $id", Mapear, "Não foi possível buscar abastecimento",
            ("$id", id.Valor.ToString())).FirstOrDefault();

    public IReadOnlyList<Abastecimento> Listar() =>
        _executor.Consultar($"SELECT * FROM abastecimentos {Ordem}", Mapear, "Não foi possível listar abastecimentos");

    public IReadOnlyList<Abastecimento> ListarPorBem(BemId bemId) =>
        _executor.Consultar($"SELECT * FROM abastecimentos WHERE bem_id = $bemId {Ordem}", Mapear, "Não foi possível listar abastecimentos",
            ("$bemId", bemId.Valor.ToString()));

    public void Salvar(Abastecimento abastecimento)
    {
        const string sql = """
            INSERT INTO abastecimentos (id, bem_id, data, quilometragem, litros, valor, moeda, combustivel, tanque_cheio, posto)
            VALUES ($id, $bemId, $data, $quilometragem, $litros, $valor, $moeda, $combustivel, $tanqueCheio, $posto)
            ON CONFLICT(id) DO UPDATE SET
                data = excluded.data, quilometragem = excluded.quilometragem, litros = excluded.litros, valor = excluded.valor,
                moeda = excluded.moeda, combustivel = excluded.combustivel, tanque_cheio = excluded.tanque_cheio, posto = excluded.posto
            """;
        _executor.Executar(sql, "Não foi possível salvar abastecimento",
            ("$id", abastecimento.Id.Valor.ToString()),
            ("$bemId", abastecimento.BemId.Valor.ToString()),
            ("$data", ConversorSqlite.Texto(abastecimento.Data)!),
            ("$quilometragem", abastecimento.Quilometragem),
            ("$litros", abastecimento.Litros.ToString(CultureInfo.InvariantCulture)),
            ("$valor", ConversorSqlite.Valor(abastecimento.Valor)!),
            ("$moeda", ConversorSqlite.Moeda(abastecimento.Valor)!),
            ("$combustivel", ConversorSqlite.EnumParaTexto(abastecimento.Combustivel)),
            ("$tanqueCheio", abastecimento.TanqueCheio ? 1 : 0),
            ("$posto", abastecimento.Posto));
    }

    public void Excluir(AbastecimentoId id) =>
        _executor.Executar("DELETE FROM abastecimentos WHERE id = $id", "Não foi possível excluir abastecimento", ("$id", id.Valor.ToString()));

    private static Abastecimento Mapear(SqliteDataReader leitor) =>
        Abastecimento.Restaurar(
            new AbastecimentoId(Guid.Parse(leitor.GetString(leitor.GetOrdinal("id")))),
            new BemId(Guid.Parse(leitor.GetString(leitor.GetOrdinal("bem_id")))),
            ConversorSqlite.Data(leitor, "data")!.Value,
            ConversorSqlite.InteiroOpcional(leitor, "quilometragem") ?? 0,
            ConversorSqlite.DecimalOpcional(leitor, "litros") ?? 0m,
            ConversorSqlite.LerDinheiro(leitor, "valor", "moeda")!,
            ConversorSqlite.EnumDeTexto<TipoCombustivel>(leitor.GetString(leitor.GetOrdinal("combustivel"))),
            leitor.GetInt64(leitor.GetOrdinal("tanque_cheio")) != 0,
            ConversorSqlite.TextoColuna(leitor, "posto"));
}
