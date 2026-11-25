using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Despesa;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Manutencao;
using Assetly.Dominio.Saude;
using static Assetly.Apresentacao.Visao.ContextoApoio;
using static Assetly.Apresentacao.Visao.LayoutApoio;

namespace Assetly.Apresentacao.Visao;

/// <summary>
/// Resumo operacional. Os números consideram só bens em uso (ativos ou em manutenção) — garantia
/// vencida de um item descartado não é pendência. "Atenção necessária" conta alertas pendentes e
/// documentos ausentes; garantias/manutenções/convênios com problema já viram alerta automático,
/// então somá-los de novo contaria a mesma pendência duas vezes.
/// </summary>
internal sealed class ModuloPainel : IModulo
{
    private const int LimiteAlertas = 8;
    private const int LimitePrioridades = 6;

    private readonly ContextoAplicacao _contexto;
    private readonly Action<string> _irParaModulo;
    private readonly Grid _painelOperacao = new();
    private readonly StackPanel _painelAlertas = new();
    private readonly StackPanel _painelAcoes = new();
    private readonly ContentControl _faixaIndicadoresSlot = new();
    private readonly TextBlock _contadorAlertasPainel = new();
    private readonly TextBlock _contadorPrioridadesPainel = new();
    private readonly TextBlock _horarioVerificacao = new();
    private readonly Border _avisoBackup = new();
    private readonly TextBlock _textoAvisoBackup = new() { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, FontSize = 12.5 };

    public string Titulo => "Painel";
    public string Descricao => "Resumo operacional";
    public bool ComContador => false;
    public UIElement Conteudo { get; }
    public int Contador => 0;

