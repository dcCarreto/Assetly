using Assetly.Dominio.Bem;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

internal static class MapeadorBem
{
    internal static Bem Mapear(SqliteDataReader leitor) =>
        Bem.Restaurar(
            new BemId(Guid.Parse(leitor.GetString(leitor.GetOrdinal("id")))),
            new NomeBem(leitor.GetString(leitor.GetOrdinal("nome"))),
            ConversorSqlite.EnumDeTexto<TipoBem>(leitor.GetString(leitor.GetOrdinal("tipo"))),
            ConversorSqlite.EnumDeTexto<StatusBem>(leitor.GetString(leitor.GetOrdinal("status"))),
            ConversorSqlite.Data(leitor, "comprado_em"),
            ConversorSqlite.LerDinheiro(leitor, "preco_compra", "moeda"),
            leitor.GetString(leitor.GetOrdinal("observacoes")));
}
