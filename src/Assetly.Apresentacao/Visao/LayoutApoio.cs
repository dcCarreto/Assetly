using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Assetly.Apresentacao.Visao;

/// <summary>Estrutura de tela compartilhada por todos os módulos: primeira linha (título + ação),
/// contêiner de tabela, cards de configuração/resumo. Extraído da antiga TelaPrincipal.Abas.cs /
/// TelaPrincipal.cs na modularização — inclui os dois construtores (ResumoOperacao, FaixaIndicadores)
/// também usados pela galeria do tema (ThemeGalleryWindow), por isso ficam aqui e não em um módulo.</summary>
internal static class LayoutApoio
{
    /// <summary>Primeira linha de toda tela: título + subtítulo à esquerda, conteúdo específico da tela
    /// (busca + criação, ou o resumo do Painel) à direita.</summary>
    internal static FrameworkElement CabecalhoTela(string titulo, string subtitulo, FrameworkElement? direita)
    {
        var recursos = Application.Current.Resources;
        var tituloLabel = new TextBlock
        {
            Text = titulo,
            FontFamily = (FontFamily)recursos["FontUi"],
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)recursos["TextTitle"]
        };
        var subtituloLabel = UiApoio.Texto(subtitulo, 12, FontWeights.Normal, (Brush)recursos["TextSecondary"]);
        var textos = UiApoio.Coluna(2, tituloLabel, subtituloLabel);
        textos.VerticalAlignment = VerticalAlignment.Center;

        var grade = new Grid { Height = 46 };
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(textos, 0);
        grade.Children.Add(textos);

        if (direita is not null)
        {
            direita.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(direita, 1);
            grade.Children.Add(direita);
        }

