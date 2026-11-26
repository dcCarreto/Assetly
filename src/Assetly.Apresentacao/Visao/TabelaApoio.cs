using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Assetly.Apresentacao.Visao;

internal static class TabelaApoio
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<DataGrid, TextBlock> AvisosVazio = new();
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<DataGridColumn, object> OcultasPeloUsuario = new();

    /// <summary>Botão direito no cabeçalho: mostrar ou esconder colunas; arrastar a borda do cabeçalho muda a largura. As duas
    /// escolhas ficam guardadas neste computador (<see cref="PreferenciasTabelas"/>) e "Restaurar colunas" volta ao padrão.</summary>
    internal static void ColunasConfiguraveis(DataGrid tabela, string chave)
    {
        var colunas = tabela.Columns.Where(c => c.Header is string).ToList();
        var larguraPadrao = colunas.ToDictionary(c => c, c => c.Width);

        foreach (var coluna in colunas.Skip(1))
        {
            if (PreferenciasTabelas.Obter(chave, (string)coluna.Header) is { } salva)
            {
                if (!salva.Visivel)
                {
                    coluna.Visibility = Visibility.Collapsed;
                    OcultasPeloUsuario.AddOrUpdate(coluna, true);
                }

                if (salva.Largura is { } largura)
                {
                    coluna.Width = new DataGridLength(largura);
                }
            }
        }

        if (colunas.FirstOrDefault() is { } primeira && PreferenciasTabelas.Obter(chave, (string)primeira.Header)?.Largura is { } larguraPrimeira)
        {
            primeira.Width = new DataGridLength(larguraPrimeira);
        }

        var menu = new ContextMenu();
        void Montar()
        {
            menu.Items.Clear();
            menu.Items.Add(new MenuItem { Header = "Colunas visíveis", IsEnabled = false });
            foreach (var coluna in colunas.Skip(1))
            {
                var item = new MenuItem
                {
                    Header = ((string)coluna.Header).Length > 0 ? coluna.Header : "(sem título)",
                    IsCheckable = true,
                    StaysOpenOnClick = true,
                    IsChecked = !OcultasPeloUsuario.TryGetValue(coluna, out _)
                };
                item.Click += (_, _) =>
                {
                    var visivel = item.IsChecked;
                    coluna.Visibility = visivel ? Visibility.Visible : Visibility.Collapsed;
                    if (visivel)
                    {
                        OcultasPeloUsuario.Remove(coluna);
                    }
                    else
                    {
                        OcultasPeloUsuario.AddOrUpdate(coluna, true);
                    }

                    PreferenciasTabelas.Definir(chave, (string)coluna.Header, salva => salva with { Visivel = visivel });
                };
                menu.Items.Add(item);
            }

            menu.Items.Add(new Separator());
            var restaurar = new MenuItem { Header = "Restaurar colunas e larguras" };
            restaurar.Click += (_, _) =>
            {
                PreferenciasTabelas.Limpar(chave);
                foreach (var coluna in colunas)
                {
                    OcultasPeloUsuario.Remove(coluna);
                    coluna.Visibility = Visibility.Visible;
                    coluna.Width = larguraPadrao[coluna];
                }
            };
            menu.Items.Add(restaurar);
        }

        var estilo = new Style(typeof(DataGridColumnHeader), tabela.ColumnHeaderStyle);
        estilo.Setters.Add(new EventSetter(UIElement.PreviewMouseRightButtonUpEvent, new System.Windows.Input.MouseButtonEventHandler((remetente, evento) =>
        {
            Montar();
            menu.PlacementTarget = (UIElement)remetente;
            menu.IsOpen = true;
            evento.Handled = true;
        })));
        estilo.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, "Botão direito: escolher colunas · arraste a borda para mudar a largura"));
        tabela.ColumnHeaderStyle = estilo;

        // Largura ajustada pelo usuário: a pegadeira esquerda de um cabeçalho redimensiona a coluna anterior.
        tabela.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler((_, evento) =>
        {
            if (evento.OriginalSource is not Thumb { TemplatedParent: DataGridColumnHeader { Column: { } coluna } } pegadeira)
            {
                return;
            }

            if (pegadeira.Name == "PART_LeftHeaderGripper")
            {
                coluna = tabela.Columns.Where(c => c.Visibility == Visibility.Visible && c.DisplayIndex < coluna.DisplayIndex)
                    .OrderByDescending(c => c.DisplayIndex).FirstOrDefault() ?? coluna;
            }

            if (coluna.Header is string titulo)
            {
                PreferenciasTabelas.Definir(chave, titulo, salva => salva with { Largura = Math.Round(coluna.ActualWidth) });
            }
        }), handledEventsToo: true);
    }

    /// <summary>Texto por cima da tabela vazia, dizendo o que vai ali e como começar (ver FiltroTabela.TextoSemRegistros).</summary>
    internal static TextBlock CriarAvisoVazio(DataGrid tabela)
    {
        var aviso = new TextBlock
        {
            FontSize = 13,
            Foreground = (System.Windows.Media.Brush)Application.Current.Resources["TextSecondary"],
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            MaxWidth = 460,
            Margin = new Thickness(24, 90, 24, 24),
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        AvisosVazio.AddOrUpdate(tabela, aviso);
        return aviso;
    }

    internal static void MostrarAvisoVazio(DataGrid tabela, string? texto)
    {
        if (AvisosVazio.TryGetValue(tabela, out var aviso))
        {
            aviso.Text = texto ?? string.Empty;
            aviso.Visibility = texto is null ? Visibility.Collapsed : Visibility.Visible;
        }
    }

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
            // Colunas que o usuário escondeu continuam escondidas (ver ColunasConfiguraveis).
            var opcionais = menosImportantesPrimeiro.Where(c => !OcultasPeloUsuario.TryGetValue(c, out _)).ToList();
            foreach (var coluna in opcionais)
            {
                coluna.Visibility = Visibility.Visible;
            }

            foreach (var coluna in opcionais)
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
