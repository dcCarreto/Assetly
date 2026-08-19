using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Assetly.Apresentacao.Visao;

internal sealed class EtiquetaFundoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        EtiquetaApoio.Cores((string?)value ?? string.Empty).Fundo;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

internal sealed class EtiquetaBordaConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        EtiquetaApoio.Cores((string?)value ?? string.Empty).Borda;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

internal sealed class EtiquetaTextoCorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        EtiquetaApoio.Cores((string?)value ?? string.Empty).Texto;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

internal sealed class LinhaFundoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        EtiquetaApoio.LinhaFundo((string?)value);

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