    internal ModuloPainel(ContextoAplicacao contexto, Action<string> irParaModulo)
    {
        _contexto = contexto;
        _irParaModulo = irParaModulo;

        _horarioVerificacao.FontFamily = (FontFamily)Application.Current.Resources["FontMono"];
        _horarioVerificacao.FontSize = 11;
        _horarioVerificacao.Foreground = (Brush)Application.Current.Resources["TextMuted"];
        _horarioVerificacao.VerticalAlignment = VerticalAlignment.Center;
        // Notificar dispara o ciclo completo da tela principal: reprocessa os alertas automáticos e
        // atualiza todos os módulos (antes, este botão só redesenhava o painel).
        var reprocessar = AcoesApoio.BotaoSecundario("Reprocessar alertas", _contexto.NotificarAlteracao);
        var direitaCabecalho = UiApoio.Linha(12, _horarioVerificacao, reprocessar);
        var cabecalho = CabecalhoTela("Visão geral", "Indicadores críticos para acompanhar a saúde dos ativos.", direitaCabecalho);
        cabecalho.Margin = new Thickness(0, 0, 0, 14);

        for (var i = 0; i < 3; i++)
        {
            _painelOperacao.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        _painelOperacao.Margin = new Thickness(0, 0, 0, 14);
        _faixaIndicadoresSlot.Margin = new Thickness(0, 0, 0, 14);

        var linkVerTodos = new TextBlock { Text = "Ver todos", Style = (Style)Application.Current.Resources["PanelLinkStyle"], Cursor = Cursors.Hand };
        linkVerTodos.MouseLeftButtonUp += (_, _) => _irParaModulo("Alertas");
        var painelAlertasBorda = PainelListaCard("Alertas pendentes", _contadorAlertasPainel, linkVerTodos, _painelAlertas);

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

        var irParaBackup = AcoesApoio.BotaoSecundario("Ir para Backup", () => _irParaModulo("Backup"));
        _avisoBackup.BorderThickness = new Thickness(1);
        _avisoBackup.CornerRadius = new CornerRadius(8);
        _avisoBackup.Padding = new Thickness(14, 10, 10, 10);
        _avisoBackup.Margin = new Thickness(0, 0, 0, 14);
        _avisoBackup.Visibility = Visibility.Collapsed;
        var linhaAviso = new Grid();
        linhaAviso.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        linhaAviso.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        irParaBackup.Margin = new Thickness(12, 0, 0, 0);
        Grid.SetColumn(irParaBackup, 1);
        linhaAviso.Children.Add(_textoAvisoBackup);
        linhaAviso.Children.Add(irParaBackup);
        _avisoBackup.Child = linhaAviso;
        System.Windows.Automation.AutomationProperties.SetLiveSetting(_textoAvisoBackup, System.Windows.Automation.AutomationLiveSetting.Polite);

        var raiz = new Grid { Margin = new Thickness(18, 16, 20, 18) };
        raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        raiz.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(cabecalho, 0);
        Grid.SetRow(_avisoBackup, 1);
        Grid.SetRow(_painelOperacao, 2);
        Grid.SetRow(_faixaIndicadoresSlot, 3);
        Grid.SetRow(painelInferior, 4);
        raiz.Children.Add(cabecalho);
        raiz.Children.Add(_avisoBackup);
        raiz.Children.Add(_painelOperacao);
        raiz.Children.Add(_faixaIndicadoresSlot);
        raiz.Children.Add(painelInferior);
        Conteudo = raiz;
    }

    private void AtualizarAvisoBackup(bool temDados)
    {
        var aviso = Aplicacao.CasoDeUso.LembreteBackup.Avaliar(_contexto.ObterConfiguracaoBackup().Executar(), DateTimeOffset.Now, temDados,
            _contexto.FalhaBackupAutomatico);
        _avisoBackup.Visibility = aviso is null ? Visibility.Collapsed : Visibility.Visible;
        if (aviso is null)
        {
            return;
        }

        var recursos = Application.Current.Resources;
        var prefixo = aviso.Critico ? "StatusPerigo" : "StatusAviso";
        _avisoBackup.Background = (Brush)recursos[prefixo + "Bg"];
        _avisoBackup.BorderBrush = (Brush)recursos[prefixo + "Border"];
        _textoAvisoBackup.Foreground = (Brush)recursos[prefixo + "Text"];
        _textoAvisoBackup.Text = aviso.Mensagem;
    }

    public void Atualizar()
    {
        var hoje = Hoje();
        var bens = _contexto.ListarBens().Executar();
        var emUso = bens.Where(b => b.Status is StatusBem.Ativo or StatusBem.EmManutencao).Select(b => b.Id).ToHashSet();
        var nomeBem = NomesBens(_contexto);
        var garantias = _contexto.ListarGarantias().Executar().Where(g => emUso.Contains(g.BemId)).ToList();
        var manutencoes = _contexto.ListarManutencoes().Executar().Where(m => emUso.Contains(m.BemId)).ToList();
        var convenios = _contexto.ListarSaude().Executar().Where(s => emUso.Contains(s.BemId)).ToList();
        var documentos = _contexto.ListarDocumentos().Executar();
        var alertas = _contexto.ListarAlertas().Executar();
        var contasVencidas = _contexto.ListarDespesas().Executar().Count(d => d.StatusEm(hoje) == StatusDespesa.Vencida);
        var veiculos = _contexto.ListarVeiculos().Executar().Select(v => v.BemId).ToHashSet();
        var imoveis = _contexto.ListarImoveis().Executar().Select(i => i.BemId).ToHashSet();
        string ModuloDoAlerta(Alerta alerta) => alerta.Tipo switch
        {
            TipoAlerta.GarantiaVencendo or TipoAlerta.GarantiaVencida => "Garantias",
            TipoAlerta.ManutencaoProxima or TipoAlerta.ManutencaoAtrasada => "Manutenções",
            TipoAlerta.ConvenioVencendo or TipoAlerta.ConvenioVencido => "Convênios",
            TipoAlerta.DocumentoAusente => "Documentos",
            TipoAlerta.LicenciamentoVencendo or TipoAlerta.LicenciamentoVencido or TipoAlerta.RevisaoVeiculo => ModulosVeiculoImovel.TituloAutomoveis,
            TipoAlerta.ContratoAluguel => ModulosVeiculoImovel.TituloImoveis,
            TipoAlerta.SeguroVencendo or TipoAlerta.SeguroVencido or TipoAlerta.ContaVencendo or TipoAlerta.ContaVencida
                => veiculos.Contains(alerta.BemId) ? ModulosVeiculoImovel.TituloAutomoveis
                : imoveis.Contains(alerta.BemId) ? ModulosVeiculoImovel.TituloImoveis : "Alertas",
            _ => "Alertas"
        };

        var garantiasVencidas = garantias.Count(g => g.StatusEm(hoje) == StatusGarantia.Vencida);
        var garantiasProximas = garantias.Count(g => g.StatusEm(hoje) == StatusGarantia.PertoDoVencimento);
        var manutencoesAtrasadas = manutencoes.Count(m => m.StatusEm(hoje) == StatusManutencao.Atrasada);
        var conveniosAVencer = convenios.Count(s => s.StatusEm(hoje) is StatusSaude.Vencida or StatusSaude.PertoDoVencimento);
        var documentosAusentes = documentos.Where(d => d.Status == StatusDocumento.Ausente && emUso.Contains(d.BemId)).ToList();
        var pendentes = alertas.Where(a => a.EstaAcionavel()).ToList();
        var alertasCriticos = pendentes.Count(a => a.Severidade == SeveridadeAlerta.Critico);
        var pontosAtencao = pendentes.Count + documentosAusentes.Count;

        _horarioVerificacao.Text = $"verificado {Formatador.DataHora(DateTime.Now)}";
        AtualizarAvisoBackup(bens.Count > 0 || documentos.Count > 0);

        _painelOperacao.Children.Clear();
        AdicionarResumoOperacao(0, "Atenção necessária", pontosAtencao.ToString(),
            pontosAtencao == 0 ? "tudo em dia" : pontosAtencao == 1 ? "item pede revisão" : "itens pedem revisão", pontosAtencao == 0 ? "StatusOk" : "StatusAviso");
        AdicionarResumoOperacao(1, "Alertas críticos", alertasCriticos.ToString(),
            alertasCriticos == 0 ? "sem pendências críticas" : alertasCriticos == 1 ? "alerta crítico pendente" : "alertas críticos pendentes",
            alertasCriticos == 0 ? "StatusOk" : "StatusPerigo");
        AdicionarResumoOperacao(2, "Bens em uso", emUso.Count.ToString(),
            $"de {bens.Count} cadastrado{(bens.Count == 1 ? "" : "s")} · {documentos.Count} documento{(documentos.Count == 1 ? "" : "s")}", "StatusInfo");

        _faixaIndicadoresSlot.Content = FaixaIndicadores(
            ("Garantias vencidas", garantiasVencidas, garantiasVencidas > 0, "StatusPerigo"),
            ("Garantias a vencer", garantiasProximas, garantiasProximas > 0, "StatusAviso"),
            ("Manutenções atrasadas", manutencoesAtrasadas, manutencoesAtrasadas > 0, "StatusPerigo"),
            ("Convênios a vencer", conveniosAVencer, conveniosAVencer > 0, "StatusAviso"),
            ("Contas vencidas", contasVencidas, contasVencidas > 0, "StatusPerigo"),
            ("Documentos ausentes", documentosAusentes.Count, documentosAusentes.Count > 0, "StatusAviso"));

        _contadorAlertasPainel.Text = pendentes.Count.ToString();
        _painelAlertas.Children.Clear();
        foreach (var alerta in pendentes
                     .OrderBy(a => a.Severidade switch { SeveridadeAlerta.Critico => 0, SeveridadeAlerta.Aviso => 1, _ => 2 })
                     .ThenBy(a => a.PrazoEm ?? DateOnly.MaxValue)
                     .Take(LimiteAlertas))
        {
            _painelAlertas.Children.Add(LinhaAlerta(alerta, nomeBem(alerta.BemId)));
        }

        if (_painelAlertas.Children.Count == 0)
        {
            _painelAlertas.Children.Add(TextoVazio("Nenhum alerta pendente."));
        }

        var prioridades = new List<Prioridade>();
        foreach (var alerta in pendentes.Where(a => a.Severidade is SeveridadeAlerta.Critico or SeveridadeAlerta.Aviso))
        {
            prioridades.Add(new Prioridade(CategoriaAlerta(alerta.Tipo), nomeBem(alerta.BemId), alerta.Mensagem,
                Formatador.ClasseEtiqueta(alerta.Severidade), alerta.PrazoEm, alerta.PrazoEm ?? hoje, ModuloDoAlerta(alerta)));
        }

        foreach (var documento in documentosAusentes)
        {
            prioridades.Add(new Prioridade("Documento", nomeBem(documento.BemId), $"{documento.Nome.Valor} (ausente)",
                Formatador.ClasseEtiqueta(StatusDocumento.Ausente), null, hoje, "Documentos"));
        }

        var prioridadesOrdenadas = prioridades.OrderBy(p => p.Ordenacao).ThenBy(p => p.Bem, StringComparer.CurrentCulture).Take(LimitePrioridades).ToList();
        _contadorPrioridadesPainel.Text = prioridades.Count.ToString();
        _painelAcoes.Children.Clear();
        foreach (var prioridade in prioridadesOrdenadas)
        {
            _painelAcoes.Children.Add(LinhaPrioridade(prioridade));
        }

        if (_painelAcoes.Children.Count == 0)
        {
            _painelAcoes.Children.Add(TextoVazio("Nenhuma prioridade no momento."));
        }
    }

    private sealed record Prioridade(string Categoria, string Bem, string Detalhe, string ClasseCss, DateOnly? Prazo, DateOnly Ordenacao, string Modulo);

    private static string CategoriaAlerta(TipoAlerta tipo) => tipo switch
    {
        TipoAlerta.GarantiaVencendo or TipoAlerta.GarantiaVencida => "Garantia",
        TipoAlerta.ManutencaoProxima or TipoAlerta.ManutencaoAtrasada => "Manutenção",
        TipoAlerta.ConvenioVencendo or TipoAlerta.ConvenioVencido => "Convênio",
        TipoAlerta.DocumentoAusente => "Nota fiscal",
        TipoAlerta.LicenciamentoVencendo or TipoAlerta.LicenciamentoVencido => "Licenciamento",
        TipoAlerta.SeguroVencendo or TipoAlerta.SeguroVencido => "Seguro",
        TipoAlerta.RevisaoVeiculo => "Revisão",
        TipoAlerta.ContaVencendo or TipoAlerta.ContaVencida => "Conta",
        TipoAlerta.ContratoAluguel => "Aluguel",
        _ => "Alerta"
    };

    private void AdicionarResumoOperacao(int coluna, string titulo, string valor, string detalhe, string prefixoStatus)
    {
        var card = ResumoOperacao(titulo, valor, detalhe, prefixoStatus);
        card.Margin = coluna < 2 ? new Thickness(0, 0, 12, 0) : new Thickness(0);
        Grid.SetColumn(card, coluna);
        _painelOperacao.Children.Add(card);
    }

    /// <summary>Linha clicável: leva ao módulo onde a pendência é resolvida.</summary>
    private Border LinhaClicavel(UIElement conteudo, string modulo, string dica)
    {
        var borda = new Border
        {
            BorderBrush = (Brush)Application.Current.Resources["RowDivider"],
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(14, 0, 14, 0),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            ToolTip = dica,
            Child = conteudo
        };
        borda.MouseEnter += (_, _) => borda.Background = (Brush)Application.Current.Resources["BgSurface2"];
        borda.MouseLeave += (_, _) => borda.Background = Brushes.Transparent;
        borda.MouseLeftButtonUp += (_, _) => _irParaModulo(modulo);
        return borda;
    }

    private UIElement LinhaAlerta(Alerta alerta, string nomeBem)
    {
        var recursos = Application.Current.Resources;
        var badge = EtiquetaApoio.CriarEtiqueta(Formatador.TextoEnum(alerta.Severidade), Formatador.ClasseEtiqueta(alerta.Severidade));
        badge.VerticalAlignment = VerticalAlignment.Center;

        var nomeBemLabel = new TextBlock
        {
            Text = nomeBem,
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

        return LinhaClicavel(grade, "Alertas", $"{nomeBem}: {alerta.Mensagem}\nClique para abrir Alertas.");
    }

    private UIElement LinhaPrioridade(Prioridade prioridade)
    {
        var recursos = Application.Current.Resources;
        var badge = EtiquetaApoio.CriarEtiqueta(prioridade.Categoria, prioridade.ClasseCss);
        badge.VerticalAlignment = VerticalAlignment.Center;

        var bemLabel = new TextBlock
        {
            Text = prioridade.Bem,
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)recursos["TextBody"],
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var detalheLabel = new TextBlock
        {
            Text = prioridade.Detalhe,
            FontSize = 11.5,
            Foreground = (Brush)recursos["TextLabel"],
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var textos = UiApoio.Coluna(2, bemLabel, detalheLabel);
        textos.VerticalAlignment = VerticalAlignment.Center;
        textos.Margin = new Thickness(10, 0, 10, 0);

        var prazoLabel = new TextBlock
        {
            Text = Formatador.Data(prioridade.Prazo),
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

        return LinhaClicavel(grade, prioridade.Modulo, $"{prioridade.Bem}: {prioridade.Detalhe}\nClique para abrir {prioridade.Modulo}.");
    }

    private static TextBlock TextoVazio(string texto) => new()
    {
        Text = texto,
        FontSize = 13,
        Foreground = (Brush)Application.Current.Resources["TextSecondary"],
        Margin = new Thickness(14, 12, 14, 12)
    };
}
