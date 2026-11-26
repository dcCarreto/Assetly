using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace Assetly.Apresentacao.Visao;

/// <summary>Converte o texto para maiúsculas — TextBlock não expõe CharacterCasing como TextBox.</summary>
internal sealed class MaiusculaConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value as string)?.ToUpper(culture) ?? string.Empty;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Extrai só o nome do arquivo de um caminho completo — usado na coluna "Arquivo local",
/// que mostra o nome e deixa o caminho completo só no ToolTip (caminhos longos ficavam ilegíveis truncados no meio).</summary>
internal sealed class NomeArquivoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string caminho ? Path.GetFileName(caminho) : string.Empty;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Esconde o elemento quando o texto ligado é vazio (selo de status só nas exceções).</summary>
internal sealed class VisivelSeTemTextoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrEmpty(value as string) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
