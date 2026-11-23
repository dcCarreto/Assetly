using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Shell = System.Windows.Shell;

namespace Assetly.Apresentacao.Visao;

internal static class JanelaEscura
{
    private const double AlturaBarraTitulo = 38;
    private const string FonteGlifos = "Segoe MDL2 Assets";
    private const string GlifoMinimizar = "\uE921";
    private const string GlifoMaximizar = "\uE922";
    private const string GlifoRestaurar = "\uE923";
    private const string GlifoFechar = "\uE8BB";

    /// <summary>
    /// Alterna o scrim (esmaecimento) da janela principal enquanto um dialogo modal esta aberto.
    /// Definido pela tela principal em sua construcao; PrepararDialogo o aciona automaticamente
    /// em todo dialogo (contador interno em TelaPrincipal cobre dialogos aninhados).
    /// </summary>
    internal static Action<bool>? AlternarScrim;

    internal static void Decorar(Window janela, string titulo, UIElement conteudo, UIElement? extraNaBarra = null)
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
        var barra = CriarBarraTitulo(janela, titulo, comMaximizar: true, extraNaBarra);
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
            CornerRadius = new CornerRadius(10),
            UseAeroCaptionButtons = false
        });

        AplicarMoldura(dialogo, ehDialogo: true);
        dialogo.ResizeMode = ResizeMode.NoResize;

        var raiz = new DockPanel();
        var barra = CriarBarraTitulo(dialogo, titulo, comMaximizar: false, extraNaBarra: null);
        DockPanel.SetDock(barra, Dock.Top);
        raiz.Children.Add(barra);
        raiz.Children.Add(conteudo);
        dialogo.Content = raiz;

        dialogo.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                dialogo.Close();
                e.Handled = true;
            }
        };
        dialogo.Loaded += (_, _) => AlternarScrim?.Invoke(true);
        dialogo.Closed += (_, _) => AlternarScrim?.Invoke(false);
    }

    private static void AplicarMoldura(Window janela, bool ehDialogo = false)
    {
        janela.WindowStyle = WindowStyle.None;
        janela.Background = (Brush)Application.Current.Resources[ehDialogo ? "CardBg" : "BgChrome"];
        janela.BorderBrush = (Brush)Application.Current.Resources["CardBorder"];
        janela.BorderThickness = new Thickness(1);
        // Herdado por todos os controles da janela: datas em dd/mm/aaaa no seletor de data, por exemplo.
        janela.Language = System.Windows.Markup.XmlLanguage.GetLanguage("pt-BR");
    }

    private static UIElement CriarBarraTitulo(Window janela, string titulo, bool comMaximizar, UIElement? extraNaBarra)
    {
        var linha = new Grid { Height = AlturaBarraTitulo };
        linha.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        linha.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        linha.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        linha.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
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

        linha.Children.Add(icone);
        linha.Children.Add(tituloTexto);

        if (extraNaBarra is not null)
        {
            if (extraNaBarra is FrameworkElement extraElemento)
            {
                extraElemento.Margin = new Thickness(0, 0, 10, 0);
            }

            Grid.SetColumn(extraNaBarra, 3);
            linha.Children.Add(extraNaBarra);
        }

        var minimizar = CriarBotaoJanela("Minimizar", CriarGlifo(GlifoMinimizar));
        minimizar.Click += (_, _) => janela.WindowState = WindowState.Minimized;
        minimizar.Visibility = comMaximizar ? Visibility.Visible : Visibility.Collapsed;
        Grid.SetColumn(minimizar, 4);
        Shell.WindowChrome.SetIsHitTestVisibleInChrome(minimizar, true);

        var glifoMaximizar = CriarGlifo(GlifoMaximizar);
        var maximizar = CriarBotaoJanela("Maximizar", glifoMaximizar);
        maximizar.Click += (_, _) => janela.WindowState =
            janela.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        maximizar.Visibility = comMaximizar ? Visibility.Visible : Visibility.Collapsed;
        Grid.SetColumn(maximizar, 5);
        Shell.WindowChrome.SetIsHitTestVisibleInChrome(maximizar, true);
        if (comMaximizar)
        {
            janela.StateChanged += (_, _) =>
                glifoMaximizar.Text = janela.WindowState == WindowState.Maximized ? GlifoRestaurar : GlifoMaximizar;
        }

        var fechar = CriarBotaoJanela("Fechar", CriarGlifo(GlifoFechar));
        fechar.Style = (Style)Application.Current.Resources["BotaoJanelaFecharStyle"];
        fechar.Click += (_, _) => janela.Close();
        Grid.SetColumn(fechar, 6);
        Shell.WindowChrome.SetIsHitTestVisibleInChrome(fechar, true);

        linha.Children.Add(minimizar);
        linha.Children.Add(maximizar);
        linha.Children.Add(fechar);

        return new Border
        {
            Background = (Brush)Application.Current.Resources["BgChrome"],
            BorderBrush = (Brush)Application.Current.Resources["CardBorder"],
            BorderThickness = new Thickness(0, 0, 0, 1),
            Height = AlturaBarraTitulo,
            Child = linha
        };
    }

    private static FrameworkElement CriarIcone()
    {
        var imagem = new Image
        {
            Width = 18,
            Height = 18,
            VerticalAlignment = VerticalAlignment.Center,
            Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assetly;component/Recursos/Imagens/assetly-icon-64.png"))
        };
        RenderOptions.SetBitmapScalingMode(imagem, BitmapScalingMode.HighQuality);
        return imagem;
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

    private static TextBlock CriarGlifo(string glifo) => new()
    {
        Text = glifo,
        FontFamily = new FontFamily(FonteGlifos),
        FontSize = 10,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };
}
