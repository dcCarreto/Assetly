using Assetly.Dominio.Bem;
using Assetly.Dominio.Saude;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

internal static class MapeadorSaude
{
    internal static Saude Mapear(SqliteDataReader leitor) =>
        Saude.Restaurar(
            new SaudeId(Guid.Parse(leitor.GetString(leitor.GetOrdinal("id")))),
            new BemId(Guid.Parse(leitor.GetString(leitor.GetOrdinal("bem_id")))),
            ConversorSqlite.EnumDeTexto<TipoSaude>(leitor.GetString(leitor.GetOrdinal("tipo"))),
            leitor.GetString(leitor.GetOrdinal("fornecedor")),
            ConversorSqlite.Data(leitor, "inicia_em")!.Value,
            ConversorSqlite.Data(leitor, "termina_em")!.Value,
            leitor.GetString(leitor.GetOrdinal("contato")),
            ConversorSqlite.Data(leitor, "data_cancelamento"));
}
