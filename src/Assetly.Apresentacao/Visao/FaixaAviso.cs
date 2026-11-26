using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Assetly.Apresentacao.Visao;

/// <summary>Faixa de aviso no topo de uma tela (ex.: "Você ainda não fez nenhum backup"), escondida quando não há o que
/// avisar. Vermelha quando o problema é crítico, laranja nos demais; o botão opcional leva à ação.</summary>
internal sealed class FaixaAviso
{
    private readonly TextBlock _texto = new() { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, FontSize = 12.5 };

    internal Border Raiz { get; } = new()
    {
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(14, 10, 10, 10),
        Margin = new Thickness(0, 0, 0, 14),
        Visibility = Visibility.Collapsed
    };

    internal FaixaAviso(string? textoBotao = null, Action? acao = null)
    {
        var linha = new Grid();
        linha.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        linha.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        linha.Children.Add(_texto);
        if (textoBotao is not null && acao is not null)
        {
            var botao = AcoesApoio.BotaoSecundario(textoBotao, acao);
            botao.Margin = new Thickness(12, 0, 0, 0);
            Grid.SetColumn(botao, 1);
            linha.Children.Add(botao);
        }

        Raiz.Child = linha;
        System.Windows.Automation.AutomationProperties.SetLiveSetting(_texto, System.Windows.Automation.AutomationLiveSetting.Polite);
    }

    /// <summary>Mostra a mensagem; nula esconde a faixa.</summary>
    internal void Mostrar(string? mensagem, bool critico)
    {
        Raiz.Visibility = mensagem is null ? Visibility.Collapsed : Visibility.Visible;
        if (mensagem is null)
        {
            return;
        }

        var recursos = Application.Current.Resources;
        var prefixo = critico ? "StatusPerigo" : "StatusAviso";
        Raiz.Background = (Brush)recursos[prefixo + "Bg"];
        Raiz.BorderBrush = (Brush)recursos[prefixo + "Border"];
        _texto.Foreground = (Brush)recursos[prefixo + "Text"];
        _texto.Text = mensagem;
    }
}
