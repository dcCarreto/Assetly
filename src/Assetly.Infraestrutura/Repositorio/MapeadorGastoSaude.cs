using Assetly.Dominio.Bem;
using Assetly.Dominio.Saude;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

internal static class MapeadorGastoSaude
{
    internal static GastoSaude Mapear(SqliteDataReader leitor) =>
        GastoSaude.Restaurar(
            new GastoSaudeId(Guid.Parse(leitor.GetString(leitor.GetOrdinal("id")))),
            new BemId(Guid.Parse(leitor.GetString(leitor.GetOrdinal("bem_id")))),
            ConversorSqlite.EnumDeTexto<TipoGastoSaude>(leitor.GetString(leitor.GetOrdinal("tipo"))),
            leitor.GetString(leitor.GetOrdinal("descricao")),
            ConversorSqlite.Data(leitor, "data")!.Value,
            ConversorSqlite.LerDinheiro(leitor, "valor", "moeda")!);
}
