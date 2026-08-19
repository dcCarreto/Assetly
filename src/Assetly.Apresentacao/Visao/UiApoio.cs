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

    /// <summary>
    /// Linha com um elemento de largura fixa à esquerda e um elemento elástico à direita
    /// que ocupa o espaço restante — necessário para que TextBlocks com TextWrapping
    /// realmente quebrem linha em vez de estourar a largura (StackPanel não restringe
    /// a largura dos filhos, então TextWrapping nunca é acionado dentro de um).
    /// </summary>
    internal static Grid LinhaComCrescimento(double espaco, UIElement fixo, UIElement elastico)
    {
        var grade = new Grid();
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        if (elastico is FrameworkElement elementoElastico)
        {
            elementoElastico.Margin = new Thickness(espaco, 0, 0, 0);
            elementoElastico.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        Grid.SetColumn(fixo, 0);
        Grid.SetColumn(elastico, 1);
        grade.Children.Add(fixo);
        grade.Children.Add(elastico);
        return grade;
    }
}
