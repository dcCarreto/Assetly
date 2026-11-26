using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Assetly.Apresentacao.Visao;

/// <summary>Uma opção de chip da barra de filtros — rótulo e o predicado que decide quais linhas ela inclui.</summary>
internal sealed record OpcaoFiltro<T>(string Rotulo, Func<T, bool> Incluir);

/// <summary>
/// Barra de filtros por chip + busca em memória + contagem "N de M registros", reaproveitada pelas
/// telas de tabela. Mantém a lista mestre (todos os registros já formatados em linha) e recalcula a
/// lista filtrada da tabela sempre que o filtro, a busca ou os dados mudam. O primeiro item de
/// <paramref name="opcoes"/> é sempre o selecionado por padrão.
/// </summary>
internal sealed class FiltroTabela<T> where T : class
{
    private readonly DataGrid _tabela;
    private readonly Func<T, object> _chave;
    private readonly Func<T, string> _textoBusca;
    private readonly IReadOnlyList<OpcaoFiltro<T>> _opcoes;
    private readonly List<ToggleButton> _chips = [];
    private readonly TextBlock _contagem;
    private IReadOnlyList<T> _mestre = [];
    private int _filtroSelecionado;
    private string _busca = string.Empty;
    private TextBox? _caixaBusca;

    internal FrameworkElement Barra { get; }

    /// <summary>Depois de cada filtragem, com as linhas que ficaram visíveis.</summary>
    internal event Action<IReadOnlyList<T>>? Aplicado;

    internal FiltroTabela(DataGrid tabela, Func<T, object> chave, Func<T, string> textoBusca, IReadOnlyList<OpcaoFiltro<T>> opcoes)
    {
        _tabela = tabela;
        _chave = chave;
        _textoBusca = textoBusca;
        _opcoes = opcoes;

        var recursos = Application.Current.Resources;
        var chipsPainel = new WrapPanel();
        for (var i = 0; i < opcoes.Count; i++)
        {
            var indice = i;
            var chip = new ToggleButton
            {
                Style = (Style)recursos["ChipFilterStyle"],
                Content = opcoes[i].Rotulo,
                Margin = new Thickness(0, 0, 8, 0),
                IsChecked = i == 0
            };
            chip.Click += (_, _) =>
            {
                if (chip.IsChecked != true)
                {
                    chip.IsChecked = true;
                    return;
                }

                for (var j = 0; j < _chips.Count; j++)
                {
                    if (j != indice)
                    {
                        _chips[j].IsChecked = false;
                    }
                }

                _filtroSelecionado = indice;
                Aplicar();
            };
            _chips.Add(chip);
            chipsPainel.Children.Add(chip);
        }

        _contagem = new TextBlock
        {
            Style = (Style)recursos["CelulaMonoStyle"],
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)recursos["TextMuted"]
        };

        var grade = new Grid { Height = 27 };
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(chipsPainel, 0);
        Grid.SetColumn(_contagem, 1);
        grade.Children.Add(chipsPainel);
        grade.Children.Add(_contagem);
        Barra = grade;
    }

    /// <summary>Liga a caixa de busca da tela ao filtro (e permite que <see cref="Localizar"/> a limpe).</summary>
    internal void ConectarBusca(TextBox busca)
    {
        _caixaBusca = busca;
        busca.TextChanged += (_, _) => DefinirBusca(busca.Text);
    }

    /// <summary>Mostra e seleciona o registro com esta chave (vindo da busca geral): escolhe um filtro que o mostre e limpa a busca,
    /// para ele não ficar escondido. Falso se o registro não está nesta tabela.</summary>
    internal bool Localizar(object chave)
    {
        var item = _mestre.FirstOrDefault(linha => Equals(_chave(linha), chave));
        if (item is null)
        {
            return false;
        }

        // O primeiro filtro que mostra o registro (normalmente "Todos"; em Alertas, o primeiro pode esconder os resolvidos).
        var filtro = Math.Max(0, _opcoes.ToList().FindIndex(opcao => opcao.Incluir(item)));
        for (var i = 0; i < _chips.Count; i++)
        {
            _chips[i].IsChecked = i == filtro;
        }

        _filtroSelecionado = filtro;
        if (_caixaBusca is not null)
        {
            _caixaBusca.Text = string.Empty;
        }

        _busca = string.Empty;
        Aplicar();
        var visivel = _tabela.Items.Cast<object>().FirstOrDefault(linha => linha is T t && Equals(_chave(t), chave));
        _tabela.SelectedItem = visivel;
        if (visivel is not null)
        {
            _tabela.ScrollIntoView(visivel);
        }

        return true;
    }

    internal void DefinirBusca(string texto)
    {
        _busca = texto.Trim();
        Aplicar();
    }

    internal void AtualizarDados(IReadOnlyList<T> itens)
    {
        _mestre = itens;
        for (var i = 0; i < _opcoes.Count; i++)
        {
            _chips[i].Content = $"{_opcoes[i].Rotulo} {_mestre.Count(_opcoes[i].Incluir)}";
        }

        Aplicar();
    }

    private void Aplicar()
    {
        IEnumerable<T> filtrados = _mestre.Where(_opcoes[_filtroSelecionado].Incluir);
        if (!string.IsNullOrEmpty(_busca))
        {
            filtrados = filtrados.Where(item => _textoBusca(item).Contains(_busca, StringComparison.OrdinalIgnoreCase));
        }

        var lista = filtrados.ToList();
        TabelaApoio.RecarregarTabela(_tabela, lista, _chave);
        _contagem.Text = $"{lista.Count} de {_mestre.Count} registros";
        Aplicado?.Invoke(lista);
    }
}
