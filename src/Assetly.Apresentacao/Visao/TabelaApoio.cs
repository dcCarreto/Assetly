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

    /// <summary>
    /// Troca as linhas da tabela preservando o que o usuário fez nela: seleção, ordenação por coluna e
    /// posição de rolagem. Como a tela recarrega a cada ciclo de atualização, sem isso a ordenação se
    /// perdia e a lista voltava ao topo a cada poucos segundos. Quando nada mudou, não toca na tabela.
    /// </summary>
    internal static void RecarregarTabela<T>(DataGrid grade, IReadOnlyList<T> itens, Func<T, object> chave) where T : class
    {
        if (grade.ItemsSource is IReadOnlyList<T> atuais && MesmoConteudo(atuais, itens))
        {
            return;
        }

        var selecionado = grade.SelectedItem as T;
        var chaveSelecionada = selecionado is null ? null : chave(selecionado);
        var ordenacoes = grade.Items.SortDescriptions.ToList();
        var direcoes = grade.Columns.Select(coluna => coluna.SortDirection).ToList();
        var rolagem = EncontrarDescendente<ScrollViewer>(grade);
        var deslocamento = rolagem?.VerticalOffset ?? 0;

        grade.ItemsSource = itens;

        foreach (var ordenacao in ordenacoes)
        {
            grade.Items.SortDescriptions.Add(ordenacao);
        }

        for (var i = 0; i < direcoes.Count && i < grade.Columns.Count; i++)
        {
            grade.Columns[i].SortDirection = direcoes[i];
        }

        if (chaveSelecionada is not null)
        {
            var encontrado = itens.FirstOrDefault(item => Equals(chave(item), chaveSelecionada));
            if (encontrado is not null)
            {
                grade.SelectedItem = encontrado;
            }
        }

        if (rolagem is not null && deslocamento > 0)
        {
            grade.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () => rolagem.ScrollToVerticalOffset(deslocamento));
        }
    }

    /// <summary>As linhas são records posicionais: o <c>ToString()</c> gerado lista todos os valores
    /// exibidos (a entidade de origem aparece só pelo nome do tipo), então serve de assinatura barata
    /// do conteúdo para detectar "nada mudou".</summary>
    private static bool MesmoConteudo<T>(IReadOnlyList<T> atuais, IReadOnlyList<T> novos) where T : class
    {
        if (atuais.Count != novos.Count)
        {
            return false;
        }

        for (var i = 0; i < atuais.Count; i++)
        {
            if (!string.Equals(atuais[i].ToString(), novos[i].ToString(), StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static TDescendente? EncontrarDescendente<TDescendente>(DependencyObject origem) where TDescendente : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(origem); i++)
        {
            var filho = VisualTreeHelper.GetChild(origem, i);
            if (filho is TDescendente encontrado)
            {
                return encontrado;
            }

            if (EncontrarDescendente<TDescendente>(filho) is { } profundo)
            {
                return profundo;
            }
        }

        return null;
    }

    /// <summary>Numa tabela estreita (janela pequena), esconde as colunas opcionais — a menos importante primeiro — até as
    /// restantes caberem na largura mínima delas; ao alargar, elas voltam. Sem isso a tabela espremia todas as colunas até
    /// nenhuma ser legível (a rolagem horizontal fica desligada no tema).</summary>
    internal static void ColunasOpcionais(DataGrid grade, params DataGridColumn[] menosImportantesPrimeiro)
    {
        static double Minima(DataGridColumn coluna) => coluna.Width.IsAbsolute ? coluna.Width.Value : coluna.MinWidth;

        void Ajustar()
        {
            var disponivel = grade.ActualWidth - 12;
            foreach (var coluna in menosImportantesPrimeiro)
            {
                coluna.Visibility = Visibility.Visible;
            }

            foreach (var coluna in menosImportantesPrimeiro)
            {
                if (grade.Columns.Where(c => c.Visibility == Visibility.Visible).Sum(Minima) <= disponivel)
                {
                    break;
                }

                coluna.Visibility = Visibility.Collapsed;
            }
        }

        grade.SizeChanged += (_, evento) =>
        {
            if (evento.WidthChanged)
            {
                Ajustar();
            }
        };
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
    internal static DataGridTemplateColumn ColunaTextoMono(string titulo, string caminho, DataGridLength largura, string? ordenarPor = null)
    {
        var fabricaTexto = new FrameworkElementFactory(typeof(TextBlock));
        fabricaTexto.SetValue(FrameworkElement.StyleProperty, (Style)Application.Current.Resources["CelulaMonoStyle"]);
        fabricaTexto.SetBinding(TextBlock.TextProperty, new Binding(caminho));
        fabricaTexto.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);
        fabricaTexto.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        fabricaTexto.SetBinding(FrameworkElement.ToolTipProperty, new Binding(caminho));

        return new DataGridTemplateColumn
        {
            Header = titulo,
            CellTemplate = new DataTemplate { VisualTree = fabricaTexto },
            ClipboardContentBinding = new Binding(caminho),
            SortMemberPath = ordenarPor ?? caminho,
            Width = largura
        };
    }

    internal static DataGridTemplateColumn ColunaEtiqueta(string titulo, string caminhoTexto, string caminhoClasse, DataGridLength largura, string? ordenarPor = null,
        string? dica = null)
    {
        var fabricaBorda = new FrameworkElementFactory(typeof(Border));
        fabricaBorda.SetValue(FrameworkElement.StyleProperty, (Style)Application.Current.Resources["BadgeStyle"]);
        fabricaBorda.SetBinding(Border.BackgroundProperty, new Binding(caminhoClasse) { Converter = new ConversorStatusPincel() });
        fabricaBorda.SetBinding(Border.BorderBrushProperty, new Binding(caminhoClasse) { Converter = new ConversorStatusPincel(), ConverterParameter = "Borda" });

        var fabricaTexto = new FrameworkElementFactory(typeof(TextBlock));
        fabricaTexto.SetValue(FrameworkElement.StyleProperty, (Style)Application.Current.Resources["BadgeTextoStyle"]);
        fabricaTexto.SetBinding(TextBlock.TextProperty, new Binding(caminhoTexto));
        fabricaTexto.SetBinding(TextBlock.ForegroundProperty, new Binding(caminhoClasse) { Converter = new ConversorStatusPincel(), ConverterParameter = "Texto" });
        fabricaBorda.AppendChild(fabricaTexto);
        if (dica is not null)
        {
            fabricaBorda.SetBinding(FrameworkElement.ToolTipProperty, new Binding(dica));
        }

        // Texto vazio = sem selo (ex.: documento disponível — só as exceções ganham selo).
        fabricaBorda.SetBinding(UIElement.VisibilityProperty, new Binding(caminhoTexto) { Converter = new VisivelSeTemTextoConverter() });

        return new DataGridTemplateColumn
        {
            Header = titulo,
            CellTemplate = new DataTemplate { VisualTree = fabricaBorda },
            ClipboardContentBinding = new Binding(caminhoTexto),
            SortMemberPath = ordenarPor ?? caminhoTexto,
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
            ClipboardContentBinding = new Binding(caminho),
            SortMemberPath = caminho,
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
