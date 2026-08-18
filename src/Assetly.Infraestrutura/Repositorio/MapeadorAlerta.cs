using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

internal static class MapeadorAlerta
{
    internal static Alerta Mapear(SqliteDataReader leitor) =>
        Alerta.Restaurar(
            new AlertaId(Guid.Parse(leitor.GetString(leitor.GetOrdinal("id")))),
            new BemId(Guid.Parse(leitor.GetString(leitor.GetOrdinal("bem_id")))),
            ConversorSqlite.EnumDeTexto<TipoAlerta>(leitor.GetString(leitor.GetOrdinal("tipo"))),
            ConversorSqlite.EnumDeTexto<SeveridadeAlerta>(leitor.GetString(leitor.GetOrdinal("severidade"))),
            leitor.GetString(leitor.GetOrdinal("mensagem")),
            ConversorSqlite.Data(leitor, "criado_em")!.Value,
            ConversorSqlite.Data(leitor, "prazo_em"),
            ConversorSqlite.EnumDeTexto<StatusAlerta>(leitor.GetString(leitor.GetOrdinal("status"))));
}
