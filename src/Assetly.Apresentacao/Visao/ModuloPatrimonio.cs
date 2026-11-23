using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Assetly.Aplicacao.CasoDeUso;
using static Assetly.Apresentacao.Visao.ContextoApoio;
using static Assetly.Apresentacao.Visao.LayoutApoio;

namespace Assetly.Apresentacao.Visao;

/// <summary>
/// Painel patrimonial: quanto os bens em uso custaram, quanto valem hoje (estimativa por depreciação linear
/// conforme o tipo — ver <c>Depreciacao</c> no domínio), a distribuição por tipo e por responsável (modo
/// família), a evolução estimada ano a ano e os bens de maior valor. Valores em reais; bens em outra moeda
/// ficam fora dos totais e são informados no rodapé.
/// </summary>
internal sealed class ModuloPatrimonio : IModulo
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    private readonly ContextoAplicacao _contexto;
    private readonly Grid _kpis = new();
    private readonly ContentControl _porTipo = new();
    private readonly ContentControl _evolucao = new();
    private readonly ContentControl _porResponsavel = new();
    private readonly ContentControl _maiores = new();
    private readonly TextBlock _rodape = new() { TextWrapping = TextWrapping.Wrap };

    public string Titulo => "Patrimônio";
    public string Descricao => "Valor e depreciação";
    public bool ComContador => false;
    public UIElement Conteudo { get; }
    public int Contador => 0;

    internal ModuloPatrimonio(ContextoAplicacao contexto)
    {
        _contexto = contexto;
        var recursos = Application.Current.Resources;
        var cabecalho = CabecalhoTela("Patrimônio", "Quanto seus bens custaram e quanto valem hoje (estimativa por depreciação).", null);
        cabecalho.Margin = new Thickness(0, 0, 0, 14);

        for (var i = 0; i < 3; i++)
        {
            _kpis.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        _kpis.Margin = new Thickness(0, 0, 0, 14);

        var cardTipo = Card("Por tipo de bem", "valor estimado hoje · barra clara = valor de compra", _porTipo);
        var cardEvolucao = Card("Evolução estimada", "valor dos bens atuais no fim de cada ano", _evolucao);
        var cardResponsavel = Card("Por responsável", "modo família: bens atribuídos a cada pessoa", _porResponsavel);
        var cardMaiores = Card("Bens de maior valor", "estimativa hoje · % do valor de compra que resta", _maiores);

        var grade = new Grid();
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grade.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Posicionar(grade, cardTipo, 0, 0, new Thickness(0, 0, 6, 12));
        Posicionar(grade, cardEvolucao, 0, 1, new Thickness(6, 0, 0, 12));
        Posicionar(grade, cardMaiores, 1, 0, new Thickness(0, 0, 6, 0));
        Posicionar(grade, cardResponsavel, 1, 1, new Thickness(6, 0, 0, 0));

        _rodape.FontSize = 11.5;
        _rodape.Foreground = (Brush)recursos["TextMuted"];
        _rodape.Margin = new Thickness(2, 12, 2, 0);

        var pilha = new StackPanel();
        pilha.Children.Add(_kpis);
        pilha.Children.Add(grade);
        pilha.Children.Add(_rodape);

        var raiz = new DockPanel { Margin = new Thickness(18, 16, 20, 18) };
        DockPanel.SetDock(cabecalho, Dock.Top);
        raiz.Children.Add(cabecalho);
        raiz.Children.Add(new ScrollViewer { Content = pilha, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Focusable = false });
        Conteudo = raiz;
    }

    public void Atualizar()
    {
        var resumo = _contexto.CalcularPatrimonio().Executar(Hoje());
        _kpis.Children.Clear();
        var compra = ResumoOperacao("Valor de compra", Dinheiro(resumo.ValorCompra), $"{resumo.Itens.Count} bens em uso com preço", "StatusInfo");
        var atual = ResumoOperacao("Valor estimado hoje", Dinheiro(resumo.ValorAtual), resumo.ValorCompra == 0 ? "—" : $"{(1 - resumo.PercentualDepreciado):P0} do valor de compra", "StatusOk");
        var perda = ResumoOperacao("Depreciação acumulada", Dinheiro(resumo.Depreciacao), $"{resumo.PercentualDepreciado:P1} desde a compra", "StatusAviso");
        Posicionar(_kpis, compra, 0, 0, new Thickness(0, 0, 6, 0));
        Posicionar(_kpis, atual, 0, 1, new Thickness(6, 0, 6, 0));
        Posicionar(_kpis, perda, 0, 2, new Thickness(6, 0, 0, 0));

        _porTipo.Content = resumo.PorTipo.Count == 0
            ? Vazio("Cadastre bens com preço de compra para ver a distribuição.")
            : Barras(resumo.PorTipo.Select(p => p.Grupo with { Rotulo = Formatador.TextoEnum(p.Tipo) }).ToList());
        _evolucao.Content = resumo.Evolucao.Count == 0 ? Vazio("Sem dados suficientes.") : Colunas(resumo.Evolucao);

        var comResponsavel = resumo.PorResponsavel.Where(g => g.Quantidade > 0).ToList();
        _porResponsavel.Content = comResponsavel.All(g => g.Rotulo == "Sem responsável")
            ? Vazio("Nenhum bem atribuído a uma pessoa. Cadastre pessoas (tipo \"Pessoa\") em Bens e escolha o responsável de cada bem.")
            : Barras(comResponsavel);

        _maiores.Content = resumo.Itens.Count == 0 ? Vazio("Nenhum bem com preço de compra.") : Maiores(resumo.Itens);

        var observacoes = new List<string>
        {
            "Estimativa por depreciação linear: eletrônicos 20% ao ano, veículos 12%, demais 10%, com um valor residual mínimo; imóveis mantêm o valor de compra."
        };
        if (resumo.SemPreco > 0)
        {
            observacoes.Add($"{resumo.SemPreco} bem(ns) sem preço de compra não entram nos totais.");
        }

        if (resumo.SemDataCompra > 0)
        {
            observacoes.Add($"{resumo.SemDataCompra} bem(ns) sem data de compra são contados pelo preço cheio.");
        }

        if (resumo.EmOutraMoeda > 0)
        {
            observacoes.Add($"{resumo.EmOutraMoeda} bem(ns) com preço em outra moeda ficam fora dos totais em reais.");
        }

        _rodape.Text = string.Join("  ", observacoes);
    }

    private static string Dinheiro(decimal valor) => valor.ToString("C0", Cultura);

    private static string Compacto(decimal valor) => valor switch
    {
        >= 1_000_000 => (valor / 1_000_000m).ToString("0.#", Cultura) + " mi",
        >= 1_000 => (valor / 1_000m).ToString("0.#", Cultura) + " mil",
        _ => valor.ToString("0", Cultura)
    };

    private static void Posicionar(Grid grade, UIElement elemento, int linha, int coluna, Thickness margem)
    {
        if (elemento is FrameworkElement fe)
        {
            fe.Margin = margem;
        }

        Grid.SetRow(elemento, linha);
        Grid.SetColumn(elemento, coluna);
        grade.Children.Add(elemento);
    }

    private static Border Card(string titulo, string subtitulo, UIElement corpo)
    {
        var recursos = Application.Current.Resources;
        var tituloLabel = new TextBlock { Text = titulo, Style = (Style)recursos["PanelTituloStyle"] };
        var sub = UiApoio.Texto(subtitulo, 11, FontWeights.Normal, (Brush)recursos["TextMuted"]);
        var coluna = new StackPanel();
        coluna.Children.Add(tituloLabel);
        sub.Margin = new Thickness(0, 2, 0, 14);
        coluna.Children.Add(sub);
        coluna.Children.Add(corpo);
        return new Border { Style = (Style)recursos["CardPanelStyle"], Padding = new Thickness(18, 16, 18, 18), Child = coluna, MinHeight = 250 };
    }

    private static TextBlock Vazio(string texto) =>
        new() { Text = texto, TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = (Brush)Application.Current.Resources["TextMuted"], Margin = new Thickness(0, 8, 0, 0) };

    /// <summary>Barras horizontais de série única: trilho claro = valor de compra, barra = valor hoje.</summary>
    private static UIElement Barras(IReadOnlyList<GrupoPatrimonio> grupos)
    {
        var recursos = Application.Current.Resources;
        var maximo = Math.Max(1m, grupos.Max(g => g.ValorCompra));
        var destaque = (Brush)recursos["AccentPrimary"];
        var trilho = (Brush)recursos["AccentPrimaryHalo"];
        var grade = new Grid();
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
        var linha = 0;
        foreach (var grupo in grupos.OrderByDescending(g => g.ValorAtual))
        {
            grade.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
            var rotulo = UiApoio.Texto($"{grupo.Rotulo}  ·  {grupo.Quantidade}", 12, FontWeights.Normal, (Brush)recursos["TextSecondary"]);
            rotulo.VerticalAlignment = VerticalAlignment.Center;
            rotulo.TextTrimming = TextTrimming.CharacterEllipsis;

            var area = new Grid { Height = 12, VerticalAlignment = VerticalAlignment.Center };
            area.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength((double)(grupo.ValorCompra / maximo), GridUnitType.Star) });
            area.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength((double)(1 - grupo.ValorCompra / maximo), GridUnitType.Star) });
            var trilhoBorda = new Border { Background = trilho, CornerRadius = new CornerRadius(0, 4, 4, 0) };
            var proporcao = grupo.ValorCompra == 0 ? 0 : (double)(grupo.ValorAtual / grupo.ValorCompra);
            var barraInterna = new Grid();
            barraInterna.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(proporcao, GridUnitType.Star) });
            barraInterna.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1 - proporcao, GridUnitType.Star) });
            barraInterna.Children.Add(new Border { Background = destaque, CornerRadius = new CornerRadius(0, 4, 4, 0) });
            trilhoBorda.Child = barraInterna;
            area.Children.Add(trilhoBorda);
            area.Background = Brushes.Transparent;
            area.ToolTip = $"{grupo.Rotulo}: {grupo.Quantidade} bem(ns)\nCompra: {Dinheiro(grupo.ValorCompra)}\nHoje: {Dinheiro(grupo.ValorAtual)} ({proporcao:P0})";

            var valor = UiApoio.Texto(Dinheiro(grupo.ValorAtual), 12, FontWeights.SemiBold, (Brush)recursos["TextTitle"]);
            valor.HorizontalAlignment = HorizontalAlignment.Right;
            valor.VerticalAlignment = VerticalAlignment.Center;

            Grid.SetRow(rotulo, linha);
            Grid.SetRow(area, linha);
            Grid.SetColumn(area, 1);
            Grid.SetRow(valor, linha);
            Grid.SetColumn(valor, 2);
            grade.Children.Add(rotulo);
            grade.Children.Add(area);
            grade.Children.Add(valor);
            linha++;
        }

        return grade;
    }

    /// <summary>Colunas de série única, uma por ano; rótulo de valor só na primeira e na última.</summary>
    private static UIElement Colunas(IReadOnlyList<PontoEvolucao> pontos)
    {
        var recursos = Application.Current.Resources;
        const double alturaArea = 150;
        var maximo = Math.Max(1m, pontos.Max(p => p.Valor));
        var grade = new Grid();
        grade.RowDefinitions.Add(new RowDefinition { Height = new GridLength(18) });
        grade.RowDefinitions.Add(new RowDefinition { Height = new GridLength(alturaArea) });
        grade.RowDefinitions.Add(new RowDefinition { Height = new GridLength(22) });
        var linhaBase = new Border { BorderBrush = (Brush)recursos["CardBorder"], BorderThickness = new Thickness(0, 0, 0, 1) };
        Grid.SetRow(linhaBase, 1);
        Grid.SetColumnSpan(linhaBase, Math.Max(1, pontos.Count));
        for (var i = 0; i < pontos.Count; i++)
        {
            var ponto = pontos[i];
            grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var altura = Math.Max(2, alturaArea * (double)(ponto.Valor / maximo));
            var coluna = new Border
            {
                Width = 26,
                Height = altura,
                VerticalAlignment = VerticalAlignment.Bottom,
                CornerRadius = new CornerRadius(4, 4, 0, 0),
                Background = (Brush)recursos[ponto.Ano == Hoje().Year ? "AccentPrimary" : "AccentPrimaryHalo"],
                BorderBrush = (Brush)recursos["AccentPrimary"],
                BorderThickness = new Thickness(ponto.Ano == Hoje().Year ? 0 : 1, ponto.Ano == Hoje().Year ? 0 : 1, ponto.Ano == Hoje().Year ? 0 : 1, 0),
                ToolTip = ponto.Ano == Hoje().Year ? $"Hoje: {Dinheiro(ponto.Valor)}" : $"Fim de {ponto.Ano}: {Dinheiro(ponto.Valor)}"
            };
            Grid.SetRow(coluna, 1);
            Grid.SetColumn(coluna, i);
            grade.Children.Add(coluna);

            if (i == 0 || i == pontos.Count - 1 || ponto.Ano == Hoje().Year)
            {
                var valor = UiApoio.Texto(Compacto(ponto.Valor), 11, FontWeights.SemiBold, (Brush)recursos["TextSecondary"]);
                valor.HorizontalAlignment = HorizontalAlignment.Center;
                valor.VerticalAlignment = VerticalAlignment.Bottom;
                valor.Margin = new Thickness(0, 0, 0, altura + 4);
                Grid.SetRow(valor, 0);
                Grid.SetRowSpan(valor, 2);
                Grid.SetColumn(valor, i);
                grade.Children.Add(valor);
            }

            var ano = UiApoio.Texto(ponto.Ano.ToString(CultureInfo.InvariantCulture), 11, ponto.Ano == Hoje().Year ? FontWeights.SemiBold : FontWeights.Normal,
                (Brush)recursos[ponto.Ano == Hoje().Year ? "TextTitle" : "TextMuted"]);
            ano.HorizontalAlignment = HorizontalAlignment.Center;
            ano.VerticalAlignment = VerticalAlignment.Bottom;
            Grid.SetRow(ano, 2);
            Grid.SetColumn(ano, i);
            grade.Children.Add(ano);
        }

        grade.Children.Insert(0, linhaBase);
        return grade;
    }

    private static UIElement Maiores(IReadOnlyList<ItemPatrimonio> itens)
    {
        var recursos = Application.Current.Resources;
        var grade = new Grid();
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
        var linha = 0;
        foreach (var item in itens.OrderByDescending(i => i.ValorAtual).Take(7))
        {
            grade.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
            var nome = UiApoio.Texto(item.Bem.Nome.Valor, 12.5, FontWeights.Normal, (Brush)recursos["TextBody"]);
            nome.TextTrimming = TextTrimming.CharacterEllipsis;
            nome.VerticalAlignment = VerticalAlignment.Center;
            nome.ToolTip = $"{item.Bem.Nome.Valor}\nCompra: {Dinheiro(item.ValorCompra)}\nHoje: {Dinheiro(item.ValorAtual)}";
            var valor = UiApoio.Texto(Dinheiro(item.ValorAtual), 12.5, FontWeights.SemiBold, (Brush)recursos["TextTitle"]);
            valor.HorizontalAlignment = HorizontalAlignment.Right;
            valor.VerticalAlignment = VerticalAlignment.Center;
            var percentual = UiApoio.Texto(item.PercentualAtual.ToString("P0", Cultura), 11.5, FontWeights.Normal, (Brush)recursos["TextMuted"]);
            percentual.HorizontalAlignment = HorizontalAlignment.Right;
            percentual.VerticalAlignment = VerticalAlignment.Center;
            var separador = new Border { BorderBrush = (Brush)recursos["CardBorder"], BorderThickness = new Thickness(0, linha == 0 ? 0 : 1, 0, 0) };
            Grid.SetRow(separador, linha);
            Grid.SetColumnSpan(separador, 3);
            Grid.SetRow(nome, linha);
            Grid.SetRow(valor, linha);
            Grid.SetColumn(valor, 1);
            Grid.SetRow(percentual, linha);
            Grid.SetColumn(percentual, 2);
            grade.Children.Add(separador);
            grade.Children.Add(nome);
            grade.Children.Add(valor);
            grade.Children.Add(percentual);
            linha++;
        }

        return grade;
    }
}
