using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Assetly.Apresentacao.Visao;

/// <summary>
/// Converte a classe de status/severidade de uma linha (ex.: "etiqueta-vencida", "linha-perigo")
/// no par fundo/borda/texto da semântica correspondente. O ConverterParameter escolhe qual parte
/// da tripla retornar ("Borda" ou "Texto"; qualquer outro valor, incluindo nulo, retorna o fundo).
/// Usado tanto pelo badge da coluna de status quanto pelo tingimento da linha da tabela, para que
/// as duas representações sempre venham do mesmo mapeamento semântico.
/// </summary>
internal sealed class ConversorStatusPincel : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var (fundo, borda, texto) = EtiquetaApoio.Cores((string?)value ?? string.Empty);
        return (parameter as string) switch
        {
            "Borda" => borda,
            "Texto" => texto,
            _ => fundo
        };
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
