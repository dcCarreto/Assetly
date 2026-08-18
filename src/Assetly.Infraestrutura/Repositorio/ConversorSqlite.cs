using System.Globalization;
using System.Text.RegularExpressions;
using Assetly.Dominio.Compartilhado;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Repositorio;

internal static partial class ConversorSqlite
{
    internal static DateOnly? Data(SqliteDataReader leitor, string coluna)
    {
        var indice = leitor.GetOrdinal(coluna);
        return leitor.IsDBNull(indice) ? null : DateOnly.Parse(leitor.GetString(indice), CultureInfo.InvariantCulture);
    }

    internal static string? Texto(DateOnly? data) => data?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    internal static Dinheiro? LerDinheiro(SqliteDataReader leitor, string colunaValor, string colunaMoeda)
    {
        var indiceValor = leitor.GetOrdinal(colunaValor);
        if (leitor.IsDBNull(indiceValor))
        {
            return null;
        }

        var valor = decimal.Parse(leitor.GetString(indiceValor), CultureInfo.InvariantCulture);
        var moeda = leitor.GetString(leitor.GetOrdinal(colunaMoeda));
        return Dinheiro.De(valor, moeda);
    }

    internal static string? Valor(Dinheiro? dinheiro) => dinheiro?.Valor.ToString(CultureInfo.InvariantCulture);

    internal static string? Moeda(Dinheiro? dinheiro) => dinheiro?.CodigoMoeda;

    internal static string EnumParaTexto<TEnum>(TEnum valor) where TEnum : struct, Enum =>
        RegexSeparadorMaiuscula().Replace(valor.ToString(), "_$1").ToUpperInvariant();

    internal static TEnum EnumDeTexto<TEnum>(string texto) where TEnum : struct, Enum
    {
        var pascal = string.Concat(texto.Split('_').Select(
            parte => parte.Length == 0 ? parte : char.ToUpperInvariant(parte[0]) + parte[1..].ToLowerInvariant()));
        return Enum.Parse<TEnum>(pascal);
    }

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex RegexSeparadorMaiuscula();
}
