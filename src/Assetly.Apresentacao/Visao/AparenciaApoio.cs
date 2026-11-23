using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Assetly.Dominio.Preferencias;

namespace Assetly.Apresentacao.Visao;

/// <summary>Aplica tema/cor de destaque/densidade em runtime. Boa parte do app (chrome da janela,
/// menu lateral, cabeçalhos construídos em C#) lê `Application.Current.Resources[...]` uma única vez
/// na construção — trocar só as entradas do dicionário não bastaria para reflear tudo, então quem
/// chama <see cref="Aplicar"/> depois de um lançamento inicial também precisa reconstruir a árvore
/// visual (ver App.xaml.cs, ReconstruirAparencia). AccentPrimary/AccentPrimaryHalo/SidebarIconSelected/
/// InputFocusBorder/FocusHalo resolvem sua Color via StaticResource dentro de Cores.xaml — por isso a
/// cor de destaque é aplicada sobrescrevendo os objetos de brush/effect diretamente, não a Color de
/// origem, que já foi capturada e não reagiria à mudança.</summary>
internal static class AparenciaApoio
{
    internal static void Aplicar(ConfiguracaoPreferencias preferencias)
    {
        AplicarTema(preferencias.Tema);
        AplicarCorDestaque(preferencias.CorDestaque);
        AplicarDensidade(preferencias.DensidadeTabela);
    }

    internal static void AplicarTema(TemaVisual tema)
    {
        var arquivo = tema == TemaVisual.Claro ? "CoresClaro.xaml" : "Cores.xaml";
        Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary { Source = UriTema(arquivo) };
    }

    internal static void AplicarCorDestaque(CorDestaque corDestaque)
    {
        var paleta = ObterPaletaAccent(corDestaque);
        var recursos = Application.Current.Resources;
        recursos["AccentPrimary"] = new SolidColorBrush(paleta.Principal);
        recursos["AccentPrimaryHalo"] = new SolidColorBrush(paleta.Halo);
        recursos["SidebarIconSelected"] = new SolidColorBrush(paleta.Principal);
        recursos["InputFocusBorder"] = new SolidColorBrush(paleta.Principal);
        recursos["FocusHalo"] = new DropShadowEffect { Color = paleta.Principal, Opacity = 0.35, BlurRadius = 6, ShadowDepth = 0 };
        recursos["ButtonPrimaryText"] = new SolidColorBrush(paleta.TextoBotao);
        recursos["PrimaryButtonGradient"] = Gradiente(paleta.GradienteInicio, paleta.GradienteFim);
        recursos["PrimaryButtonHoverGradient"] = Gradiente(paleta.HoverInicio, paleta.HoverFim);
        recursos["PrimaryButtonPressedGradient"] = Gradiente(paleta.PressedInicio, paleta.PressedFim);
    }

    internal static void AplicarDensidade(DensidadeTabela densidadeTabela)
    {
        var recursos = Application.Current.Resources;
        recursos["TabelaAlturaLinha"] = densidadeTabela == DensidadeTabela.Compacta ? 28d : 36d;
        recursos["TabelaPaddingCelula"] = densidadeTabela == DensidadeTabela.Compacta ? new Thickness(8, 0, 8, 0) : new Thickness(10, 0, 10, 0);
    }

    private static Uri UriTema(string arquivo) => new($"pack://application:,,,/Assetly;component/Temas/{arquivo}", UriKind.Absolute);

    private static LinearGradientBrush Gradiente(Color inicio, Color fim) =>
        new(inicio, fim, new Point(0, 0), new Point(1, 0));

    /// <summary>Presets de cor de destaque — Verde e Âmbar usam matizes deliberadamente distintos
    /// das cores semânticas StatusOk/StatusAviso (verde-floresta e âmbar-dourado, respectivamente),
    /// para não confundir "botão de ação" com "selo de status" na mesma tela.</summary>
    private static PaletaAccent ObterPaletaAccent(CorDestaque corDestaque) => corDestaque switch
    {
        CorDestaque.Violeta => new PaletaAccent(
            Cor("#A78BFA"), CorComAlfa(0x33, "#A78BFA"), Cor("#2E1065"),
            Cor("#A78BFA"), Cor("#7C3AED"), Cor("#C4B5FD"), Cor("#A78BFA"), Cor("#7C3AED"), Cor("#6D28D9")),
        CorDestaque.Verde => new PaletaAccent(
            Cor("#84CC16"), CorComAlfa(0x33, "#84CC16"), Cor("#1A2E05"),
            Cor("#A3E635"), Cor("#65A30D"), Cor("#BEF264"), Cor("#84CC16"), Cor("#65A30D"), Cor("#4D7C0F")),
        CorDestaque.Ambar => new PaletaAccent(
            Cor("#F97316"), CorComAlfa(0x33, "#F97316"), Cor("#431407"),
            Cor("#FB923C"), Cor("#EA580C"), Cor("#FDBA74"), Cor("#FB923C"), Cor("#EA580C"), Cor("#C2410C")),
        _ => new PaletaAccent(
            Cor("#2DD4BF"), CorComAlfa(0x33, "#2DD4BF"), Cor("#022C26"),
            Cor("#14B8A6"), Cor("#0D9488"), Cor("#2DD4BF"), Cor("#14B8A6"), Cor("#0D9488"), Cor("#0F766E"))
    };

    private static Color Cor(string hex) => (Color)ColorConverter.ConvertFromString(hex)!;

    private static Color CorComAlfa(byte alfa, string hex)
    {
        var cor = Cor(hex);
        return Color.FromArgb(alfa, cor.R, cor.G, cor.B);
    }

    private sealed record PaletaAccent(
        Color Principal, Color Halo, Color TextoBotao,
        Color GradienteInicio, Color GradienteFim,
        Color HoverInicio, Color HoverFim,
        Color PressedInicio, Color PressedFim);
}
