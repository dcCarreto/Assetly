using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Assetly.Apresentacao.Visao;

internal static class UiApoio
{
    internal static StackPanel Linha(double espaco, params UIElement[] filhos) => Empilhar(Orientation.Horizontal, espaco, filhos);

    internal static StackPanel Coluna(double espaco, params UIElement[] filhos) => Empilhar(Orientation.Vertical, espaco, filhos);

    private static StackPanel Empilhar(Orientation orientacao, double espaco, UIElement[] filhos)
    {
        var painel = new StackPanel { Orientation = orientacao };
        for (var i = 0; i < filhos.Length; i++)
        {
            if (i > 0 && filhos[i] is FrameworkElement elemento)
            {
                elemento.Margin = orientacao == Orientation.Horizontal
                    ? new Thickness(espaco, 0, 0, 0)
                    : new Thickness(0, espaco, 0, 0);
            }

            painel.Children.Add(filhos[i]);
        }

        return painel;
    }

    internal static TextBlock Texto(string conteudo, double tamanho, FontWeight peso, Brush cor) => new()
    {
        Text = conteudo,
        FontSize = tamanho,
        FontWeight = peso,
        Foreground = cor,
        TextWrapping = TextWrapping.Wrap
    };
}
