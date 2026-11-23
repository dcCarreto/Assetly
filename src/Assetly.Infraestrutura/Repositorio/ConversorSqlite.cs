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

    internal static DateTimeOffset? DataHora(SqliteDataReader leitor, string coluna)
    {
        var indice = leitor.GetOrdinal(coluna);
        return leitor.IsDBNull(indice) ? null : DateTimeOffset.Parse(leitor.GetString(indice), CultureInfo.InvariantCulture);
    }

    internal static string? Texto(DateTimeOffset? dataHora) => dataHora?.ToString("O", CultureInfo.InvariantCulture);

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

    internal static int? InteiroOpcional(SqliteDataReader leitor, string coluna)
    {
        var indice = leitor.GetOrdinal(coluna);
        return leitor.IsDBNull(indice) ? null : (int)leitor.GetInt64(indice);
    }

    internal static decimal? DecimalOpcional(SqliteDataReader leitor, string coluna)
    {
        var indice = leitor.GetOrdinal(coluna);
        return leitor.IsDBNull(indice) ? null : decimal.Parse(leitor.GetString(indice), CultureInfo.InvariantCulture);
    }

    internal static string TextoColuna(SqliteDataReader leitor, string coluna)
    {
        var indice = leitor.GetOrdinal(coluna);
        return leitor.IsDBNull(indice) ? string.Empty : leitor.GetString(indice);
    }

    internal static string? Texto(decimal? valor) => valor?.ToString(CultureInfo.InvariantCulture);

    /// <summary>Converte nulo em <see cref="DBNull"/> para parâmetros SQLite.</summary>
    internal static object Nulo(object? valor) => valor ?? DBNull.Value;

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

    internal static string ListaParaTexto(IEnumerable<string> valores) => string.Join('|', valores);

    internal static IReadOnlyList<string> ListaDeTexto(string texto) =>
        texto.Split('|', StringSplitOptions.RemoveEmptyEntries);

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex RegexSeparadorMaiuscula();
}
