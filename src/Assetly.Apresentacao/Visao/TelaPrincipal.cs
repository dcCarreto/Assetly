using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Assetly.Dominio.Preferencias;

namespace Assetly.Apresentacao.Visao;

/// <summary>
/// Casca da aplicação: sidebar, área de conteúdo, scrim de diálogo, painel de espera das operações em segundo
/// plano e o loop de atualização (intervalo configurável).
/// Não conhece nenhum módulo específico — só monta a lista de <see cref="IModulo"/> e itera sobre
/// ela para desenhar a sidebar, trocar de conteúdo e atualizar os dados. Adicionar um módulo novo
/// é criar uma classe implementando IModulo e adicionar uma linha na lista abaixo.
/// </summary>
internal sealed class TelaPrincipal
{
    private readonly ContextoAplicacao _contexto;
    private readonly Grid _raizComScrim = new();
    private readonly Border _scrim = new();
    private readonly ContentControl _areaConteudo = new();
    private readonly List<IModulo> _modulos = [];
    private readonly List<ToggleButton> _botoesNavegacao = [];
    private readonly Dictionary<string, ToggleButton> _botoesPorTitulo = [];
    private readonly Dictionary<string, TextBlock> _contadoresNavegacao = [];
    private readonly Border _painelEspera = new();
    private readonly TextBlock _textoEspera = new();
    private readonly DockPanel _estrutura = new();
    private int _dialogosAbertos;
    private bool _emEspera;
    private bool _atualizando;
    private bool _falhaAtualizacaoNotificada;
    private DispatcherTimer? _atualizacaoTempoReal;
    private IModulo? _moduloVisivel;
    private DateOnly _diaDosAlertas;

    internal UIElement Raiz => _raizComScrim;

    internal UIElement SeloAmbiente { get; }

    internal IReadOnlyList<IModulo> Modulos => _modulos;

    /// <summary>Depois de cada recálculo dos alertas (abertura, alteração de dados, virada do dia) — para as notificações.</summary>
    internal event Action? AlertasAtualizados;

    private ModuloBackup? Backup => _modulos.OfType<ModuloBackup>().FirstOrDefault();

    /// <summary>Sincronização opcional entre computadores (ver ModuloBackup): ao abrir pode receber.</summary>
    internal void SincronizarAoAbrir() => Backup?.SincronizarAoAbrir();

    /// <summary>Antes de fechar: termina a operação em andamento e envia a sincronização; depois chama <paramref name="fechar"/>.</summary>
    internal void FinalizarAntesDeFechar(Action fechar)
    {
        if (Backup is { } backup)
        {
            backup.FinalizarAntesDeFechar(fechar);
        }
        else
        {
            fechar();
        }
    }

    /// <summary>Mostra um módulo mesmo que esteja oculto na barra lateral (usado pelo autoteste).</summary>
    internal void IrPara(string titulo)
    {
        if (_botoesPorTitulo.ContainsKey(titulo))
        {
            IrParaModulo(titulo);
            return;
        }

        SelecionarConteudo(_modulos.First(m => m.Titulo == titulo).Conteudo);
    }

