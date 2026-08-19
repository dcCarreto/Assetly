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

namespace Assetly.Apresentacao.Visao;

internal sealed partial class TelaPrincipal
{
    private readonly ContextoAplicacao _contexto;
    private readonly DockPanel _raiz = new();
    private readonly ContentControl _areaConteudo = new();
    private readonly DataGrid _tabelaBens = new();
    private readonly DataGrid _tabelaGarantias = new();
    private readonly DataGrid _tabelaManutencoes = new();
    private readonly DataGrid _tabelaDocumentos = new();
    private readonly DataGrid _tabelaAlertas = new();
    private readonly StackPanel _painelOperacao = new() { Orientation = Orientation.Horizontal };
    private readonly WrapPanel _painelResumo = new();
    private readonly StackPanel _painelAlertas = new();
    private readonly StackPanel _painelAcoes = new();
    private DispatcherTimer? _atualizacaoTempoReal;

    internal UIElement Raiz => _raiz;

    internal TelaPrincipal(ContextoAplicacao contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto), "contexto da aplicação é obrigatório");
        ConfigurarRaiz();
        ConfigurarTabelas();
        AtualizarTudo();
        IniciarAtualizacaoTempoReal();
    }

    internal void AtualizarTudo()
    {
        _contexto.GerarAlertasAutomaticos().Executar(Hoje());
        var bens = _contexto.ListarBens().Executar();
        TabelaApoio.RecarregarTabela(_tabelaBens, ConstruirLinhasBens(bens), l => ((BemLinha)l).Id);
        TabelaApoio.RecarregarTabela(_tabelaGarantias, ConstruirLinhasGarantias(_contexto.ListarGarantias().Executar()), l => ((GarantiaLinha)l).Id);
        TabelaApoio.RecarregarTabela(_tabelaManutencoes, ConstruirLinhasManutencoes(_contexto.ListarManutencoes().Executar()), l => ((ManutencaoLinha)l).Id);
        TabelaApoio.RecarregarTabela(_tabelaDocumentos, ConstruirLinhasDocumentos(_contexto.ListarDocumentos().Executar()), l => ((DocumentoLinha)l).Id);
        TabelaApoio.RecarregarTabela(_tabelaAlertas, ConstruirLinhasAlertas(_contexto.ListarAlertas().Executar()), l => ((AlertaLinha)l).Id);
        AtualizarPainel();
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
        _raiz.Background = (Brush)Application.Current.Resources["FundoRaizBrush"];

        var itens = new List<ItemNavegacao>
        {
            new("Painel", "Resumo operacional", CriarPainel()),
            new("Bens", "Inventário e status", CriarAbaBens()),
            new("Garantias", "Cobertura e vencimentos", CriarAbaGarantias()),
            new("Manutenções", "Agenda e custos", CriarAbaManutencoes()),
            new("Documentos", "Arquivos vinculados", CriarAbaDocumentos()),
            new("Alertas", "Pendências abertas", CriarAbaAlertas())
        };

        var cabecalho = Cabecalho();
        DockPanel.SetDock(cabecalho, Dock.Top);

        var estrutura = new DockPanel();
        var navegacao = NavegacaoLateral(itens);
        DockPanel.SetDock(navegacao, Dock.Left);
        estrutura.Children.Add(navegacao);
        estrutura.Children.Add(_areaConteudo);

        _raiz.Children.Add(cabecalho);
        _raiz.Children.Add(estrutura);

        SelecionarConteudo(itens[0].Conteudo);
    }

    private UIElement Cabecalho()
    {
        var marca = MarcaVisual();

        var titulo = UiApoio.Texto("Assetly", 27, FontWeights.Black, (Brush)Application.Current.Resources["TextoTituloBrush"]);
        var subtitulo = UiApoio.Texto("Gestão local de bens, garantias, documentos e manutenções.", 13, FontWeights.Normal,
            (Brush)Application.Current.Resources["TextoSuave2Brush"]);

        var textos = UiApoio.Coluna(2, titulo, subtitulo);
        var identidade = UiApoio.Linha(12, marca, textos);
        identidade.VerticalAlignment = VerticalAlignment.Center;

        var seloAmbiente = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x10, 0x1A, 0x2A)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4F)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(10, 6, 10, 6),
            VerticalAlignment = VerticalAlignment.Center,
            Child = UiApoio.Texto(_contexto.RotuloAmbiente, 12, FontWeights.Bold, new SolidColorBrush(Color.FromRgb(0xB6, 0xC5, 0xD8)))
        };
        if (_contexto.AmbienteTeste)
        {
            seloAmbiente.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x21, 0x12));
            seloAmbiente.BorderBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
            ((TextBlock)seloAmbiente.Child).Foreground = new SolidColorBrush(Color.FromRgb(0xFD, 0xE6, 0x8A));
        }

        var cabecalho = new DockPanel
        {
            Background = (Brush)Application.Current.Resources["CabecalhoAplicacaoBrush"],
            Margin = new Thickness(0),
            LastChildFill = false
        };
        var borda = new Border
        {
            BorderBrush = (Brush)Application.Current.Resources["CardBordaBrush"],
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(24, 8, 24, 8),
            Background = (Brush)Application.Current.Resources["CabecalhoAplicacaoBrush"],
            Effect = (System.Windows.Media.Effects.Effect)Application.Current.Resources["SombraCabecalho"]
        };
        var linha = new DockPanel { LastChildFill = false };
        DockPanel.SetDock(identidade, Dock.Left);
        DockPanel.SetDock(seloAmbiente, Dock.Right);
        linha.Children.Add(identidade);
        linha.Children.Add(seloAmbiente);
        borda.Child = linha;
        return borda;
    }

    private UIElement MarcaVisual()
    {
        var caixa = new Border
        {
            Width = 78,
            Height = 78,
            CornerRadius = new CornerRadius(8),
            Background = (Brush)Application.Current.Resources["MarcaSimboloBrush"],
            Child = new System.Windows.Controls.Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Assetly;component/Recursos/Imagens/assetly-icon-128.png")),
                Width = 78,
                Height = 78,
                Stretch = Stretch.Uniform
            }
        };
        return caixa;
    }

    private UIElement NavegacaoLateral(IReadOnlyList<ItemNavegacao> itens)
    {
        var titulo = UiApoio.Texto("Módulos", 12, FontWeights.Black, (Brush)Application.Current.Resources["TextoJanelaBrush"]);
        var descricao = UiApoio.Texto("Controle por área", 11, FontWeights.Normal, (Brush)Application.Current.Resources["TextoApagadoBrush"]);
        var cabecalho = UiApoio.Coluna(2, titulo, descricao);
        cabecalho.Margin = new Thickness(6, 0, 6, 8);

        var menu = new StackPanel { Background = (Brush)Application.Current.Resources["MenuLateralBrush"], Width = 226 };
        var borda = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x18, 0x22, 0x33)),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(14, 18, 14, 18),
            Child = menu
        };

        menu.Children.Add(cabecalho);

        ToggleButton? primeiro = null;
        var botoes = new List<ToggleButton>();
        foreach (var item in itens)
        {
            var botao = BotaoNavegacao(item, botoes);
            botoes.Add(botao);
            menu.Children.Add(botao);
            primeiro ??= botao;
        }

        if (primeiro is not null)
        {
            primeiro.IsChecked = true;
        }

        var espacador = new Border { MinHeight = 8 };
        menu.Children.Add(espacador);

        var rodape = new Border
        {
            Style = (Style)Application.Current.Resources["CardStyle"],
            Padding = new Thickness(10),
            Effect = null,
            Child = UiApoio.Texto("Base local SQLite\nDados privados no dispositivo", 11, FontWeights.Bold,
                (Brush)Application.Current.Resources["TextoSuaveBrush"])
        };
        menu.Children.Add(rodape);

        return borda;
    }

    private ToggleButton BotaoNavegacao(ItemNavegacao item, List<ToggleButton> irmaos)
    {
        var titulo = UiApoio.Texto(item.Titulo, 13, FontWeights.Black, (Brush)Application.Current.Resources["TextoJanelaBrush"]);
        var descricao = UiApoio.Texto(item.Descricao, 11, FontWeights.Normal, new SolidColorBrush(Color.FromRgb(0x7F, 0x92, 0xAA)));
        var textos = UiApoio.Coluna(2, titulo, descricao);

        var botao = new ToggleButton
        {
            Style = (Style)Application.Current.Resources["BotaoNavegacaoStyle"],
            Content = textos,
            Margin = new Thickness(0, 0, 0, 6),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        botao.Click += (_, _) =>
        {
            if (botao.IsChecked != true)
            {
                botao.IsChecked = true;
                return;
            }

            foreach (var outro in irmaos)
            {
                if (!ReferenceEquals(outro, botao))
                {
                    outro.IsChecked = false;
                }
            }

            SelecionarConteudo(item.Conteudo);
        };
        return botao;
    }

    private void SelecionarConteudo(UIElement conteudo)
    {
        _areaConteudo.Content = conteudo;
    }

    private UIElement CriarPainel()
    {
        _painelResumo.Margin = new Thickness(0, 0, 0, 0);

        var tituloPainel = UiApoio.Texto("Visão geral", 17, FontWeights.Black, (Brush)Application.Current.Resources["TextoTituloBrush"]);
        var subtituloPainel = UiApoio.Texto("Indicadores críticos para acompanhar a saúde dos ativos.", 13, FontWeights.Normal,
            (Brush)Application.Current.Resources["TextoSuaveBrush"]);
        var cabecalhoPainel = UiApoio.Coluna(4, tituloPainel, subtituloPainel);

        var tituloAlertas = UiApoio.Texto("Alertas abertos", 17, FontWeights.Black, (Brush)Application.Current.Resources["TextoTituloBrush"]);
        var tituloAcoes = UiApoio.Texto("Prioridades", 17, FontWeights.Black, (Brush)Application.Current.Resources["TextoTituloBrush"]);

        var painelAlertasBorda = new Border { Style = (Style)Application.Current.Resources["CardStyle"], Child = _painelAlertas };
        var painelAcoesBorda = new Border { Style = (Style)Application.Current.Resources["CardStyle"], Child = _painelAcoes };

        var secaoAlertas = UiApoio.Coluna(10, tituloAlertas, painelAlertasBorda);
        var secaoAcoes = UiApoio.Coluna(10, tituloAcoes, painelAcoesBorda);

        var painelInferior = new Grid();
        painelInferior.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        painelInferior.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        secaoAlertas.Margin = new Thickness(0, 0, 7, 0);
        secaoAcoes.Margin = new Thickness(7, 0, 0, 0);
        Grid.SetColumn(secaoAlertas, 0);
        Grid.SetColumn(secaoAcoes, 1);
        painelInferior.Children.Add(secaoAlertas);
        painelInferior.Children.Add(secaoAcoes);

        var conteudo = UiApoio.Coluna(18, cabecalhoPainel, _painelOperacao, _painelResumo, painelInferior);
        conteudo.Margin = new Thickness(18);
        return new ScrollViewer { Content = conteudo, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
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

        _painelOperacao.Children.Clear();
        _painelOperacao.Children.Add(ResumoOperacao("Atenção necessária", pontosAtencao.ToString(),
            pontosAtencao == 0 ? "sem pendências críticas" : "itens pedem revisão", pontosAtencao == 0 ? "resumo-ok" : "resumo-perigo"));
        _painelOperacao.Children.Add(ResumoOperacao("Alertas críticos", alertasCriticos.ToString(),
            alertasCriticos == 1 ? "alerta crítico aberto" : "alertas críticos abertos", alertasCriticos == 0 ? "resumo-ok" : "resumo-perigo"));
        _painelOperacao.Children.Add(ResumoOperacao("Base local", (bens.Count + documentos.Count).ToString(),
            "registros sob controle", "resumo-informativo"));

        _painelResumo.Children.Clear();
        _painelResumo.Children.Add(Indicador("Bens monitorados", bens.Count.ToString(), "indicador-neutro"));
        _painelResumo.Children.Add(Indicador("Garantias vencidas", garantiasVencidas.ToString(), "indicador-perigo"));
        _painelResumo.Children.Add(Indicador("Garantias próximas", garantiasProximas.ToString(), "indicador-aviso"));
        _painelResumo.Children.Add(Indicador("Manutenções atrasadas", manutencoesAtrasadas.ToString(), "indicador-perigo"));
        _painelResumo.Children.Add(Indicador("Documentos ausentes", documentosAusentes.ToString(), "indicador-aviso"));
        _painelResumo.Children.Add(Indicador("Alertas abertos", alertasAbertos.ToString(), "indicador-informativo"));

        _painelAlertas.Children.Clear();
        foreach (var alerta in alertas.Where(a => a.Status != StatusAlerta.Resolvido).Take(8))
        {
            _painelAlertas.Children.Add(AlertaResumo(alerta));
        }

        if (_painelAlertas.Children.Count == 0)
        {
            _painelAlertas.Children.Add(UiApoio.Texto("Nenhum alerta aberto.", 13, FontWeights.Normal, (Brush)Application.Current.Resources["TextoSuaveBrush"]));
        }

        _painelAcoes.Children.Clear();
        foreach (var garantia in garantias.Where(g => g.StatusEm(hoje) is StatusGarantia.Vencida or StatusGarantia.PertoDoVencimento).Take(2))
        {
            _painelAcoes.Children.Add(PrioridadeResumo("Garantia", NomeBem(garantia.BemId),
                $"fim em {Formatador.Data(garantia.Periodo.TerminaEm)}", Formatador.ClasseEtiqueta(garantia.StatusEm(hoje))));
        }

        foreach (var manutencao in manutencoes.Where(m => m.StatusEm(hoje) == StatusManutencao.Atrasada).Take(2))
        {
            _painelAcoes.Children.Add(PrioridadeResumo("Manutenção", NomeBem(manutencao.BemId),
                $"{manutencao.Descricao} - agendada para {Formatador.Data(manutencao.AgendadaPara)}",
                Formatador.ClasseEtiqueta(StatusManutencao.Atrasada)));
        }

        foreach (var documento in documentos.Where(d => d.Status == StatusDocumento.Ausente).Take(2))
        {
            _painelAcoes.Children.Add(PrioridadeResumo("Documento", NomeBem(documento.BemId), documento.Nome.Valor,
                Formatador.ClasseEtiqueta(StatusDocumento.Ausente)));
        }

        foreach (var alerta in alertas.Where(a => a.Status != StatusAlerta.Resolvido)
                     .Where(a => a.Severidade is SeveridadeAlerta.Critico or SeveridadeAlerta.Aviso).Take(2))
        {
            _painelAcoes.Children.Add(PrioridadeResumo("Alerta", NomeBem(alerta.BemId), alerta.Mensagem, Formatador.ClasseEtiqueta(alerta.Severidade)));
        }

        if (_painelAcoes.Children.Count == 0)
        {
            _painelAcoes.Children.Add(UiApoio.Texto("Nenhuma prioridade no momento.", 13, FontWeights.Normal, (Brush)Application.Current.Resources["TextoSuaveBrush"]));
        }
    }

    private UIElement AlertaResumo(Alerta alerta)
    {
        var severidade = EtiquetaApoio.CriarEtiqueta(Formatador.TextoEnum(alerta.Severidade), Formatador.ClasseEtiqueta(alerta.Severidade));
        var mensagem = UiApoio.Texto($"{NomeBem(alerta.BemId)} - {alerta.Mensagem}", 13, FontWeights.Normal, (Brush)Application.Current.Resources["TextoCelulaBrush"]);
        mensagem.VerticalAlignment = VerticalAlignment.Center;

        var linha = UiApoio.LinhaComCrescimento(10, severidade, mensagem);
        linha.Margin = new Thickness(0, 0, 0, 6);
        return new Border { Background = new SolidColorBrush(Color.FromRgb(0x11, 0x1A, 0x29)), CornerRadius = new CornerRadius(7), Padding = new Thickness(10, 9, 10, 9), Child = linha };
    }

    private UIElement ResumoOperacao(string titulo, string valor, string detalhe, string classeCss)
    {
        var tituloLabel = UiApoio.Texto(titulo, 12, FontWeights.Black, (Brush)Application.Current.Resources["TextoSuaveBrush"]);
        var valorLabel = UiApoio.Texto(valor, 32, FontWeights.Black, (Brush)Application.Current.Resources["TextoTituloBrush"]);
        var detalheLabel = UiApoio.Texto(detalhe, 12, FontWeights.Normal, (Brush)Application.Current.Resources["TextoSuave2Brush"]);
        var caixa = UiApoio.Coluna(3, tituloLabel, valorLabel, detalheLabel);

        var borda = CorResumo(classeCss);
        var moldura = new Border
        {
            Background = (Brush)Application.Current.Resources["ResumoOperacaoBrush"],
            BorderBrush = borda,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14),
            Width = 240,
            Margin = new Thickness(0, 0, 10, 0),
            Effect = (System.Windows.Media.Effects.Effect)Application.Current.Resources["SombraCard"],
            Child = caixa
        };
        return moldura;
    }

    private static Brush CorResumo(string classeCss) => classeCss switch
    {
        "resumo-ok" => new SolidColorBrush(Color.FromRgb(0x0F, 0x76, 0x6E)),
        "resumo-informativo" => new SolidColorBrush(Color.FromRgb(0x15, 0x5E, 0x75)),
        _ => new SolidColorBrush(Color.FromRgb(0xB9, 0x1C, 0x1C))
    };

    private UIElement PrioridadeResumo(string categoria, string titulo, string detalhe, string classeCss)
    {
        var categoriaLabel = EtiquetaApoio.CriarEtiqueta(categoria, classeCss);
        var tituloLabel = UiApoio.Texto(titulo, 13, FontWeights.Black, (Brush)Application.Current.Resources["TextoCelulaBrush"]);
        var detalheLabel = UiApoio.Texto(detalhe, 12, FontWeights.Normal, (Brush)Application.Current.Resources["TextoSuaveBrush"]);
        var textos = UiApoio.Coluna(2, tituloLabel, detalheLabel);

        var linha = UiApoio.LinhaComCrescimento(10, categoriaLabel, textos);
        linha.Margin = new Thickness(0, 0, 0, 6);
        return new Border { Background = new SolidColorBrush(Color.FromRgb(0x11, 0x1A, 0x29)), CornerRadius = new CornerRadius(7), Padding = new Thickness(10, 9, 10, 9), Child = linha };
    }

    private UIElement Indicador(string titulo, string valor, string classeCss)
    {
        var tituloLabel = UiApoio.Texto(titulo, 12, FontWeights.Bold, (Brush)Application.Current.Resources["TextoSuaveBrush"]);
        var valorLabel = UiApoio.Texto(valor, 28, FontWeights.Black, (Brush)Application.Current.Resources["TextoTituloBrush"]);
        var caixa = UiApoio.Coluna(4, tituloLabel, valorLabel);

        return new Border
        {
            Background = (Brush)Application.Current.Resources["CardFundoBrush"],
            BorderBrush = CorIndicador(classeCss),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14),
            Width = 190,
            Margin = new Thickness(0, 0, 12, 12),
            Effect = (System.Windows.Media.Effects.Effect)Application.Current.Resources["SombraCard"],
            Child = caixa
        };
    }

    private static Brush CorIndicador(string classeCss) => classeCss switch
    {
        "indicador-informativo" => new SolidColorBrush(Color.FromRgb(0x15, 0x5E, 0x75)),
        "indicador-aviso" => new SolidColorBrush(Color.FromRgb(0x92, 0x40, 0x0E)),
        "indicador-perigo" => new SolidColorBrush(Color.FromRgb(0x7F, 0x1D, 0x1D)),
        _ => new SolidColorBrush(Color.FromRgb(0x2A, 0x3A, 0x53))
    };

    private sealed record ItemNavegacao(string Titulo, string Descricao, UIElement Conteudo);
}
