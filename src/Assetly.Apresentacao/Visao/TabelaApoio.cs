using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Assetly.Apresentacao.Visao;

internal static class TabelaApoio
{
    internal static void AplicarEstiloBase(DataGrid grade)
    {
        grade.Style = (Style)Application.Current.Resources["TabelaStyle"];
        grade.ColumnHeaderStyle = (Style)Application.Current.Resources["CabecalhoColunaStyle"];
        grade.CellStyle = (Style)Application.Current.Resources["CelulaTabelaStyle"];
        grade.RowStyle = (Style)Application.Current.Resources["LinhaTabelaStyle"];
    }

    internal static void RecarregarTabela<T>(DataGrid grade, IReadOnlyList<T> itens, Func<T, object> chave) where T : class
    {
        var selecionado = grade.SelectedItem as T;
        var chaveSelecionada = selecionado is null ? null : chave(selecionado);
        grade.ItemsSource = itens;
        if (chaveSelecionada is not null)
        {
            var encontrado = itens.FirstOrDefault(item => Equals(chave(item), chaveSelecionada));
            if (encontrado is not null)
            {
                grade.SelectedItem = encontrado;
            }
        }
    }

    internal static DataGridTextColumn ColunaTexto(string titulo, string caminho) => new()
    {
        Header = titulo,
        Binding = new Binding(caminho),
        Width = new DataGridLength(1, DataGridLengthUnitType.Star)
    };

    internal static DataGridTemplateColumn ColunaEtiqueta(string titulo, string caminhoTexto, string caminhoClasse)
    {
        var fabricaBorda = new FrameworkElementFactory(typeof(Border));
        fabricaBorda.SetValue(FrameworkElement.StyleProperty, (Style)Application.Current.Resources["EtiquetaStyle"]);
        fabricaBorda.SetBinding(Border.BackgroundProperty, new Binding(caminhoClasse) { Converter = new EtiquetaFundoConverter() });
        fabricaBorda.SetBinding(Border.BorderBrushProperty, new Binding(caminhoClasse) { Converter = new EtiquetaBordaConverter() });

        var fabricaTexto = new FrameworkElementFactory(typeof(TextBlock));
        fabricaTexto.SetValue(FrameworkElement.StyleProperty, (Style)Application.Current.Resources["EtiquetaTextoStyle"]);
        fabricaTexto.SetBinding(TextBlock.TextProperty, new Binding(caminhoTexto));
        fabricaTexto.SetBinding(TextBlock.ForegroundProperty, new Binding(caminhoClasse) { Converter = new EtiquetaTextoCorConverter() });
        fabricaBorda.AppendChild(fabricaTexto);

        return new DataGridTemplateColumn
        {
            Header = titulo,
            CellTemplate = new DataTemplate { VisualTree = fabricaBorda },
            Width = new DataGridLength(1, DataGridLengthUnitType.Star)
        };
    }

    internal static DataGridTemplateColumn ColunaArquivo(string titulo, string caminho, Action<object> aoAbrir)
    {
        var fabricaTexto = new FrameworkElementFactory(typeof(TextBlock));
        fabricaTexto.SetBinding(TextBlock.TextProperty, new Binding(caminho));
        fabricaTexto.SetValue(TextBlock.TextDecorationsProperty, TextDecorations.Underline);
        fabricaTexto.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0x7D, 0xD3, 0xFC)));
        fabricaTexto.SetValue(FrameworkElement.CursorProperty, Cursors.Hand);
        fabricaTexto.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        fabricaTexto.AddHandler(UIElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler((remetente, evento) =>
        {
            var elemento = (FrameworkElement)remetente;
            if (elemento.DataContext is not null)
            {
                aoAbrir(elemento.DataContext);
            }

            evento.Handled = true;
        }));

        return new DataGridTemplateColumn
        {
            Header = titulo,
            CellTemplate = new DataTemplate { VisualTree = fabricaTexto },
            Width = new DataGridLength(1, DataGridLengthUnitType.Star)
        };
    }

    internal static void ConfigurarLinhas<T>(
        DataGrid grade,
        Func<T, string> classeLinha,
        Func<T, ContextMenu>? menuContexto = null,
        Action<T>? aoDarDuploClique = null)
        where T : class
    {
        grade.LoadingRow += (_, e) =>
        {
            if (e.Row.Item is T item)
            {
                e.Row.Background = EtiquetaApoio.LinhaFundo(classeLinha(item));
            }
        };

        if (menuContexto is not null)
        {
            grade.ContextMenuOpening += (_, e) =>
            {
                if (EncontrarAncestral<DataGridRow>(e.OriginalSource as DependencyObject) is { Item: T item } linha)
                {
                    grade.SelectedItem = linha.Item;
                    grade.ContextMenu = menuContexto(item);
                }
                else
                {
                    e.Handled = true;
                }
            };
        }

        grade.MouseDoubleClick += (_, _) =>
        {
            if (aoDarDuploClique is not null && grade.SelectedItem is T item)
            {
                aoDarDuploClique(item);
            }
        };
    }

    private static T? EncontrarAncestral<T>(DependencyObject? origem) where T : DependencyObject
    {
        var atual = origem;
        while (atual is not null && atual is not T)
        {
            atual = VisualTreeHelper.GetParent(atual);
        }

        return atual as T;
    }
}
