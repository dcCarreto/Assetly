using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace Assetly.Apresentacao.Visao;

/// <summary>Uma página dentro de um <see cref="ModuloComAbas"/> — mesma ideia de <see cref="IModulo"/>,
/// mas sem entrada própria na barra lateral.</summary>
internal interface IPagina
{
    string Titulo { get; }

    UIElement Conteudo { get; }

    void Atualizar();

    bool Localizar(object chave) => false;
}

/// <summary>
/// Módulo da barra lateral que agrupa várias páginas em abas (ex.: Automóveis = Veículos, Abastecimentos
/// e Despesas). Evita encher a barra lateral com uma entrada por tabela, mantendo tudo de um assunto junto.
/// </summary>
internal sealed class ModuloComAbas : IModulo
{
    private readonly IReadOnlyList<IPagina> _paginas;
    private readonly List<ToggleButton> _abas = [];
    private readonly ContentControl _area = new();
    private readonly Func<int> _contador;
    private int _selecionada;
    private readonly Border _faixa;
    private readonly Border _faixaNoTopo = new() { Margin = new Thickness(18, 8, 20, 0) };

    public string Titulo { get; }
    public string Descricao { get; }
    public bool ComContador => true;
    public UIElement Conteudo { get; }
    public int Contador => _contador();

    internal ModuloComAbas(string titulo, string descricao, Func<int> contador, params IPagina[] paginas)
    {
        Titulo = titulo;
        Descricao = descricao;
        _contador = contador;
        _paginas = paginas;

        var barra = new StackPanel { Orientation = Orientation.Horizontal };
        for (var i = 0; i < paginas.Length; i++)
        {
            var indice = i;
            var aba = new ToggleButton { Style = (Style)Application.Current.Resources["AbaStyle"], Content = paginas[i].Titulo, IsChecked = i == 0 };
            AutomationProperties.SetName(aba, $"Aba {paginas[i].Titulo}");
            aba.Click += (_, _) => Selecionar(indice);
            _abas.Add(aba);
            barra.Children.Add(aba);
        }

        _faixa = new Border
        {
            BorderBrush = (Brush)Application.Current.Resources["CardBorder"],
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = barra
        };
        _faixaNoTopo.Child = null;
        DockPanel.SetDock(_faixaNoTopo, Dock.Top);

        var raiz = new DockPanel { Focusable = true, FocusVisualStyle = null };
        raiz.Children.Add(_faixaNoTopo);
        raiz.Children.Add(_area);
        // A tela principal foca o conteúdo do módulo ao trocar de módulo; repassa o foco para a página
        // visível para que os atalhos dela (Ctrl+N, F2, Delete...) funcionem de imediato.
        raiz.GotKeyboardFocus += (_, e) =>
        {
            if (ReferenceEquals(e.NewFocus, raiz))
            {
                FocarPagina();
            }
        };
        Conteudo = raiz;
        MostrarAbasNaPagina(paginas[0]);
        _area.Content = paginas[0].Conteudo;
    }

    public void Atualizar()
    {
        foreach (var pagina in _paginas)
        {
            pagina.Atualizar();
        }
    }

    public bool Localizar(object chave)
    {
        for (var i = 0; i < _paginas.Count; i++)
        {
            if (_paginas[i].Localizar(chave))
            {
                Selecionar(i);
                return true;
            }
        }

        return false;
    }

    internal void MostrarPagina(IPagina pagina)
    {
        var indice = _paginas.ToList().IndexOf(pagina);
        if (indice >= 0)
        {
            Selecionar(indice);
        }
    }

    internal int QuantidadeAbas => _paginas.Count;

    internal void Selecionar(int indice)
    {
        _selecionada = indice;
        for (var i = 0; i < _abas.Count; i++)
        {
            _abas[i].IsChecked = i == indice;
        }

        MostrarAbasNaPagina(_paginas[indice]);
        _area.Content = _paginas[indice].Conteudo;
        FocarPagina();
    }

    /// <summary>As abas vão para logo abaixo do cabeçalho da página (como nas outras telas, o título não desce); uma página que
    /// não tem esse lugar as recebe no topo, como antes.</summary>
    private void MostrarAbasNaPagina(IPagina pagina)
    {
        if (_faixa.Parent is ContentControl anterior)
        {
            anterior.Content = null;
        }
        else if (_faixa.Parent is Border borda)
        {
            borda.Child = null;
        }

        if (LayoutApoio.LugarDasAbas(pagina.Conteudo) is { } lugar)
        {
            _faixa.Margin = new Thickness(0, 0, 0, 12);
            lugar.Content = _faixa;
        }
        else
        {
            _faixa.Margin = new Thickness(0);
            _faixaNoTopo.Child = _faixa;
        }
    }

    private void FocarPagina()
    {
        var conteudo = _paginas[_selecionada].Conteudo;
        if (conteudo.Focusable)
        {
            conteudo.Dispatcher.BeginInvoke(DispatcherPriority.Input, () => conteudo.Focus());
        }
    }
}
