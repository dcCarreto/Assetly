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
            leitor.GetString(leitor.GetOrdinal("observacoes")),
            ConversorSqlite.TextoColuna(leitor, "responsavel_id") is { Length: > 0 } responsavel ? new BemId(Guid.Parse(responsavel)) : null,
            new InventarioBem(
                ConversorSqlite.TextoColuna(leitor, "marca"),
                ConversorSqlite.TextoColuna(leitor, "modelo"),
                ConversorSqlite.TextoColuna(leitor, "numero_serie"),
                ConversorSqlite.TextoColuna(leitor, "loja"),
                ConversorSqlite.TextoColuna(leitor, "localizacao")),
            ConversorSqlite.LerDinheiro(leitor, "valor_informado", "moeda_valor_informado"),
            ConversorSqlite.Data(leitor, "valor_informado_em"),
            ConversorSqlite.DecimalOpcional(leitor, "taxa_depreciacao"));
}
