using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Shell = System.Windows.Shell;

namespace Assetly.Apresentacao.Visao;

internal static class JanelaEscura
{
    private const double AlturaBarraTitulo = 30;

    internal static void Decorar(Window janela, string titulo, UIElement conteudo)
    {
        Shell.WindowChrome.SetWindowChrome(janela, new Shell.WindowChrome
        {
            CaptionHeight = AlturaBarraTitulo,
            ResizeBorderThickness = new Thickness(6),
            GlassFrameThickness = new Thickness(0),
            CornerRadius = new CornerRadius(0),
            UseAeroCaptionButtons = false
        });

        AplicarMoldura(janela);

        var raiz = new DockPanel();
        var barra = CriarBarraTitulo(janela, titulo, comMaximizar: true);
        DockPanel.SetDock(barra, Dock.Top);
        raiz.Children.Add(barra);
        raiz.Children.Add(conteudo);
        janela.Content = raiz;

        janela.StateChanged += (_, _) => raiz.Margin = janela.WindowState == WindowState.Maximized
            ? new Thickness(SystemParameters.WindowResizeBorderThickness.Left, SystemParameters.WindowResizeBorderThickness.Top,
                SystemParameters.WindowResizeBorderThickness.Right, SystemParameters.WindowResizeBorderThickness.Bottom)
            : new Thickness(0);
    }

    internal static void PrepararDialogo(Window dialogo, string titulo, UIElement conteudo)
    {
        Shell.WindowChrome.SetWindowChrome(dialogo, new Shell.WindowChrome
        {
            CaptionHeight = AlturaBarraTitulo,
            ResizeBorderThickness = new Thickness(0),
            GlassFrameThickness = new Thickness(0),
            CornerRadius = new CornerRadius(0),
            UseAeroCaptionButtons = false
        });

        AplicarMoldura(dialogo);
        dialogo.ResizeMode = ResizeMode.NoResize;

        var raiz = new DockPanel();
        var barra = CriarBarraTitulo(dialogo, titulo, comMaximizar: false);
        DockPanel.SetDock(barra, Dock.Top);
        raiz.Children.Add(barra);
        raiz.Children.Add(conteudo);
        dialogo.Content = raiz;
    }

    private static void AplicarMoldura(Window janela)
    {
        janela.WindowStyle = WindowStyle.None;
        janela.Background = (Brush)Application.Current.Resources["CorJanelaEscuraFundo"];
        janela.BorderBrush = (Brush)Application.Current.Resources["CorJanelaEscuraBorda"];
        janela.BorderThickness = new Thickness(1);
    }

    private static UIElement CriarBarraTitulo(Window janela, string titulo, bool comMaximizar)
    {
        var linha = new Grid { Height = AlturaBarraTitulo };
        linha.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        linha.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        linha.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        linha.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        linha.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        linha.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icone = CriarIcone();
        icone.Margin = new Thickness(10, 0, 0, 0);
        Grid.SetColumn(icone, 0);

        var tituloTexto = new TextBlock
        {
            Text = titulo,
            Style = (Style)Application.Current.Resources["TituloJanelaTextoStyle"],
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(tituloTexto, 1);

        var minimizar = CriarBotaoJanela("Minimizar", CriarGlifoMinimizar());
        minimizar.Click += (_, _) => janela.WindowState = WindowState.Minimized;
        minimizar.Visibility = comMaximizar ? Visibility.Visible : Visibility.Collapsed;
        Grid.SetColumn(minimizar, 3);
        Shell.WindowChrome.SetIsHitTestVisibleInChrome(minimizar, true);

        var maximizar = CriarBotaoJanela("Maximizar", CriarGlifoMaximizar());
        maximizar.Click += (_, _) => janela.WindowState =
            janela.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        maximizar.Visibility = comMaximizar ? Visibility.Visible : Visibility.Collapsed;
        Grid.SetColumn(maximizar, 4);
        Shell.WindowChrome.SetIsHitTestVisibleInChrome(maximizar, true);

        var fechar = CriarBotaoJanela("Fechar", CriarGlifoFechar());
        fechar.Style = (Style)Application.Current.Resources["BotaoJanelaFecharStyle"];
        fechar.Click += (_, _) => janela.Close();
        Grid.SetColumn(fechar, 5);
        Shell.WindowChrome.SetIsHitTestVisibleInChrome(fechar, true);

        linha.Children.Add(icone);
        linha.Children.Add(tituloTexto);
        linha.Children.Add(minimizar);
        linha.Children.Add(maximizar);
        linha.Children.Add(fechar);

        return new Border
        {
            Background = (Brush)Application.Current.Resources["BarraTituloBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CorJanelaEscuraBorda"],
            BorderThickness = new Thickness(0, 0, 0, 1),
            Height = AlturaBarraTitulo,
            Child = linha
        };
    }

    private static FrameworkElement CriarIcone()
    {
        var uri = new Uri("pack://application:,,,/Assetly;component/Recursos/Imagens/assetly-icon-16.png");
        try
        {
            return new Image
            {
                Source = new BitmapImage(uri),
                Width = 16,
                Height = 16,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
        catch (IOException)
        {
            return new TextBlock
            {
                Text = "A",
                Foreground = (Brush)Application.Current.Resources["CorAccent"],
                FontWeight = FontWeights.ExtraBold,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
    }

    private static Button CriarBotaoJanela(string descricao, UIElement glifo)
    {
        var botao = new Button
        {
            Style = (Style)Application.Current.Resources["BotaoJanelaStyle"],
            Content = glifo,
            Focusable = false
        };
        AutomationProperties.SetName(botao, descricao);
        return botao;
    }

    private static UIElement CriarGlifoMinimizar() =>
        new Line { X1 = 0, Y1 = 0, X2 = 12, Y2 = 0, StrokeThickness = 2, Stroke = LinhaCor(), VerticalAlignment = VerticalAlignment.Center };

    private static UIElement CriarGlifoMaximizar() => new Rectangle
    {
        Width = 10,
        Height = 10,
        Stroke = LinhaCor(),
        StrokeThickness = 1.5,
        Fill = Brushes.Transparent
    };

    private static UIElement CriarGlifoFechar()
    {
        var canvas = new Canvas { Width = 12, Height = 12 };
        canvas.Children.Add(new Line { X1 = 0, Y1 = 0, X2 = 12, Y2 = 12, StrokeThickness = 1.5, Stroke = LinhaCor() });
        canvas.Children.Add(new Line { X1 = 12, Y1 = 0, X2 = 0, Y2 = 12, StrokeThickness = 1.5, Stroke = LinhaCor() });
        return canvas;
    }

    private static Brush LinhaCor() => new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1));
}
