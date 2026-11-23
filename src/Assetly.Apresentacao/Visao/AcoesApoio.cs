using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Assetly.Apresentacao.Visao;

/// <summary>Botões, tratamento de erro, barra contextual de ações e atalhos de teclado — compartilhados
/// por todos os módulos de tabela. Extraído da antiga TelaPrincipal.Acoes.cs na modularização.</summary>
internal static class AcoesApoio
{
    internal static void ExecutarComTratamento(Action acao)
    {
        try
        {
            acao();
        }
        catch (Exception excecao) when (excecao is not OutOfMemoryException and not StackOverflowException)
        {
            // Validação (dado inválido, regra de negócio, registro inexistente) é retorno normal ao
            // usuário; o resto é inesperado e vai também para o log de diagnóstico.
            if (excecao is not (ArgumentException or InvalidOperationException or Aplicacao.Excecao.RecursoNaoEncontradoExcecao))
            {
                RegistroErros.Registrar("Erro ao executar ação", excecao);
            }

            DialogoApoio.MostrarErro(excecao);
        }
    }

    internal static T Selecionado<T>(DataGrid tabela, string mensagem) where T : class
    {
        if (tabela.SelectedItem is T item)
        {
            return item;
        }

        throw new InvalidOperationException(mensagem);
    }

    /// <summary>Botão primário de criação — mesmo estilo do botão primário, com um glifo "+" desenhado à esquerda do rótulo.</summary>
    internal static Button BotaoPrimarioCriacao(string texto, Action acao)
    {
        var recursos = Application.Current.Resources;
        System.Windows.Shapes.Rectangle Traco(double largura, double altura) => new()
        {
            Width = largura,
            Height = altura,
            Fill = (Brush)recursos["ButtonPrimaryText"],
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var glifo = new Grid { Width = 10, Height = 10, VerticalAlignment = VerticalAlignment.Center };
        glifo.Children.Add(Traco(10, 1.5));
        glifo.Children.Add(Traco(1.5, 10));

        var rotulo = new TextBlock { Text = texto, VerticalAlignment = VerticalAlignment.Center };
        var conteudo = UiApoio.Linha(7, glifo, rotulo);

        var botao = new Button { Content = conteudo, Style = (Style)recursos["BotaoPrimarioStyle"] };
        botao.Click += (_, _) => ExecutarComTratamento(acao);
        return botao;
    }

    internal static Button BotaoSecundario(string texto, Action acao)
    {
        var botao = new Button { Content = texto, Style = (Style)Application.Current.Resources["BotaoSecundarioStyle"] };
        botao.Click += (_, _) => ExecutarComTratamento(acao);
        return botao;
    }

    internal static Button BotaoPerigo(string texto, Action acao)
    {
        var botao = new Button { Content = texto, Style = (Style)Application.Current.Resources["BotaoPerigoStyle"] };
        botao.Click += (_, _) => ExecutarComTratamento(acao);
        return botao;
    }

    /// <summary>
    /// Barra de ações ancorada no rodapé do contêiner da tabela — só aparece quando há seleção.
    /// Ações seguras ficam à esquerda; destrutivas, encostadas na borda direita, bem afastadas das
    /// seguras para que um erro de clique não seja plausível.
    /// </summary>
    internal static Border BarraContextual(DataGrid tabela, (string Texto, Action Acao)[] seguras, (string Texto, Action Acao)[] destrutivas)
    {
        var recursos = Application.Current.Resources;

        var contagem = new TextBlock
        {
            Text = "1 selecionado",
            FontFamily = (FontFamily)recursos["FontMono"],
            FontSize = 11,
            Foreground = (Brush)recursos["TextTitle"],
            VerticalAlignment = VerticalAlignment.Center
        };

        var limpar = new Button { Style = (Style)recursos["BotaoLimparSelecaoStyle"], VerticalAlignment = VerticalAlignment.Center };
        limpar.Click += (_, _) => tabela.SelectedItem = null;

        var divisor = new Border { Width = 1, Height = 18, Background = (Brush)recursos["InputBorder"], VerticalAlignment = VerticalAlignment.Center };

        var elementosEsquerda = new List<UIElement> { limpar, contagem, divisor };
        elementosEsquerda.AddRange(seguras.Select(a => (UIElement)BotaoSecundario(a.Texto, a.Acao)));
        var painelEsquerda = UiApoio.Linha(10, elementosEsquerda.ToArray());

        var painelDireita = UiApoio.Linha(8, destrutivas.Select(a => (UIElement)BotaoPerigo(a.Texto, a.Acao)).ToArray());

        var grade = new Grid();
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(painelEsquerda, 0);
        Grid.SetColumn(painelDireita, 1);
        grade.Children.Add(painelEsquerda);
        grade.Children.Add(painelDireita);

        var barra = new Border
        {
            Background = (Brush)recursos["SelectionBarBg"],
            BorderBrush = (Brush)recursos["InputBorder"],
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(14, 0, 12, 0),
            Height = 46,
            Visibility = Visibility.Collapsed,
            Child = grade
        };

        tabela.SelectionChanged += (_, _) => barra.Visibility = tabela.SelectedItem is null ? Visibility.Collapsed : Visibility.Visible;
        return barra;
    }

    /// <summary>Atalhos de teclado comuns às telas de tabela: Ctrl+N cria, Ctrl+F foca a busca,
    /// F2 edita a seleção, Delete exclui a seleção (sempre com confirmação), Esc limpa a
    /// seleção. Com o foco na caixa de busca, Delete/F2/Esc continuam sendo do campo de texto.</summary>
    internal static void ConfigurarAtalhosTabela(UIElement raiz, DataGrid tabela, TextBox busca, Action criar, Action? editar, Action? destrutivaPrincipal)
    {
        raiz.Focusable = true;
        if (raiz is FrameworkElement elemento)
        {
            elemento.FocusVisualStyle = null;
        }

        raiz.PreviewKeyDown += (_, e) =>
        {
            // Digitando na busca: Delete/F2/Esc pertencem ao campo de texto, não à tabela.
            var digitando = Keyboard.FocusedElement is TextBox;
            if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ExecutarComTratamento(criar);
                e.Handled = true;
            }
            else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                busca.Focus();
                busca.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == Key.F2 && !digitando && editar is not null && tabela.SelectedItem is not null)
            {
                ExecutarComTratamento(editar);
                e.Handled = true;
            }
            else if (e.Key == Key.Delete && !digitando && destrutivaPrincipal is not null && tabela.SelectedItem is not null)
            {
                ExecutarComTratamento(destrutivaPrincipal);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && !digitando && tabela.SelectedItem is not null)
            {
                tabela.SelectedItem = null;
                e.Handled = true;
            }
        };
    }
}
