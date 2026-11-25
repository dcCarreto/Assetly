using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

internal static class MapeadorDocumento
{
    internal static Documento Mapear(SqliteDataReader leitor) =>
        Documento.Restaurar(
            new DocumentoId(Guid.Parse(leitor.GetString(leitor.GetOrdinal("id")))),
            new BemId(Guid.Parse(leitor.GetString(leitor.GetOrdinal("bem_id")))),
            ConversorSqlite.EnumDeTexto<TipoDocumento>(leitor.GetString(leitor.GetOrdinal("tipo"))),
            new NomeDocumento(leitor.GetString(leitor.GetOrdinal("nome"))),
            CaminhoDocumentoLocal.De(leitor.GetString(leitor.GetOrdinal("caminho_local"))),
            ConversorSqlite.Data(leitor, "registrado_em")!.Value,
            ConversorSqlite.EnumDeTexto<StatusDocumento>(leitor.GetString(leitor.GetOrdinal("status"))),
            ConversorSqlite.TextoColuna(leitor, "chave_acesso"),
            ConversorSqlite.TextoColuna(leitor, "url_consulta"),
            ConversorSqlite.TextoColuna(leitor, "vinculo_tipo") is { Length: > 0 } tipoVinculo
                ? new VinculoDocumento(ConversorSqlite.EnumDeTexto<TipoVinculoDocumento>(tipoVinculo), Guid.Parse(leitor.GetString(leitor.GetOrdinal("vinculo_id"))))
                : null);
}
