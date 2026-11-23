using Assetly.Dominio.Preferencias;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

internal static class MapeadorConfiguracaoPreferencias
{
    internal static ConfiguracaoPreferencias Mapear(SqliteDataReader leitor)
    {
        var tema = ConversorSqlite.EnumDeTexto<TemaVisual>(leitor.GetString(leitor.GetOrdinal("tema")));
        var corDestaque = ConversorSqlite.EnumDeTexto<CorDestaque>(leitor.GetString(leitor.GetOrdinal("cor_destaque")));
        var densidadeTabela = ConversorSqlite.EnumDeTexto<DensidadeTabela>(leitor.GetString(leitor.GetOrdinal("densidade_tabela")));
        var intervaloAtualizacao = ConversorSqlite.EnumDeTexto<IntervaloAtualizacao>(leitor.GetString(leitor.GetOrdinal("intervalo_atualizacao")));
        var ordemModulos = ConversorSqlite.ListaDeTexto(leitor.GetString(leitor.GetOrdinal("ordem_modulos")));
        var modulosOcultos = ConversorSqlite.ListaDeTexto(leitor.GetString(leitor.GetOrdinal("modulos_ocultos")));
        var indiceModuloInicial = leitor.GetOrdinal("modulo_inicial");
        var moduloInicial = leitor.IsDBNull(indiceModuloInicial) ? null : leitor.GetString(indiceModuloInicial);
        var atualizarAutomaticamente = leitor.GetInt64(leitor.GetOrdinal("atualizar_automaticamente")) != 0;
        return ConfiguracaoPreferencias.Restaurar(tema, corDestaque, densidadeTabela, intervaloAtualizacao, ordemModulos, modulosOcultos, moduloInicial,
            atualizarAutomaticamente);
    }
}
