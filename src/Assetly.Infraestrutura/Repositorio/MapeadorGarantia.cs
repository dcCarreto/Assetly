using Assetly.Dominio.Bem;
using Assetly.Dominio.Garantia;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

internal static class MapeadorGarantia
{
    internal static Garantia Mapear(SqliteDataReader leitor) =>
        Garantia.Restaurar(
            new GarantiaId(Guid.Parse(leitor.GetString(leitor.GetOrdinal("id")))),
            new BemId(Guid.Parse(leitor.GetString(leitor.GetOrdinal("bem_id")))),
            ConversorSqlite.EnumDeTexto<TipoGarantia>(leitor.GetString(leitor.GetOrdinal("tipo"))),
            leitor.GetString(leitor.GetOrdinal("fornecedor")),
            ConversorSqlite.Data(leitor, "inicia_em")!.Value,
            ConversorSqlite.Data(leitor, "termina_em")!.Value,
            leitor.GetString(leitor.GetOrdinal("contato_suporte")));
}
