using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Manutencao;
using Assetly.Dominio.Saude;

namespace Assetly.Apresentacao.Visao;

internal sealed partial class TelaPrincipal
{
    private readonly ContextoAplicacao _contexto;
    private readonly Grid _raizComScrim = new();
    private readonly Border _scrim = new();
    private readonly ContentControl _areaConteudo = new();
    private readonly DataGrid _tabelaBens = new();
    private readonly DataGrid _tabelaGarantias = new();
    private readonly DataGrid _tabelaManutencoes = new();
    private readonly DataGrid _tabelaDocumentos = new();
    private readonly DataGrid _tabelaAlertas = new();
    private readonly DataGrid _tabelaSaude = new();
    private readonly DataGrid _tabelaGastosSaude = new();
    private readonly Grid _painelOperacao = new();
    private readonly StackPanel _painelAlertas = new();
    private readonly StackPanel _painelAcoes = new();
    private readonly ContentControl _faixaIndicadoresSlot = new();
    private readonly TextBlock _contadorAlertasPainel = new();
    private readonly TextBlock _contadorPrioridadesPainel = new();
    private readonly TextBlock _horarioVerificacao = new();
    private readonly List<ToggleButton> _botoesNavegacao = [];
    private readonly Dictionary<string, ToggleButton> _botoesPorTitulo = [];
    private readonly Dictionary<string, UIElement> _conteudoPorTitulo = [];
    private readonly Dictionary<string, TextBlock> _contadoresNavegacao = [];
    private FiltroTabela<BemLinha>? _filtroBens;
    private FiltroTabela<GarantiaLinha>? _filtroGarantias;
    private FiltroTabela<ManutencaoLinha>? _filtroManutencoes;
    private FiltroTabela<DocumentoLinha>? _filtroDocumentos;
    private FiltroTabela<AlertaLinha>? _filtroAlertas;
    private FiltroTabela<SaudeLinha>? _filtroSaude;
    private FiltroTabela<GastoSaudeLinha>? _filtroGastosSaude;
    private int _dialogosAbertos;
    private DispatcherTimer? _atualizacaoTempoReal;

    internal UIElement Raiz => _raizComScrim;

    internal UIElement SeloAmbiente { get; }

