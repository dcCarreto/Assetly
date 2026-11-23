using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Assetly.Apresentacao.Visao;

/// <summary>
/// Janela de QA visual do tema — não faz parte do fluxo normal do app, só existe para
/// conferir tokens, tipografia e componentes lado a lado. Aberta via "dotnet run -- --tema".
/// Reaproveita os mesmos construtores usados pela tela principal (ResumoOperacao, FaixaIndicadores)
/// sempre que possível, para que a galeria reflita o componente real, não uma cópia divergente.
/// </summary>
internal static class ThemeGalleryWindow
{
    internal static void Mostrar()
    {
        var raiz = new StackPanel { Margin = new Thickness(24) };

        raiz.Children.Add(Secao("Selo de ambiente (barra de título)", FaixaSelos()));
        raiz.Children.Add(Secao("Menu lateral — bloco de marca + itens", MenuLateralDemo()));
        raiz.Children.Add(Secao("Cards de resumo + faixa de indicadores", CardsEIndicadoresDemo()));
        raiz.Children.Add(Secao("Tabela — tingimento por status (OK sem tingimento)", TabelaDemo()));
        raiz.Children.Add(Secao("Barra contextual de ações (destrutiva à direita)", BarraContextualDemo()));
        raiz.Children.Add(Secao("Inputs, botões e chips de filtro", FaixaControlesDemo()));
        raiz.Children.Add(Secao("Diálogo com scrim", DialogoComScrimDemo()));

        var scroll = new ScrollViewer { Content = raiz, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

        var janela = new Window
        {
            Title = "Galeria do tema — Assetly",
            Width = 960,
            Height = 820,
            MinWidth = 700,
            MinHeight = 500
        };

        JanelaEscura.Decorar(janela, "Galeria do tema — Assetly", scroll);
        Application.Current.MainWindow = janela;
        janela.Show();
    }

    private static FrameworkElement Secao(string titulo, FrameworkElement conteudo)
    {
        var tituloLabel = new TextBlock { Text = titulo, Style = (Style)Application.Current.Resources["TituloPaginaStyle"] };
        var coluna = UiApoio.Coluna(10, tituloLabel, conteudo);
        coluna.Margin = new Thickness(0, 0, 0, 26);
        return coluna;
    }

    private static FrameworkElement FaixaSelos()
    {
        var recursos = Application.Current.Resources;
        Border Selo(string texto, string prefixo)
        {
            var ponto = new Border { Width = 5, Height = 5, CornerRadius = new CornerRadius(2.5), Background = (Brush)recursos[prefixo + "Border"], VerticalAlignment = VerticalAlignment.Center };
            var etiqueta = new TextBlock
            {
                Text = texto.ToUpperInvariant(),
                FontFamily = (FontFamily)recursos["FontMono"],
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)recursos[prefixo + "Text"],
                VerticalAlignment = VerticalAlignment.Center
            };
            return new Border { Child = UiApoio.Linha(6, ponto, etiqueta), Margin = new Thickness(0, 0, 20, 0) };
        }

        var fundo = new Border { Background = (Brush)recursos["BgChrome"], Padding = new Thickness(12), CornerRadius = new CornerRadius(6) };
        fundo.Child = UiApoio.Linha(0, Selo("Produção local", "StatusOk"), Selo("Teste", "StatusAviso"));
        return fundo;
    }

