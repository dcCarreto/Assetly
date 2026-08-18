using Assetly.Dominio.Bem;
using Assetly.Dominio.Manutencao;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

internal static class MapeadorManutencao
{
    internal static Manutencao Mapear(SqliteDataReader leitor) =>
        Manutencao.Restaurar(
            new ManutencaoId(Guid.Parse(leitor.GetString(leitor.GetOrdinal("id")))),
            new BemId(Guid.Parse(leitor.GetString(leitor.GetOrdinal("bem_id")))),
            ConversorSqlite.EnumDeTexto<TipoManutencao>(leitor.GetString(leitor.GetOrdinal("tipo"))),
            leitor.GetString(leitor.GetOrdinal("descricao")),
            ConversorSqlite.Data(leitor, "agendada_para")!.Value,
            ConversorSqlite.Data(leitor, "concluida_em"),
            ConversorSqlite.LerDinheiro(leitor, "custo", "moeda"),
            ConversorSqlite.EnumDeTexto<StatusManutencao>(leitor.GetString(leitor.GetOrdinal("status"))));
}