    internal TelaPrincipal(ContextoAplicacao contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto), "contexto da aplicação é obrigatório");
        SeloAmbiente = CriarSeloAmbiente();
        JanelaEscura.AlternarScrim = visivel =>
        {
            _dialogosAbertos += visivel ? 1 : -1;
            _scrim.Visibility = _dialogosAbertos > 0 ? Visibility.Visible : Visibility.Collapsed;
        };
        ConfigurarRaiz();
        ConfigurarTabelas();
        AtualizarTudo();
        IniciarAtualizacaoTempoReal();
    }

    internal void AtualizarTudo()
    {
        _contexto.GerarAlertasAutomaticos().Executar(Hoje());
        var bens = _contexto.ListarBens().Executar();
        _filtroBens!.AtualizarDados(ConstruirLinhasBens(bens));
        _filtroGarantias!.AtualizarDados(ConstruirLinhasGarantias(_contexto.ListarGarantias().Executar()));
        _filtroManutencoes!.AtualizarDados(ConstruirLinhasManutencoes(_contexto.ListarManutencoes().Executar()));
        _filtroDocumentos!.AtualizarDados(ConstruirLinhasDocumentos(_contexto.ListarDocumentos().Executar()));
        _filtroAlertas!.AtualizarDados(ConstruirLinhasAlertas(_contexto.ListarAlertas().Executar()));
        _filtroSaude!.AtualizarDados(ConstruirLinhasSaude(_contexto.ListarSaude().Executar()));
        _filtroGastosSaude!.AtualizarDados(ConstruirLinhasGastosSaude(_contexto.ListarGastosSaude().Executar()));
        AtualizarPainel();
        AtualizarContadoresNavegacao(bens.Count);
    }

    private static DateOnly Hoje() => DateOnly.FromDateTime(DateTime.Now);

    private void IniciarAtualizacaoTempoReal()
    {
        _atualizacaoTempoReal = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _atualizacaoTempoReal.Tick += (_, _) => ExecutarComTratamento(AtualizarTudo);
        _atualizacaoTempoReal.Start();
    }

    private void ConfigurarRaiz()
    {
        var estrutura = new DockPanel();
        var navegacao = NavegacaoLateral();
        DockPanel.SetDock(navegacao, Dock.Left);
        estrutura.Children.Add(navegacao);
        estrutura.Children.Add(_areaConteudo);

        _scrim.Background = (Brush)Application.Current.Resources["DialogScrim"];
        _scrim.Visibility = Visibility.Collapsed;
        _scrim.IsHitTestVisible = false;

        _raizComScrim.Children.Add(estrutura);
        _raizComScrim.Children.Add(_scrim);
    }

    private Border CriarSeloAmbiente()
    {
        var recursos = Application.Current.Resources;
        var prefixo = _contexto.AmbienteTeste ? "StatusAviso" : "StatusOk";

        var ponto = new Border
        {
            Width = 5,
            Height = 5,
            CornerRadius = new CornerRadius(2.5),
            Background = (Brush)recursos[prefixo + "Border"],
            VerticalAlignment = VerticalAlignment.Center
        };
        var texto = new TextBlock
        {
            Text = _contexto.RotuloAmbiente.ToUpperInvariant(),
            FontFamily = (FontFamily)recursos["FontMono"],
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)recursos[prefixo + "Text"],
            VerticalAlignment = VerticalAlignment.Center
        };

        return new Border { Child = UiApoio.Linha(6, ponto, texto), VerticalAlignment = VerticalAlignment.Center };
    }

    private UIElement NavegacaoLateral()
    {
        var recursos = Application.Current.Resources;
        var itens = new List<ItemNavegacao>
        {
            new("Painel", "Resumo operacional", CriarPainel(), ComContador: false),
            new("Bens", "Inventário e status", CriarAbaBens()),
            new("Garantias", "Cobertura e vencimentos", CriarAbaGarantias()),
            new("Manutenções", "Agenda e custos", CriarAbaManutencoes()),
            new("Documentos", "Arquivos vinculados", CriarAbaDocumentos()),
            new("Alertas", "Pendências abertas", CriarAbaAlertas()),
            new("Convênios", "Planos de saúde", CriarAbaSaude()),
            new("Gastos de saúde", "Farmácia e despesas", CriarAbaGastosSaude())
        };

        var titulo = new TextBlock
        {
            Text = "MÓDULOS",
            FontFamily = (FontFamily)recursos["FontMono"],
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)recursos["TextLabel"],
            Margin = new Thickness(6, 4, 6, 8)
        };

        var topo = new StackPanel();
        topo.Children.Add(titulo);
        foreach (var item in itens)
        {
            var botao = BotaoNavegacao(item);
            _botoesNavegacao.Add(botao);
            _botoesPorTitulo[item.Titulo] = botao;
            _conteudoPorTitulo[item.Titulo] = item.Conteudo;
            topo.Children.Add(botao);
        }

        if (_botoesNavegacao.Count > 0)
        {
            _botoesNavegacao[0].IsChecked = true;
            SelecionarConteudo(itens[0].Conteudo);
        }

        DockPanel.SetDock(topo, Dock.Top);

        var rodape = new Border
        {
            BorderBrush = (Brush)recursos["BgSurface2"],
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 12, 16, 12),
            Child = UiApoio.LinhaComCrescimento(8,
                new Border { Width = 6, Height = 6, CornerRadius = new CornerRadius(3), Background = (Brush)recursos["StatusOkBorder"], VerticalAlignment = VerticalAlignment.Center },
                UiApoio.Coluna(1,
                    UiApoio.Texto("Base local SQLite", 11.5, FontWeights.Normal, (Brush)recursos["TextSecondary"]),
                    UiApoio.Texto("Dados privados no dispositivo", 10.5, FontWeights.Normal, (Brush)recursos["TextMuted"])))
        };
        DockPanel.SetDock(rodape, Dock.Bottom);

        var menu = new DockPanel { Background = (Brush)recursos["BgSidebar"], Width = 208, LastChildFill = false };
        menu.Children.Add(topo);
        menu.Children.Add(rodape);

        return new Border
        {
            BorderBrush = (Brush)recursos["CardBorder"],
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(10, 14, 10, 0),
            Child = menu
        };
    }

    private ToggleButton BotaoNavegacao(ItemNavegacao item)
    {
        var recursos = Application.Current.Resources;
        var icone = new Border
        {
            Width = 11,
            Height = 11,
            CornerRadius = new CornerRadius(3),
            Background = (Brush)recursos["SidebarIconNormal"],
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };

        var titulo = new TextBlock { Text = item.Titulo, Style = (Style)recursos["ItemMenuLateralTextoStyle"] };
        var descricao = UiApoio.Texto(item.Descricao, 11, FontWeights.Normal, (Brush)recursos["TextMuted"]);
        var textos = UiApoio.Coluna(2, titulo, descricao);
        textos.VerticalAlignment = VerticalAlignment.Center;

        var grade = new Grid();
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(icone, 0);
        Grid.SetColumn(textos, 1);
        grade.Children.Add(icone);
        grade.Children.Add(textos);

        TextBlock? contador = null;
        if (item.ComContador)
        {
            contador = new TextBlock
            {
                Text = "0",
                FontFamily = (FontFamily)recursos["FontMono"],
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)recursos["TextMuted"],
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(contador, 2);
            grade.Children.Add(contador);
            _contadoresNavegacao[item.Titulo] = contador;
        }

        var botao = new ToggleButton
        {
            Style = (Style)recursos["SidebarItemStyle"],
            Content = grade,
            Margin = new Thickness(0, 0, 0, 2),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        void AtualizarEstadoVisual()
        {
            var selecionado = botao.IsChecked == true;
            icone.Background = (Brush)recursos[selecionado ? "SidebarIconSelected" : "SidebarIconNormal"];
            titulo.Foreground = (Brush)recursos[selecionado ? "TextTitle" : "TextSecondary"];
            titulo.FontWeight = selecionado ? FontWeights.SemiBold : FontWeights.Normal;
            if (contador is not null && contador.Tag as string != "alerta-pendente")
            {
                contador.Foreground = (Brush)recursos[selecionado ? "TextSecondary" : "TextMuted"];
            }
        }

        botao.Checked += (_, _) => AtualizarEstadoVisual();
        botao.Unchecked += (_, _) => AtualizarEstadoVisual();
        AtualizarEstadoVisual();

        botao.Click += (_, _) =>
        {
            if (botao.IsChecked != true)
            {
                botao.IsChecked = true;
                return;
            }

            SelecionarPorBotao(botao, item.Conteudo);
        };
        return botao;
    }

    private void SelecionarPorBotao(ToggleButton botao, UIElement conteudo)
    {
        foreach (var outro in _botoesNavegacao)
        {
            if (!ReferenceEquals(outro, botao))
            {
                outro.IsChecked = false;
            }
        }

        SelecionarConteudo(conteudo);
    }

    private void IrParaAba(string titulo)
    {
        if (!_botoesPorTitulo.TryGetValue(titulo, out var botao) || botao.IsChecked == true)
        {
            return;
        }

        botao.IsChecked = true;
        SelecionarPorBotao(botao, _conteudoPorTitulo[titulo]);
    }

    private void AtualizarContadoresNavegacao(int totalBens)
    {
        foreach (var (titulo, contagem) in new (string Titulo, int Contagem)[]
        {
            ("Bens", totalBens),
            ("Garantias", _contexto.ListarGarantias().Executar().Count),
            ("Manutenções", _contexto.ListarManutencoes().Executar().Count),
            ("Documentos", _contexto.ListarDocumentos().Executar().Count),
            ("Alertas", _contexto.ListarAlertas().Executar().Count(a => a.Status != StatusAlerta.Resolvido)),
            ("Convênios", _contexto.ListarSaude().Executar().Count),
            ("Gastos de saúde", _contexto.ListarGastosSaude().Executar().Count)
        })
        {
            if (!_contadoresNavegacao.TryGetValue(titulo, out var contador))
            {
                continue;
            }

            contador.Text = contagem.ToString();
            var alertaPendente = titulo == "Alertas" && contagem > 0;
            contador.Tag = alertaPendente ? "alerta-pendente" : null;
            if (alertaPendente)
            {
                contador.Foreground = (Brush)Application.Current.Resources["StatusAvisoText"];
            }
            else
            {
                var selecionado = _botoesPorTitulo.TryGetValue(titulo, out var botao) && botao.IsChecked == true;
                contador.Foreground = (Brush)Application.Current.Resources[selecionado ? "TextSecondary" : "TextMuted"];
            }
        }
    }

    private void SelecionarConteudo(UIElement conteudo)
    {
        _areaConteudo.Content = conteudo;
    }

    /// <summary>Primeira linha de toda tela: título + subtítulo à esquerda, conteúdo específico da tela
    /// (busca + criação, ou o resumo do Painel) à direita.</summary>
    private static FrameworkElement CabecalhoTela(string titulo, string subtitulo, FrameworkElement? direita)
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

    private UIElement CriarPainel()
    {
        _horarioVerificacao.FontFamily = (FontFamily)Application.Current.Resources["FontMono"];
        _horarioVerificacao.FontSize = 11;
        _horarioVerificacao.Foreground = (Brush)Application.Current.Resources["TextMuted"];
        _horarioVerificacao.VerticalAlignment = VerticalAlignment.Center;
        var reprocessar = BotaoSecundario("Reprocessar alertas", AtualizarTudo);
        var direitaCabecalho = UiApoio.Linha(12, _horarioVerificacao, reprocessar);
        var cabecalho = CabecalhoTela("Visão geral", "Indicadores críticos para acompanhar a saúde dos ativos.", direitaCabecalho);
        cabecalho.Margin = new Thickness(0, 0, 0, 14);

        for (var i = 0; i < 3; i++)
        {
            _painelOperacao.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        _painelOperacao.Margin = new Thickness(0, 0, 0, 14);
        _faixaIndicadoresSlot.Margin = new Thickness(0, 0, 0, 14);

        var linkVerTodos = new TextBlock { Text = "Ver todos", Style = (Style)Application.Current.Resources["PanelLinkStyle"] };
        linkVerTodos.MouseLeftButtonUp += (_, _) => IrParaAba("Alertas");
        var painelAlertasBorda = PainelListaCard("Alertas abertos", _contadorAlertasPainel, linkVerTodos, _painelAlertas);

        var rotuloPrazo = new TextBlock
        {
            Text = "POR PRAZO",
            FontFamily = (FontFamily)Application.Current.Resources["FontMono"],
            FontSize = 10,
            Foreground = (Brush)Application.Current.Resources["TextMuted"],
            VerticalAlignment = VerticalAlignment.Center
        };
        var painelPrioridadesBorda = PainelListaCard("Prioridades", _contadorPrioridadesPainel, rotuloPrazo, _painelAcoes);

        var painelInferior = new Grid();
        painelInferior.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
        painelInferior.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        painelAlertasBorda.Margin = new Thickness(0, 0, 6, 0);
        painelPrioridadesBorda.Margin = new Thickness(6, 0, 0, 0);
        Grid.SetColumn(painelAlertasBorda, 0);
        Grid.SetColumn(painelPrioridadesBorda, 1);
        painelInferior.Children.Add(painelAlertasBorda);
        painelInferior.Children.Add(painelPrioridadesBorda);

        var raiz = new Grid { Margin = new Thickness(18, 16, 20, 18) };
        raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        raiz.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(cabecalho, 0);
        Grid.SetRow(_painelOperacao, 1);
        Grid.SetRow(_faixaIndicadoresSlot, 2);
        Grid.SetRow(painelInferior, 3);
        raiz.Children.Add(cabecalho);
        raiz.Children.Add(_painelOperacao);
        raiz.Children.Add(_faixaIndicadoresSlot);
        raiz.Children.Add(painelInferior);
        return raiz;
    }

    /// <summary>Painel com cabeçalho (título + contador + elemento à direita) e corpo rolável — usado
    /// tanto para "Alertas abertos" quanto para "Prioridades".</summary>
    private static Border PainelListaCard(string titulo, TextBlock contador, FrameworkElement direita, UIElement corpo)
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

    private void AtualizarPainel()
    {
        var bens = _contexto.ListarBens().Executar();
        var garantias = _contexto.ListarGarantias().Executar();
        var manutencoes = _contexto.ListarManutencoes().Executar();
        var documentos = _contexto.ListarDocumentos().Executar();
        var alertas = _contexto.ListarAlertas().Executar();
        var hoje = Hoje();

        var garantiasVencidas = garantias.Count(g => g.StatusEm(hoje) == StatusGarantia.Vencida);
        var garantiasProximas = garantias.Count(g => g.StatusEm(hoje) == StatusGarantia.PertoDoVencimento);
        var manutencoesAtrasadas = manutencoes.Count(m => m.StatusEm(hoje) == StatusManutencao.Atrasada);
        var documentosAusentes = documentos.Count(d => d.Status == StatusDocumento.Ausente);
        var alertasAbertos = alertas.Count(a => a.Status != StatusAlerta.Resolvido);
        var alertasCriticos = alertas.Count(a => a.Status != StatusAlerta.Resolvido && a.Severidade == SeveridadeAlerta.Critico);
        var pontosAtencao = garantiasVencidas + garantiasProximas + manutencoesAtrasadas + documentosAusentes + alertasAbertos;

        _horarioVerificacao.Text = $"verificado {DateTime.Now:dd/MM/yyyy HH:mm}";

        _painelOperacao.Children.Clear();
        AdicionarResumoOperacao(0, "Atenção necessária", pontosAtencao.ToString(),
            pontosAtencao == 0 ? "sem pendências críticas" : "itens pedem revisão", pontosAtencao == 0 ? "StatusOk" : "StatusPerigo");
        AdicionarResumoOperacao(1, "Alertas críticos", alertasCriticos.ToString(),
            alertasCriticos == 0 ? "sem pendências críticas" : alertasCriticos == 1 ? "alerta crítico aberto" : "alertas críticos abertos",
            alertasCriticos == 0 ? "StatusOk" : "StatusPerigo");
        AdicionarResumoOperacao(2, "Base local", (bens.Count + documentos.Count).ToString(), "registros sob controle", "StatusInfo");

        _faixaIndicadoresSlot.Content = FaixaIndicadores(
            ("Bens monitorados", bens.Count, false, null),
            ("Garantias vencidas", garantiasVencidas, garantiasVencidas > 0, "StatusPerigo"),
            ("Garantias próximas", garantiasProximas, garantiasProximas > 0, "StatusAviso"),
            ("Manutenções atrasadas", manutencoesAtrasadas, manutencoesAtrasadas > 0, "StatusPerigo"),
            ("Documentos ausentes", documentosAusentes, false, null),
            ("Alertas abertos", alertasAbertos, alertasAbertos > 0, "StatusInfo"));

        _contadorAlertasPainel.Text = alertasAbertos.ToString();
        _painelAlertas.Children.Clear();
        var alertasOrdenados = alertas
            .Where(a => a.Status != StatusAlerta.Resolvido)
            .OrderBy(a => a.Severidade switch { SeveridadeAlerta.Critico => 0, SeveridadeAlerta.Aviso => 1, _ => 2 })
            .ThenBy(a => a.PrazoEm ?? DateOnly.MaxValue)
            .Take(8);
        foreach (var alerta in alertasOrdenados)
        {
            _painelAlertas.Children.Add(LinhaAlerta(alerta));
        }

        if (_painelAlertas.Children.Count == 0)
        {
            _painelAlertas.Children.Add(TextoVazioPainel("Nenhum alerta aberto."));
        }

        var prioridades = new List<(string Categoria, string Bem, string Detalhe, string ClasseCss, DateOnly? Prazo, DateOnly Ordenacao)>();
        foreach (var garantia in garantias.Where(g => g.StatusEm(hoje) is StatusGarantia.Vencida or StatusGarantia.PertoDoVencimento))
        {
            prioridades.Add(("Garantia", NomeBem(garantia.BemId), Formatador.TextoEnum(garantia.StatusEm(hoje)),
                Formatador.ClasseEtiqueta(garantia.StatusEm(hoje)), garantia.Periodo.TerminaEm, garantia.Periodo.TerminaEm));
        }

        foreach (var manutencao in manutencoes.Where(m => m.StatusEm(hoje) == StatusManutencao.Atrasada))
        {
            prioridades.Add(("Manutenção", NomeBem(manutencao.BemId), manutencao.Descricao,
                Formatador.ClasseEtiqueta(StatusManutencao.Atrasada), manutencao.AgendadaPara, manutencao.AgendadaPara));
        }

        foreach (var documento in documentos.Where(d => d.Status == StatusDocumento.Ausente))
        {
            prioridades.Add(("Documento", NomeBem(documento.BemId), documento.Nome.Valor,
                Formatador.ClasseEtiqueta(StatusDocumento.Ausente), null, hoje));
        }

        foreach (var alerta in alertas.Where(a => a.Status != StatusAlerta.Resolvido && a.Severidade is SeveridadeAlerta.Critico or SeveridadeAlerta.Aviso))
        {
            prioridades.Add(("Alerta", NomeBem(alerta.BemId), alerta.Mensagem,
                Formatador.ClasseEtiqueta(alerta.Severidade), alerta.PrazoEm, alerta.PrazoEm ?? hoje));
        }

        var prioridadesOrdenadas = prioridades.OrderBy(p => p.Ordenacao).Take(6).ToList();
        _contadorPrioridadesPainel.Text = prioridadesOrdenadas.Count.ToString();
        _painelAcoes.Children.Clear();
        foreach (var prioridade in prioridadesOrdenadas)
        {
            _painelAcoes.Children.Add(LinhaPrioridade(prioridade.Categoria, prioridade.Bem, prioridade.Detalhe, prioridade.ClasseCss, prioridade.Prazo));
        }

        if (_painelAcoes.Children.Count == 0)
        {
            _painelAcoes.Children.Add(TextoVazioPainel("Nenhuma prioridade no momento."));
        }
    }

    private void AdicionarResumoOperacao(int coluna, string titulo, string valor, string detalhe, string prefixoStatus)
    {
        var card = ResumoOperacao(titulo, valor, detalhe, prefixoStatus);
        card.Margin = coluna < 2 ? new Thickness(0, 0, 12, 0) : new Thickness(0);
        Grid.SetColumn(card, coluna);
        _painelOperacao.Children.Add(card);
    }

    /// <summary>Card de resumo (§4.1): borda externa sempre neutra — só a barra de 3px carrega a cor semântica,
    /// para que a fileira não pareça três alertas diferentes. Internal: também usado pela galeria do tema.</summary>
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

    /// <summary>Faixa única com os 6 indicadores menores (§4.2), substituindo a antiga fileira de 6 cards
    /// separados — corrige tanto o desalinhamento com a fileira de cima quanto o ruído de 6 bordas.
    /// Internal: também usado pela galeria do tema.</summary>
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
                linhaValor = UiApoio.Linha(0, valorLabel, ponto);
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

    private UIElement LinhaAlerta(Alerta alerta)
    {
        var recursos = Application.Current.Resources;
        var badge = EtiquetaApoio.CriarEtiqueta(Formatador.TextoEnum(alerta.Severidade), Formatador.ClasseEtiqueta(alerta.Severidade));
        badge.VerticalAlignment = VerticalAlignment.Center;

        var nomeBemLabel = new TextBlock
        {
            Text = NomeBem(alerta.BemId),
            Style = (Style)recursos["CelulaDestaqueStyle"],
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };
        var mensagemLabel = new TextBlock
        {
            Text = alerta.Mensagem,
            FontSize = 12.5,
            Foreground = (Brush)recursos["TextSecondary"],
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 10, 0)
        };
        var prazoLabel = new TextBlock
        {
            Text = Formatador.Data(alerta.PrazoEm),
            FontFamily = (FontFamily)recursos["FontMono"],
            FontSize = 11.5,
            Foreground = (Brush)recursos["TextLabel"],
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };

        var grade = new Grid { Height = 42 };
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(176) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
        Grid.SetColumn(badge, 0);
        Grid.SetColumn(nomeBemLabel, 1);
        Grid.SetColumn(mensagemLabel, 2);
        Grid.SetColumn(prazoLabel, 3);
        grade.Children.Add(badge);
        grade.Children.Add(nomeBemLabel);
        grade.Children.Add(mensagemLabel);
        grade.Children.Add(prazoLabel);

        return new Border { BorderBrush = (Brush)recursos["RowDivider"], BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(14, 0, 14, 0), Child = grade };
    }

    private static UIElement LinhaPrioridade(string categoria, string bem, string detalhe, string classeCss, DateOnly? prazo)
    {
        var recursos = Application.Current.Resources;
        var badge = EtiquetaApoio.CriarEtiqueta(categoria, classeCss);
        badge.VerticalAlignment = VerticalAlignment.Center;

        var bemLabel = new TextBlock
        {
            Text = bem,
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)recursos["TextBody"],
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var detalheLabel = new TextBlock
        {
            Text = detalhe,
            FontSize = 11.5,
            Foreground = (Brush)recursos["TextLabel"],
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var textos = UiApoio.Coluna(2, bemLabel, detalheLabel);
        textos.VerticalAlignment = VerticalAlignment.Center;
        textos.Margin = new Thickness(10, 0, 10, 0);

        var prazoLabel = new TextBlock
        {
            Text = Formatador.Data(prazo),
            FontFamily = (FontFamily)recursos["FontMono"],
            FontSize = 11.5,
            Foreground = (Brush)recursos["TextSecondary"],
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };

        var grade = new Grid { Height = 52 };
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(104) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(92) });
        Grid.SetColumn(badge, 0);
        Grid.SetColumn(textos, 1);
        Grid.SetColumn(prazoLabel, 2);
        grade.Children.Add(badge);
        grade.Children.Add(textos);
        grade.Children.Add(prazoLabel);

        return new Border { BorderBrush = (Brush)recursos["RowDivider"], BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(14, 0, 14, 0), Child = grade };
    }

    private static TextBlock TextoVazioPainel(string texto) => new()
    {
        Text = texto,
        FontSize = 13,
        Foreground = (Brush)Application.Current.Resources["TextSecondary"],
        Margin = new Thickness(14, 12, 14, 12)
    };

    private sealed record ItemNavegacao(string Titulo, string Descricao, UIElement Conteudo, bool ComContador = true);
}