    internal TelaPrincipal(ContextoAplicacao contexto, Action aplicarAparenciaEReconstruir, string? selecaoForcada = null, GerenciadorAtualizacoes? atualizacoes = null)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto), "contexto da aplicação é obrigatório");
        SeloAmbiente = CriarSeloAmbiente();
        JanelaEscura.AlternarScrim = visivel =>
        {
            _dialogosAbertos += visivel ? 1 : -1;
            _scrim.Visibility = _dialogosAbertos > 0 ? Visibility.Visible : Visibility.Collapsed;
        };

        OperacaoEmSegundoPlano.AlternarEspera = AlternarEspera;

        _modulos.Add(new ModuloPainel(_contexto, IrParaModulo));
        _modulos.Add(new ModuloBens(_contexto));
        _modulos.Add(new ModuloPatrimonio(_contexto));
        _modulos.Add(new ModuloGarantias(_contexto));
        _modulos.Add(new ModuloManutencoes(_contexto));
        _modulos.Add(new ModuloDocumentos(_contexto));
        _modulos.Add(new ModuloAlertas(_contexto));
        _modulos.Add(new ModuloSaude(_contexto));
        _modulos.Add(new ModuloGastosSaude(_contexto));
        _modulos.Add(ModulosVeiculoImovel.Automoveis(_contexto));
        _modulos.Add(ModulosVeiculoImovel.Imoveis(_contexto));
        _modulos.Add(new ModuloBackup(_contexto));

        var titulos = _modulos.Select(modulo => modulo.Titulo).ToList();
        var preferencias = _contexto.SincronizarModulosPreferencias().Executar(titulos);

        _modulos.Add(new ModuloConfiguracoes(_contexto, aplicarAparenciaEReconstruir, AtualizarIntervaloAtualizacao, atualizacoes));

        ConfigurarRaiz(preferencias, selecaoForcada);
        _raizComScrim.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.K && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control && _dialogosAbertos == 0 && !_emEspera)
            {
                e.Handled = true;
                AcoesApoio.ExecutarComTratamento(AbrirBuscaGeral);
            }
        };
        AtualizarTudo();
        IniciarAtualizacaoTempoReal(preferencias.IntervaloComoTimeSpan());
        _contexto.DadosAlterados += AoAlterarDados;
    }

    /// <summary>Reprocessa os alertas automáticos (uma vez, antes de qualquer módulo ler alertas) e
    /// atualiza todos os módulos e contadores da barra lateral.</summary>
    internal void AtualizarTudo()
    {
        if (_atualizando)
        {
            return;
        }

        _atualizando = true;
        try
        {
            var hoje = ContextoApoio.Hoje();
            _contexto.LixeiraBens().EsvaziarVencidos(hoje);
            _contexto.GerarAlertasAutomaticos().Executar(hoje);
            _diaDosAlertas = hoje;
            foreach (var modulo in _modulos)
            {
                modulo.Atualizar();
            }

            AtualizarContadoresNavegacao();
        }
        finally
        {
            _atualizando = false;
        }

        AlertasAtualizados?.Invoke();
    }

    private void AoAlterarDados() => AcoesApoio.ExecutarComTratamento(AtualizarTudo);

    /// <summary>Ctrl+K: busca em todos os módulos e leva ao registro escolhido, já selecionado na tabela da tela dele.</summary>
    internal void AbrirBuscaGeral()
    {
        if (BuscaGeralDialogo.Escolher(_contexto) is not { } resultado)
        {
            return;
        }

        var titulo = resultado.Area switch
        {
            Aplicacao.CasoDeUso.AreaBusca.Bem => "Bens",
            Aplicacao.CasoDeUso.AreaBusca.Garantia => "Garantias",
            Aplicacao.CasoDeUso.AreaBusca.Manutencao => "Manutenções",
            Aplicacao.CasoDeUso.AreaBusca.Documento => "Documentos",
            Aplicacao.CasoDeUso.AreaBusca.Alerta => "Alertas",
            Aplicacao.CasoDeUso.AreaBusca.Convenio => "Convênios",
            Aplicacao.CasoDeUso.AreaBusca.GastoSaude => "Gastos de saúde",
            Aplicacao.CasoDeUso.AreaBusca.Imovel => ModulosVeiculoImovel.TituloImoveis,
            Aplicacao.CasoDeUso.AreaBusca.Despesa when _contexto.RepositorioVeiculo.BuscarPorBem(resultado.BemId) is null => ModulosVeiculoImovel.TituloImoveis,
            _ => ModulosVeiculoImovel.TituloAutomoveis
        };
        IrPara(titulo);
        _modulos.First(m => m.Titulo == titulo).Localizar(resultado.Chave);
    }

    /// <summary>Para o timer de atualização antes desta instância ser descartada — chamado pelo App
    /// ao reconstruir a janela após uma troca de aparência, para não deixar dois timers rodando.</summary>
    internal void Encerrar()
    {
        _atualizacaoTempoReal?.Stop();
        _contexto.DadosAlterados -= AoAlterarDados;
    }

    internal void AtualizarIntervaloAtualizacao(TimeSpan intervalo)
    {
        if (_atualizacaoTempoReal is not null)
        {
            _atualizacaoTempoReal.Interval = intervalo;
        }
    }

    private void IniciarAtualizacaoTempoReal(TimeSpan intervalo)
    {
        _atualizacaoTempoReal = new DispatcherTimer { Interval = intervalo };
        _atualizacaoTempoReal.Tick += (_, _) => AtualizarPorTemporizador();
        _atualizacaoTempoReal.Start();
    }

    /// <summary>Ciclo automático, leve: os dados só mudam por ação do usuário ou por uma sincronização recebida —
    /// e as duas já atualizam tudo na hora (<see cref="ContextoAplicacao.DadosAlterados"/>). Aqui só o módulo visível
    /// é redesenhado; os alertas são refeitos e todos os módulos recarregados apenas na virada do dia (prazos mudam
    /// com a data). Também dispara o backup automático e o envio da sincronização, em segundo plano.
    /// <para>Não atualiza por trás de um diálogo aberto ou de uma operação em andamento e, se a atualização falhar
    /// (ex.: banco bloqueado por outro programa), avisa uma única vez — sem empilhar uma janela de erro a cada ciclo —
    /// e registra as falhas seguintes no log até a próxima atualização bem-sucedida.</para></summary>
    private void AtualizarPorTemporizador()
    {
        if (_dialogosAbertos > 0 || _emEspera)
        {
            return;
        }

        try
        {
            if (ContextoApoio.Hoje() != _diaDosAlertas)
            {
                AtualizarTudo();
            }
            else
            {
                _moduloVisivel?.Atualizar();
            }

            Backup?.ExecutarTarefasPeriodicas();
            _falhaAtualizacaoNotificada = false;
        }
        catch (Exception excecao) when (excecao is not OutOfMemoryException)
        {
            RegistroErros.Registrar("Falha na atualização automática", excecao);
            if (!_falhaAtualizacaoNotificada)
            {
                _falhaAtualizacaoNotificada = true;
                DialogoApoio.MostrarErro(excecao);
            }
        }
    }

    private void ConfigurarRaiz(ConfiguracaoPreferencias preferencias, string? selecaoForcada)
    {
        var recursos = Application.Current.Resources;
        var navegacao = NavegacaoLateral(preferencias, selecaoForcada);
        DockPanel.SetDock(navegacao, Dock.Left);
        _estrutura.Children.Add(navegacao);
        _estrutura.Children.Add(_areaConteudo);

        _scrim.Background = (Brush)recursos["DialogScrim"];
        _scrim.Visibility = Visibility.Collapsed;
        _scrim.IsHitTestVisible = false;

        _raizComScrim.Children.Add(_estrutura);
        _raizComScrim.Children.Add(_scrim);
        _raizComScrim.Children.Add(CriarPainelEspera(recursos));
    }

    /// <summary>Painel por cima de tudo enquanto uma operação pedida pelo usuário roda em segundo plano: a janela
    /// continua respondendo (move, redesenha), mas não aceita cliques nem teclas até a operação terminar.</summary>
    private Border CriarPainelEspera(ResourceDictionary recursos)
    {
        _textoEspera.FontSize = 13;
        _textoEspera.Foreground = (Brush)recursos["TextTitle"];
        _textoEspera.TextWrapping = TextWrapping.Wrap;
        _textoEspera.TextAlignment = TextAlignment.Center;
        var barra = new ProgressBar
        {
            IsIndeterminate = true,
            Height = 4,
            Margin = new Thickness(0, 14, 0, 0),
            BorderThickness = new Thickness(0),
            Foreground = (Brush)recursos["AccentPrimary"],
            Background = (Brush)recursos["BgSurface2"]
        };
        var detalhe = UiApoio.Texto("Isso pode levar alguns instantes se houver muitos documentos.", 11.5, FontWeights.Normal, (Brush)recursos["TextMuted"]);
        detalhe.Margin = new Thickness(0, 10, 0, 0);
        detalhe.HorizontalAlignment = HorizontalAlignment.Center;
        var cartao = new Border
        {
            Style = (Style)recursos["CardPanelStyle"],
            Padding = new Thickness(28, 22, 28, 22),
            Width = 420,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = UiApoio.Coluna(0, _textoEspera, barra, detalhe)
        };
        _painelEspera.Background = (Brush)recursos["DialogScrim"];
        _painelEspera.Visibility = Visibility.Collapsed;
        _painelEspera.Child = cartao;
        System.Windows.Automation.AutomationProperties.SetLiveSetting(_textoEspera, System.Windows.Automation.AutomationLiveSetting.Polite);
        return _painelEspera;
    }

    private void AlternarEspera(string? mensagem)
    {
        _emEspera = mensagem is not null;
        _textoEspera.Text = mensagem ?? string.Empty;
        _painelEspera.Visibility = _emEspera ? Visibility.Visible : Visibility.Collapsed;
        _estrutura.IsEnabled = !_emEspera;
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

    private UIElement NavegacaoLateral(ConfiguracaoPreferencias preferencias, string? selecaoForcada)
    {
        var recursos = Application.Current.Resources;

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
        var buscar = AcoesApoio.BotaoSecundario("Buscar em tudo   Ctrl+K", () => AbrirBuscaGeral());
        buscar.HorizontalAlignment = HorizontalAlignment.Stretch;
        buscar.HorizontalContentAlignment = HorizontalAlignment.Left;
        buscar.Margin = new Thickness(0, 0, 0, 12);
        buscar.ToolTip = "Busca em bens, garantias, manutenções, documentos, alertas, saúde, veículos, imóveis e contas (Ctrl+K)";
        topo.Children.Add(buscar);
        topo.Children.Add(titulo);

        var modulosPorTitulo = _modulos.ToDictionary(modulo => modulo.Titulo);
        var visiveisNaOrdem = preferencias.OrdemModulos
            .Where(titulo => !preferencias.ModulosOcultos.Contains(titulo))
            .Select(titulo => modulosPorTitulo[titulo]);
        var paraSidebar = visiveisNaOrdem.Append(modulosPorTitulo["Configurações"]).ToList();

        foreach (var modulo in paraSidebar)
        {
            var botao = BotaoNavegacao(modulo);
            _botoesNavegacao.Add(botao);
            _botoesPorTitulo[modulo.Titulo] = botao;
            topo.Children.Add(botao);
        }

        if (_botoesNavegacao.Count > 0)
        {
            var tituloSelecionado = selecaoForcada ?? preferencias.ResolverModuloInicial();
            var moduloSelecionado = tituloSelecionado is not null && modulosPorTitulo.TryGetValue(tituloSelecionado, out var encontrado)
                ? encontrado
                : paraSidebar[0];
            _botoesPorTitulo[moduloSelecionado.Titulo].IsChecked = true;
            SelecionarConteudo(moduloSelecionado.Conteudo);
        }

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

        // A lista de módulos rola quando não cabe na altura (janela pequena ou muitos módulos visíveis);
        // o rodapé fica sempre fixo embaixo.
        var listaRolavel = new ScrollViewer
        {
            Content = topo,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Focusable = false
        };
        var menu = new DockPanel { Background = (Brush)recursos["BgSidebar"], Width = 208, LastChildFill = true };
        menu.Children.Add(rodape);
        menu.Children.Add(listaRolavel);

        return new Border
        {
            BorderBrush = (Brush)recursos["CardBorder"],
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(10, 14, 10, 0),
            Child = menu
        };
    }

    /// <summary>Ícone (fonte de símbolos do Windows) de cada módulo na barra lateral.</summary>
    private static string GlifoModulo(string titulo) => titulo switch
    {
        "Painel" => "",
        "Bens" => "",
        "Patrimônio" => "",
        "Garantias" => "",
        "Manutenções" => "",
        "Documentos" => "",
        "Alertas" => "",
        "Convênios" => "",
        "Gastos de saúde" => "",
        "Automóveis" => "",
        "Imóveis" => "",
        "Backup" => "",
        "Configurações" => "",
        _ => ""
    };

    private ToggleButton BotaoNavegacao(IModulo modulo)
    {
        var recursos = Application.Current.Resources;
        var icone = new TextBlock
        {
            Text = GlifoModulo(modulo.Titulo),
            FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
            FontSize = 15,
            Width = 18,
            TextAlignment = TextAlignment.Center,
            Foreground = (Brush)recursos["SidebarIconNormal"],
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };

        var titulo = new TextBlock { Text = modulo.Titulo, Style = (Style)recursos["ItemMenuLateralTextoStyle"] };
        var descricao = UiApoio.Texto(modulo.Descricao, 11, FontWeights.Normal, (Brush)recursos["TextMuted"]);
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
        if (modulo.ComContador)
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
            _contadoresNavegacao[modulo.Titulo] = contador;
        }

        var botao = new ToggleButton
        {
            Style = (Style)recursos["SidebarItemStyle"],
            Content = grade,
            Margin = new Thickness(0, 0, 0, 2),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        System.Windows.Automation.AutomationProperties.SetName(botao, modulo.Titulo);

        void AtualizarEstadoVisual()
        {
            var selecionado = botao.IsChecked == true;
            icone.Foreground = (Brush)recursos[selecionado ? "SidebarIconSelected" : "SidebarIconNormal"];
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

            SelecionarPorBotao(botao, modulo.Conteudo);
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

    private void IrParaModulo(string titulo)
    {
        if (!_botoesPorTitulo.TryGetValue(titulo, out var botao) || botao.IsChecked == true)
        {
            return;
        }

        var modulo = _modulos.First(m => m.Titulo == titulo);
        botao.IsChecked = true;
        SelecionarPorBotao(botao, modulo.Conteudo);
    }

    private void AtualizarContadoresNavegacao()
    {
        foreach (var modulo in _modulos.Where(m => m.ComContador))
        {
            if (!_contadoresNavegacao.TryGetValue(modulo.Titulo, out var contador))
            {
                continue;
            }

            var contagem = modulo.Contador;
            contador.Text = contagem.ToString();

            var alertaPendente = modulo.Titulo == "Alertas" && contagem > 0;
            contador.Tag = alertaPendente ? "alerta-pendente" : null;
            if (alertaPendente)
            {
                contador.Foreground = (Brush)Application.Current.Resources["StatusAvisoText"];
            }
            else
            {
                var selecionado = _botoesPorTitulo.TryGetValue(modulo.Titulo, out var botao) && botao.IsChecked == true;
                contador.Foreground = (Brush)Application.Current.Resources[selecionado ? "TextSecondary" : "TextMuted"];
            }
        }
    }

    /// <summary>Troca o conteúdo e leva o foco do teclado para ele — sem isso o foco ficava no botão
    /// da barra lateral e os atalhos do módulo (Ctrl+N, Ctrl+F, F2, Delete) não respondiam até o
    /// usuário clicar dentro da tela.</summary>
    private void SelecionarConteudo(UIElement conteudo)
    {
        _areaConteudo.Content = conteudo;
        _moduloVisivel = _modulos.FirstOrDefault(modulo => ReferenceEquals(modulo.Conteudo, conteudo));
        if (conteudo.Focusable)
        {
            conteudo.Dispatcher.BeginInvoke(DispatcherPriority.Input, () => conteudo.Focus());
        }
    }
}
