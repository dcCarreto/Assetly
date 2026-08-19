using System.Globalization;
using System.Text.RegularExpressions;
using Assetly.Dominio.Compartilhado;

namespace Assetly.Apresentacao.Visao;

internal static partial class Formatador
{
    internal static string Data(DateOnly? data) => data is null ? string.Empty : data.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    internal static string Dinheiro(Dinheiro? dinheiro) =>
        dinheiro is null ? string.Empty : $"{dinheiro.CodigoMoeda} {dinheiro.Valor.ToString(CultureInfo.InvariantCulture)}";

    internal static string TextoEnum<TEnum>(TEnum valor) where TEnum : struct, Enum =>
        RegexSeparadorMaiuscula().Replace(valor.ToString(), " $1").ToLowerInvariant();

    internal static string ClasseEtiqueta<TEnum>(TEnum valor) where TEnum : struct, Enum =>
        "etiqueta-" + RegexSeparadorMaiuscula().Replace(valor.ToString(), "-$1").ToLowerInvariant();

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex RegexSeparadorMaiuscula();
}
