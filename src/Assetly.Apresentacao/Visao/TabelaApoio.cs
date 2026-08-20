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

    /// <summary>Coluna de texto — recorte com reticências e ToolTip com o valor completo em qualquer largura.</summary>
    internal static DataGridTextColumn ColunaTexto(string titulo, string caminho, DataGridLength largura, double larguraMinima = 0, string? estiloBase = null)
    {
        var estilo = new Style(typeof(TextBlock));
        if (estiloBase is not null)
        {
            estilo.BasedOn = (Style)Application.Current.Resources[estiloBase];
        }

        estilo.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis));
        estilo.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, new Binding(caminho)));

        return new DataGridTextColumn
        {
            Header = titulo,
            Binding = new Binding(caminho),
            Width = largura,
            MinWidth = larguraMinima,
            ElementStyle = estilo
        };
    }

    /// <summary>Coluna de dado monoespaçado alinhado à direita — datas e valores monetários.</summary>
    internal static DataGridTemplateColumn ColunaTextoMono(string titulo, string caminho, DataGridLength largura)
    {
        var fabricaTexto = new FrameworkElementFactory(typeof(TextBlock));
        fabricaTexto.SetValue(FrameworkElement.StyleProperty, (Style)Application.Current.Resources["CelulaMonoStyle"]);
        fabricaTexto.SetBinding(TextBlock.TextProperty, new Binding(caminho));
        fabricaTexto.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);

        return new DataGridTemplateColumn
        {
            Header = titulo,
            CellTemplate = new DataTemplate { VisualTree = fabricaTexto },
            Width = largura
        };
    }

    internal static DataGridTemplateColumn ColunaEtiqueta(string titulo, string caminhoTexto, string caminhoClasse, DataGridLength largura)
    {
        var fabricaBorda = new FrameworkElementFactory(typeof(Border));
        fabricaBorda.SetValue(FrameworkElement.StyleProperty, (Style)Application.Current.Resources["BadgeStyle"]);
        fabricaBorda.SetBinding(Border.BackgroundProperty, new Binding(caminhoClasse) { Converter = new StatusToBrushConverter() });
        fabricaBorda.SetBinding(Border.BorderBrushProperty, new Binding(caminhoClasse) { Converter = new StatusToBrushConverter(), ConverterParameter = "Borda" });

        var fabricaTexto = new FrameworkElementFactory(typeof(TextBlock));
        fabricaTexto.SetValue(FrameworkElement.StyleProperty, (Style)Application.Current.Resources["BadgeTextoStyle"]);
        fabricaTexto.SetBinding(TextBlock.TextProperty, new Binding(caminhoTexto));
        fabricaTexto.SetBinding(TextBlock.ForegroundProperty, new Binding(caminhoClasse) { Converter = new StatusToBrushConverter(), ConverterParameter = "Texto" });
        fabricaBorda.AppendChild(fabricaTexto);

        return new DataGridTemplateColumn
        {
            Header = titulo,
            CellTemplate = new DataTemplate { VisualTree = fabricaBorda },
            Width = largura
        };
    }

    /// <summary>Coluna de arquivo — mostra só o nome (ver NomeArquivoConverter); o caminho completo vai para o ToolTip.</summary>
    internal static DataGridTemplateColumn ColunaArquivo(string titulo, string caminho, DataGridLength largura, Action<object> aoAbrir)
    {
        var fabricaTexto = new FrameworkElementFactory(typeof(TextBlock));
        fabricaTexto.SetValue(FrameworkElement.StyleProperty, (Style)Application.Current.Resources["CelulaDestaqueStyle"]);
        fabricaTexto.SetValue(TextBlock.FontWeightProperty, FontWeights.Normal);
        fabricaTexto.SetBinding(TextBlock.TextProperty, new Binding(caminho) { Converter = new NomeArquivoConverter() });
        fabricaTexto.SetBinding(FrameworkElement.ToolTipProperty, new Binding(caminho));
        fabricaTexto.SetValue(TextBlock.TextDecorationsProperty, TextDecorations.Underline);
        fabricaTexto.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        fabricaTexto.SetValue(TextBlock.ForegroundProperty, (Brush)Application.Current.Resources["AccentPrimary"]);
        fabricaTexto.SetValue(FrameworkElement.CursorProperty, Cursors.Hand);
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
            Width = largura
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
                var classe = classeLinha(item);
                e.Row.Background = EtiquetaApoio.LinhaFundo(classe);
                e.Row.BorderBrush = EtiquetaApoio.LinhaBorda(classe);
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