    private static FrameworkElement MenuLateralDemo()
    {
        var recursos = Application.Current.Resources;
        var menu = new StackPanel { Background = (Brush)recursos["BgSidebar"], Width = 208 };

        var logo = new Border
        {
            Width = 30,
            Height = 30,
            CornerRadius = new CornerRadius(7),
            Background = (Brush)recursos["LogoGradient"],
            Child = new TextBlock { Text = "A", FontWeight = FontWeights.SemiBold, FontSize = 15, Foreground = (Brush)recursos["ButtonPrimaryText"], HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        };
        var nomeApp = UiApoio.Texto("Assetly", 13.5, FontWeights.SemiBold, (Brush)recursos["TextTitle"]);
        var subtitulo = UiApoio.Texto("Gestão local de bens", 10.5, FontWeights.Normal, (Brush)recursos["TextMuted"]);
        var marca = new Border
        {
            Padding = new Thickness(15, 14, 14, 14),
            BorderBrush = (Brush)recursos["BgSurface2"],
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = UiApoio.Linha(10, logo, UiApoio.Coluna(1, nomeApp, subtitulo))
        };
        menu.Children.Add(marca);

        var itensPainel = new StackPanel { Margin = new Thickness(10, 10, 10, 10) };
        (string Titulo, string Descricao, string Contador, bool Selecionado)[] itens =
        [
            ("Painel", "Resumo operacional", "", true),
            ("Alertas", "Pendências abertas", "8", false)
        ];

        foreach (var (titulo, descricao, contadorTexto, selecionado) in itens)
        {
            var icone = new Border { Width = 11, Height = 11, CornerRadius = new CornerRadius(3), Background = (Brush)recursos[selecionado ? "SidebarIconSelected" : "SidebarIconNormal"], VerticalAlignment = VerticalAlignment.Center };
            var textos = UiApoio.Coluna(2,
                new TextBlock { Text = titulo, FontSize = 12.5, FontWeight = selecionado ? FontWeights.SemiBold : FontWeights.Normal, Foreground = (Brush)recursos[selecionado ? "TextTitle" : "TextSecondary"] },
                UiApoio.Texto(descricao, 11, FontWeights.Normal, (Brush)recursos["TextMuted"]));
            textos.VerticalAlignment = VerticalAlignment.Center;

            var grade = new Grid { Height = 46 };
            grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(icone, 0);
            Grid.SetColumn(textos, 1);
            grade.Children.Add(icone);
            grade.Children.Add(textos);
            if (!string.IsNullOrEmpty(contadorTexto))
            {
                var contador = new TextBlock
                {
                    Text = contadorTexto,
                    FontFamily = (FontFamily)recursos["FontMono"],
                    FontSize = 10.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)recursos["StatusAvisoText"],
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(contador, 2);
                grade.Children.Add(contador);
            }

            var item = new Border
            {
                Background = selecionado ? (Brush)recursos["BgSurface2"] : Brushes.Transparent,
                BorderBrush = selecionado ? (Brush)recursos["AccentPrimary"] : Brushes.Transparent,
                BorderThickness = new Thickness(2, 0, 0, 0),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(15, 0, 14, 0),
                Margin = new Thickness(0, 0, 0, 2),
                Child = grade
            };
            itensPainel.Children.Add(item);
        }

        menu.Children.Add(itensPainel);

        return new Border { BorderBrush = (Brush)recursos["CardBorder"], BorderThickness = new Thickness(1), Child = menu };
    }

    private static FrameworkElement CardsEIndicadoresDemo()
    {
        var grade = new UniformGrid { Columns = 3, Height = 88 };
        grade.Children.Add(LayoutApoio.ResumoOperacao("Atenção necessária", "3", "itens pedem revisão", "StatusPerigo"));
        grade.Children.Add(LayoutApoio.ResumoOperacao("Alertas críticos", "0", "sem pendências críticas", "StatusOk"));
        grade.Children.Add(LayoutApoio.ResumoOperacao("Base local", "42", "registros sob controle", "StatusInfo"));

        var faixa = LayoutApoio.FaixaIndicadores(
            ("Bens monitorados", 42, false, null),
            ("Garantias vencidas", 1, true, "StatusPerigo"),
            ("Garantias próximas", 2, true, "StatusAviso"),
            ("Manutenções atrasadas", 0, false, "StatusPerigo"),
            ("Documentos ausentes", 0, false, null),
            ("Alertas abertos", 8, true, "StatusInfo"));
        faixa.Margin = new Thickness(0, 12, 0, 0);

        return UiApoio.Coluna(0, grade, faixa);
    }

    private sealed record LinhaGaleria(string Nome, string Status, string StatusClasse, string LinhaClasse);

    private static FrameworkElement TabelaDemo()
    {
        var linhas = new List<LinhaGaleria>
        {
            new("Notebook de trabalho", "Ativo", "etiqueta-ativo", "linha-ok"),
            new("Ar-condicionado da sala", "Em manutenção", "etiqueta-em-manutencao", "linha-aviso"),
            new("Geladeira", "Garantia a vencer", "etiqueta-perto-do-vencimento", "linha-aviso"),
            new("Impressora antiga", "Descartado", "etiqueta-descartado", "linha-perigo"),
            new("Monitor reserva", "Arquivado", "etiqueta-arquivado", "linha-neutra")
        };

        var tabela = new DataGrid();
        TabelaApoio.AplicarEstiloBase(tabela);
        tabela.Columns.Add(TabelaApoio.ColunaTexto("Bem", nameof(LinhaGaleria.Nome), new DataGridLength(1, DataGridLengthUnitType.Star)));
        tabela.Columns.Add(TabelaApoio.ColunaEtiqueta("Status", nameof(LinhaGaleria.Status), nameof(LinhaGaleria.StatusClasse), new DataGridLength(170)));
        TabelaApoio.ConfigurarLinhas<LinhaGaleria>(tabela, l => l.LinhaClasse);
        tabela.ItemsSource = linhas;
        tabela.MaxHeight = 220;

        return new Border { Style = (Style)Application.Current.Resources["CardPanelStyle"], Child = tabela };
    }

    private static FrameworkElement BarraContextualDemo()
    {
        var recursos = Application.Current.Resources;
        var limpar = new Button { Style = (Style)recursos["BotaoLimparSelecaoStyle"], VerticalAlignment = VerticalAlignment.Center };
        var contagem = new TextBlock { Text = "1 selecionado", FontFamily = (FontFamily)recursos["FontMono"], FontSize = 11, Foreground = (Brush)recursos["TextTitle"], VerticalAlignment = VerticalAlignment.Center };
        var divisor = new Border { Width = 1, Height = 18, Background = (Brush)recursos["InputBorder"], VerticalAlignment = VerticalAlignment.Center };
        var detalhes = new Button { Content = "Detalhes", Style = (Style)recursos["BotaoSecundarioStyle"] };
        var editar = new Button { Content = "Editar", Style = (Style)recursos["BotaoSecundarioStyle"] };
        var esquerda = UiApoio.Linha(10, limpar, contagem, divisor, detalhes, editar);

        var descartar = new Button { Content = "Descartar", Style = (Style)recursos["BotaoPerigoStyle"] };
        var arquivar = new Button { Content = "Arquivar", Style = (Style)recursos["BotaoPerigoStyle"] };
        var direita = UiApoio.Linha(8, descartar, arquivar);

        var grade = new Grid();
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(esquerda, 0);
        Grid.SetColumn(direita, 1);
        grade.Children.Add(esquerda);
        grade.Children.Add(direita);

        return new Border
        {
            Background = (Brush)recursos["SelectionBarBg"],
            BorderBrush = (Brush)recursos["InputBorder"],
            BorderThickness = new Thickness(1),
            Padding = new Thickness(14, 0, 12, 0),
            Height = 46,
            Child = grade
        };
    }

    private static FrameworkElement FaixaControlesDemo()
    {
        var recursos = Application.Current.Resources;

        var input = new TextBox { Style = (Style)recursos["CampoTextoStyle"], Text = "Campo com foco", Width = 200 };
        var combo = new ComboBox { Style = (Style)recursos["ComboStyle"], Width = 160 };
        combo.Items.Add("Eletrônico");
        combo.Items.Add("Móvel");
        combo.SelectedIndex = 0;

        var primario = new Button { Content = "Salvar", Style = (Style)recursos["BotaoPrimarioStyle"], Margin = new Thickness(0, 0, 8, 0) };
        var secundario = new Button { Content = "Cancelar", Style = (Style)recursos["BotaoSecundarioStyle"], Margin = new Thickness(0, 0, 8, 0) };
        var perigo = new Button { Content = "Excluir", Style = (Style)recursos["BotaoPerigoStyle"] };
        var linhaControles = UiApoio.Linha(16, input, combo, primario, secundario, perigo);
        input.Loaded += (_, _) => input.Focus();

        var chipTodos = new ToggleButton { Style = (Style)recursos["ChipFilterStyle"], Content = "Todos 9", IsChecked = true, Margin = new Thickness(0, 0, 8, 0) };
        var chipAtivos = new ToggleButton { Style = (Style)recursos["ChipFilterStyle"], Content = "Ativos 6", Margin = new Thickness(0, 0, 8, 0) };
        var chipArquivados = new ToggleButton { Style = (Style)recursos["ChipFilterStyle"], Content = "Arquivados 1" };
        var linhaChips = UiApoio.Linha(0, chipTodos, chipAtivos, chipArquivados);
        linhaChips.Margin = new Thickness(0, 14, 0, 0);

        return UiApoio.Coluna(0, linhaControles, linhaChips);
    }

    private static FrameworkElement DialogoComScrimDemo()
    {
        var recursos = Application.Current.Resources;
        var fundo = new Border { Background = (Brush)recursos["CardBg"], Height = 160, CornerRadius = new CornerRadius(6) };

        var barraTitulo = new Border { Background = (Brush)recursos["BgSurface1"], BorderBrush = (Brush)recursos["CardBorder"], BorderThickness = new Thickness(0, 0, 0, 1), Height = 32, Child = UiApoio.Texto("Novo bem", 12.5, FontWeights.SemiBold, (Brush)recursos["TextTitle"]) };
        ((TextBlock)barraTitulo.Child).Margin = new Thickness(14, 0, 0, 0);
        ((TextBlock)barraTitulo.Child).VerticalAlignment = VerticalAlignment.Center;

        var nota = UiApoio.Texto("* campo obrigatório", 11, FontWeights.Normal, (Brush)recursos["TextMuted"]);
        var salvar = new Button { Content = "Salvar", Style = (Style)recursos["BotaoPrimarioStyle"], MinWidth = 80 };
        var rodapeGrade = new Grid();
        rodapeGrade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rodapeGrade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(nota, 0);
        Grid.SetColumn(salvar, 1);
        rodapeGrade.Children.Add(nota);
        rodapeGrade.Children.Add(salvar);
        var rodape = new Border { Background = (Brush)recursos["TableFooterBg"], BorderBrush = (Brush)recursos["CardBorder"], BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(14, 10, 14, 10), Child = rodapeGrade };

        var corpo = new DockPanel();
        DockPanel.SetDock(barraTitulo, Dock.Top);
        DockPanel.SetDock(rodape, Dock.Bottom);
        corpo.Children.Add(barraTitulo);
        corpo.Children.Add(rodape);
        corpo.Children.Add(new Border { Padding = new Thickness(16) });
        fundo.Child = corpo;
        fundo.BorderBrush = (Brush)recursos["CardBorder"];
        fundo.BorderThickness = new Thickness(1);
        fundo.Width = 420;
        fundo.HorizontalAlignment = HorizontalAlignment.Center;

        var scrim = new Border { Background = (Brush)recursos["DialogScrim"], Height = 200 };
        var raiz = new Grid();
        raiz.Children.Add(scrim);
        raiz.Children.Add(new Border { Child = fundo, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(0, 20, 0, 20) });
        return raiz;
    }
}
