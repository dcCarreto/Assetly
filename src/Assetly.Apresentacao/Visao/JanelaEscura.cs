using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
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
        CorrigirLimitesMaximizados(janela);

        var raiz = new DockPanel();
        var barra = CriarBarraTitulo(janela, titulo, comMaximizar: true);
        DockPanel.SetDock(barra, Dock.Top);
        raiz.Children.Add(barra);
        raiz.Children.Add(conteudo);
        janela.Content = raiz;
    }

    /// <summary>
    /// Sem chrome nativo (WindowStyle.None), o WPF nao encolhe automaticamente os
    /// limites da janela maximizada para excluir a barra de tarefas — ela cobre a
    /// barra de tarefas por padrao. Este e o hook padrao (WM_GETMINMAXINFO) usado
    /// para corrigir isso, restringindo a maximizacao a area de trabalho do monitor.
    /// </summary>
    private static void CorrigirLimitesMaximizados(Window janela)
    {
        janela.SourceInitialized += (_, _) =>
        {
            var identificador = new WindowInteropHelper(janela).Handle;
            HwndSource.FromHwnd(identificador)?.AddHook(AoReceberMensagem);
        };
    }

    private static IntPtr AoReceberMensagem(IntPtr hwnd, int mensagem, IntPtr wParam, IntPtr lParam, ref bool tratado)
    {
        const int wmGetMinMaxInfo = 0x0024;
        if (mensagem == wmGetMinMaxInfo)
        {
            AjustarMinMaxInfo(hwnd, lParam);
            tratado = true;
        }

        return IntPtr.Zero;
    }

    private static void AjustarMinMaxInfo(IntPtr hwnd, IntPtr lParam)
    {
        var minMaxInfo = Marshal.PtrToStructure<MinMaxInfo>(lParam);

        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor != IntPtr.Zero)
        {
            var infoMonitor = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
            GetMonitorInfo(monitor, ref infoMonitor);

            var areaTrabalho = infoMonitor.rcWork;
            var areaMonitor = infoMonitor.rcMonitor;

            minMaxInfo.ptMaxPosition.X = areaTrabalho.Left - areaMonitor.Left;
            minMaxInfo.ptMaxPosition.Y = areaTrabalho.Top - areaMonitor.Top;
            minMaxInfo.ptMaxSize.X = areaTrabalho.Right - areaTrabalho.Left;
            minMaxInfo.ptMaxSize.Y = areaTrabalho.Bottom - areaTrabalho.Top;
        }

        Marshal.StructureToPtr(minMaxInfo, lParam, true);
    }

    private const int MonitorDefaultToNearest = 2;

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo infoMonitor);

    [StructLayout(LayoutKind.Sequential)]
    private struct RectNativo
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int cbSize;
        public RectNativo rcMonitor;
        public RectNativo rcWork;
        public int dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PontoNativo
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public PontoNativo ptReserved;
        public PontoNativo ptMaxSize;
        public PontoNativo ptMaxPosition;
        public PontoNativo ptMinTrackSize;
        public PontoNativo ptMaxTrackSize;
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