        return grade;
    }

    /// <summary>Estrutura comum das telas de tabela: título/ações no topo, barra de filtros, e o
    /// contêiner único da tabela (linhas, barra contextual de ações e resumo no rodapé).</summary>
    internal static UIElement TelaComTabela(
        string titulo, string descricao, FrameworkElement direitaCabecalho,
        FrameworkElement barraFiltro, DataGrid tabela, Border barraContextual, Border barraResumo)
    {
        var cabecalho = CabecalhoTela(titulo, descricao, direitaCabecalho);
        cabecalho.Margin = new Thickness(0, 0, 0, 14);
        var exportar = AcoesApoio.BotaoSecundario("Exportar CSV", () => ExportacaoCsv.Salvar(tabela, titulo));
        exportar.Height = 27;
        exportar.Padding = new Thickness(10, 0, 10, 0);
        exportar.Margin = new Thickness(12, 0, 0, 0);
        exportar.ToolTip = "Salva as linhas visíveis (com o filtro e a busca atuais) numa planilha CSV";
        var linhaFiltro = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        linhaFiltro.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        linhaFiltro.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(exportar, 1);
        linhaFiltro.Children.Add(barraFiltro);
        linhaFiltro.Children.Add(exportar);

        var corpoGrade = new Grid();
        corpoGrade.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        corpoGrade.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        corpoGrade.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(tabela, 0);
        Grid.SetRow(barraContextual, 1);
        Grid.SetRow(barraResumo, 2);
        corpoGrade.Children.Add(tabela);
        corpoGrade.Children.Add(barraContextual);
        corpoGrade.Children.Add(barraResumo);

        var moldura = new Border { Style = (Style)Application.Current.Resources["CardPanelStyle"], Child = corpoGrade };

        var raiz = new Grid { Margin = new Thickness(18, 16, 20, 18) };
        raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        raiz.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(cabecalho, 0);
        Grid.SetRow(linhaFiltro, 1);
        Grid.SetRow(moldura, 2);
        raiz.Children.Add(cabecalho);
        raiz.Children.Add(linhaFiltro);
        raiz.Children.Add(moldura);
        return raiz;
    }

    /// <summary>Busca (filtro em memória, <c>Ctrl+F</c> foca) e botão primário de criação, sempre à
    /// direita da primeira linha das telas de entidade.</summary>
    internal static (FrameworkElement Direita, TextBox Busca) CabecalhoBuscaCriacao(string placeholder, string textoNovo, Action acaoNovo)
    {
        var busca = new TextBox
        {
            Style = (Style)Application.Current.Resources["CampoTextoStyle"],
            Width = 252,
            Tag = placeholder,
            ToolTip = placeholder + " (Ctrl+F)"
        };
        var criar = AcoesApoio.BotaoPrimarioCriacao(textoNovo, acaoNovo);
        return (UiApoio.Linha(10, busca, criar), busca);
    }

    internal static Border BarraResumoTabela(TextBlock esquerda, TextBlock direita)
    {
        var recursos = Application.Current.Resources;
        esquerda.FontFamily = (FontFamily)recursos["FontMono"];
        esquerda.FontSize = 11;
        esquerda.Foreground = (Brush)recursos["TextLabel"];
        esquerda.VerticalAlignment = VerticalAlignment.Center;
        direita.FontSize = 11.5;
        direita.Foreground = (Brush)recursos["TextMuted"];
        direita.VerticalAlignment = VerticalAlignment.Center;

        var grade = new Grid();
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(esquerda, 0);
        Grid.SetColumn(direita, 1);
        grade.Children.Add(esquerda);
        grade.Children.Add(direita);

        return new Border
        {
            Background = (Brush)recursos["TableFooterBg"],
            BorderBrush = (Brush)recursos["CardBorder"],
            BorderThickness = new Thickness(0, 1, 0, 0),
            Height = 40,
            Padding = new Thickness(16, 0, 16, 0),
            Child = grade
        };
    }

    internal static DataGridLength Estrela(double fator) => new(fator, DataGridLengthUnitType.Star);

    /// <summary>Card simples de configuração (título + conteúdo livre) — usado pelo módulo de Backup.</summary>
    internal static Border CardConfiguracao(string titulo, UIElement conteudo)
    {
        var recursos = Application.Current.Resources;
        var tituloLabel = new TextBlock { Text = titulo, Style = (Style)recursos["PanelTituloStyle"] };
        var coluna = UiApoio.Coluna(12, tituloLabel, conteudo);
        return new Border { Style = (Style)recursos["CardPanelStyle"], Padding = new Thickness(18), Child = coluna };
    }

    /// <summary>Painel com cabeçalho (título + contador + elemento à direita) e corpo rolável — usado
    /// pelo Painel para "Alertas abertos" e "Prioridades".</summary>
    internal static Border PainelListaCard(string titulo, TextBlock contador, FrameworkElement direita, UIElement corpo)
    {
        var recursos = Application.Current.Resources;
        var tituloLabel = new TextBlock { Text = titulo, Style = (Style)recursos["PanelTituloStyle"] };
        contador.Style = (Style)recursos["PanelContadorTextoStyle"];
        var contadorBorda = new Border { Style = (Style)recursos["PanelContadorStyle"], Child = contador };
        var esquerda = UiApoio.Linha(8, tituloLabel, contadorBorda);

        var grade = new Grid();
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(esquerda, 0);
        Grid.SetColumn(direita, 1);
        grade.Children.Add(esquerda);
        grade.Children.Add(direita);
        var cabecalho = new Border { Style = (Style)recursos["PanelHeaderStyle"], Child = grade };

        var corpoRolavel = new ScrollViewer { Content = corpo, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

        var raiz = new Grid();
        raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        raiz.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(cabecalho, 0);
        Grid.SetRow(corpoRolavel, 1);
        raiz.Children.Add(cabecalho);
        raiz.Children.Add(corpoRolavel);

        return new Border { Style = (Style)recursos["CardPanelStyle"], Child = raiz };
    }

    /// <summary>Card de resumo (§4.1 da spec de UI): borda externa sempre neutra — só a barra de 3px
    /// carrega a cor semântica, para que a fileira não pareça três alertas diferentes. Também usado
    /// pela galeria do tema.</summary>
    internal static Border ResumoOperacao(string titulo, string valor, string detalhe, string prefixoStatus)
    {
        var recursos = Application.Current.Resources;
        var barra = new Border { Width = 3, Background = (Brush)recursos[prefixoStatus + "Border"] };

        var tituloLabel = new TextBlock { Text = titulo.ToUpperInvariant(), Style = (Style)recursos["RotuloKpiStyle"] };
        var valorLabel = new TextBlock
        {
            Text = valor,
            FontFamily = (FontFamily)recursos["FontUi"],
            FontSize = 27,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)recursos["TextTitle"],
            VerticalAlignment = VerticalAlignment.Bottom
        };
        var detalheLabel = new TextBlock
        {
            Text = detalhe,
            FontSize = 11.5,
            Foreground = (Brush)recursos[prefixoStatus + "Text"],
            VerticalAlignment = VerticalAlignment.Bottom
        };
        var linhaValor = UiApoio.Linha(9, valorLabel, detalheLabel);
        var conteudo = UiApoio.Coluna(6, tituloLabel, linhaValor);
        var conteudoBorda = new Border { Padding = new Thickness(16, 13, 16, 13), Child = conteudo };

        var grade = new Grid();
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(barra, 0);
        Grid.SetColumn(conteudoBorda, 1);
        grade.Children.Add(barra);
        grade.Children.Add(conteudoBorda);

        return new Border { Style = (Style)recursos["CardKpiStyle"], Padding = new Thickness(0), Height = 88, Child = grade };
    }

    /// <summary>Faixa única com os 6 indicadores menores (§4.2): substitui a antiga fileira de 6 cards
    /// separados. Também usado pela galeria do tema.</summary>
    internal static FrameworkElement FaixaIndicadores(params (string Rotulo, int Valor, bool Critico, string? PrefixoStatus)[] itens)
    {
        var recursos = Application.Current.Resources;
        var grade = new Grid();
        foreach (var _ in itens)
        {
            grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        for (var i = 0; i < itens.Length; i++)
        {
            var (rotulo, valor, critico, prefixoStatus) = itens[i];
            var tituloLabel = new TextBlock { Text = rotulo.ToUpperInvariant(), Style = (Style)recursos["RotuloKpiStyle"] };
            var valorLabel = new TextBlock
            {
                Text = valor.ToString(),
                FontFamily = (FontFamily)recursos["FontUi"],
                FontSize = 22,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)recursos[valor == 0 ? "TextMuted" : "TextTitle"],
                VerticalAlignment = VerticalAlignment.Center
            };

            UIElement linhaValor = valorLabel;
            if (critico && prefixoStatus is not null)
            {
                var ponto = new Border
                {
                    Width = 6,
                    Height = 6,
                    CornerRadius = new CornerRadius(3),
                    Background = (Brush)recursos[prefixoStatus + "Border"],
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 0, 0)
                };
                linhaValor = UiApoio.Linha(6, valorLabel, ponto);
            }

            var conteudo = UiApoio.Coluna(4, tituloLabel, linhaValor);
            var celula = new Border { Style = (Style)recursos["KpiStripCellStyle"], Child = conteudo };
            if (i == 0)
            {
                celula.BorderThickness = new Thickness(0);
            }

            Grid.SetColumn(celula, i);
            grade.Children.Add(celula);
        }

        return new Border { Style = (Style)recursos["CardPanelStyle"], Height = 78, Child = grade };
    }
}
