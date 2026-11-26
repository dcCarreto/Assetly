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

        var faixa = new Border
        {
            BorderBrush = (Brush)Application.Current.Resources["CardBorder"],
            BorderThickness = new Thickness(0, 0, 0, 1),
            Margin = new Thickness(18, 8, 20, 0),
            Child = barra
        };
        DockPanel.SetDock(faixa, Dock.Top);

        var raiz = new DockPanel { Focusable = true, FocusVisualStyle = null };
        raiz.Children.Add(faixa);
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

        _area.Content = _paginas[indice].Conteudo;
        FocarPagina();
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
