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

    private readonly ContextoAplicacao _contexto;
    private readonly Action<string> _irParaModulo;
    private readonly Grid _painelOperacao = new();
    private readonly StackPanel _painelAcoes = new();
    private readonly ContentControl _faixaIndicadoresSlot = new();
    private readonly TextBlock _contadorAcoes = new();
    private readonly TextBlock _horarioVerificacao = new();
    private readonly FaixaAviso _avisoBackup;

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

        // Uma lista só, por prazo, com tudo o que pede ação (antes eram "Alertas pendentes" e "Prioridades" lado a lado,
        // quase iguais e cortadas em 8 e 6 itens).
        var linkVerTodos = new TextBlock { Text = "Ver alertas", Style = (Style)Application.Current.Resources["PanelLinkStyle"], Cursor = Cursors.Hand };
        linkVerTodos.MouseLeftButtonUp += (_, _) => _irParaModulo("Alertas");
        var painelInferior = PainelListaCard("O que fazer agora", _contadorAcoes, linkVerTodos, _painelAcoes);

        _avisoBackup = new FaixaAviso("Ir para Backup", () => _irParaModulo("Backup"));

        var raiz = new Grid { Margin = new Thickness(18, 16, 20, 18) };
        raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        raiz.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(cabecalho, 0);
        Grid.SetRow(_avisoBackup.Raiz, 1);
        Grid.SetRow(_painelOperacao, 2);
        Grid.SetRow(_faixaIndicadoresSlot, 3);
        Grid.SetRow(painelInferior, 4);
        raiz.Children.Add(cabecalho);
        raiz.Children.Add(_avisoBackup.Raiz);
        raiz.Children.Add(_painelOperacao);
        raiz.Children.Add(_faixaIndicadoresSlot);
        raiz.Children.Add(painelInferior);
        Conteudo = raiz;
    }

    private void AtualizarAvisoBackup(bool temDados)
    {
        var aviso = Aplicacao.CasoDeUso.LembreteBackup.Avaliar(_contexto.ObterConfiguracaoBackup().Executar(), DateTimeOffset.Now, temDados,
            _contexto.FalhaBackupAutomatico);
        _avisoBackup.Mostrar(aviso?.Mensagem, aviso?.Critico ?? false);
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
        string ModuloDoAlerta(Alerta alerta) => Visao.ModuloDoAlerta.Titulo(alerta, veiculos, imoveis);

        var garantiasVencidas = garantias.Count(g => g.StatusEm(hoje) == StatusGarantia.Vencida);
        var garantiasProximas = garantias.Count(g => g.StatusEm(hoje) == StatusGarantia.PertoDoVencimento);
        var manutencoesAtrasadas = manutencoes.Count(m => m.StatusEm(hoje) == StatusManutencao.Atrasada);
        var conveniosAVencer = convenios.Count(s => s.StatusEm(hoje) is StatusSaude.Vencida or StatusSaude.PertoDoVencimento);
        var documentosAusentes = documentos.Where(d => d.Status == StatusDocumento.Ausente && emUso.Contains(d.BemId)).ToList();
        var pendentes = alertas.Where(a => a.EstaAcionavel()).ToList();
        var semNotaFiscal = pendentes.Count(a => a.Tipo == TipoAlerta.DocumentoAusente);

        var acoes = pendentes
            .Select(a => new Prioridade(CategoriaAlerta(a.Tipo), nomeBem(a.BemId), a.Mensagem, Formatador.ClasseEtiqueta(a.Severidade), a.PrazoEm,
                (int)a.Severidade, ModuloDoAlerta(a)))
            .Concat(documentosAusentes.Select(d => new Prioridade("Arquivo", nomeBem(d.BemId), $"{d.Nome.Valor}: arquivo não encontrado",
                Formatador.ClasseEtiqueta(StatusDocumento.Ausente), null, (int)SeveridadeAlerta.Aviso, "Documentos")))
            .ToList();
        var vencidas = acoes.Count(a => GrupoPrazo(a.Prazo, hoje) == 0);

        _horarioVerificacao.Text = $"verificado {Formatador.DataHora(DateTime.Now)}";
        AtualizarAvisoBackup(bens.Count > 0 || documentos.Count > 0);

        var bensSemPessoas = bens.Where(b => b.Tipo != TipoBem.Pessoa).ToList();
        var emUsoSemPessoas = bensSemPessoas.Count(b => b.Status is StatusBem.Ativo or StatusBem.EmManutencao);
        _painelOperacao.Children.Clear();
        AdicionarResumoOperacao(0, "O que fazer agora", acoes.Count.ToString(),
            acoes.Count == 0 ? "tudo em dia" : acoes.Count == 1 ? "item pede ação" : "itens pedem ação", acoes.Count == 0 ? "StatusOk" : "StatusAviso");
        AdicionarResumoOperacao(1, "Vencidos", vencidas.ToString(),
            vencidas == 0 ? "nada vencido" : vencidas == 1 ? "prazo já passou" : "prazos já passaram", vencidas == 0 ? "StatusOk" : "StatusPerigo");
        AdicionarResumoOperacao(2, "Bens em uso", emUsoSemPessoas.ToString(),
            $"de {bensSemPessoas.Count} cadastrado{(bensSemPessoas.Count == 1 ? "" : "s")} · {documentos.Count} documento{(documentos.Count == 1 ? "" : "s")}", "StatusInfo");

        _faixaIndicadoresSlot.Content = FaixaIndicadores(
            ("Garantias vencidas", garantiasVencidas, garantiasVencidas > 0, "StatusPerigo"),
            ("Garantias a vencer", garantiasProximas, garantiasProximas > 0, "StatusAviso"),
            ("Manutenções atrasadas", manutencoesAtrasadas, manutencoesAtrasadas > 0, "StatusPerigo"),
            ("Convênios a vencer", conveniosAVencer, conveniosAVencer > 0, "StatusAviso"),
            ("Contas vencidas", contasVencidas, contasVencidas > 0, "StatusPerigo"),
            ("Sem nota fiscal", semNotaFiscal, semNotaFiscal > 0, "StatusAviso"));

        _contadorAcoes.Text = acoes.Count.ToString();
        _painelAcoes.Children.Clear();
        foreach (var grupo in acoes.GroupBy(a => GrupoPrazo(a.Prazo, hoje)).OrderBy(g => g.Key))
        {
            _painelAcoes.Children.Add(TituloGrupo($"{NomesGrupos[grupo.Key]} · {grupo.Count()}"));
            foreach (var acao in grupo.OrderByDescending(a => a.Severidade).ThenBy(a => a.Prazo ?? DateOnly.MaxValue).ThenBy(a => a.Bem, StringComparer.CurrentCulture))
            {
                _painelAcoes.Children.Add(LinhaPrioridade(acao));
            }
        }

        if (_painelAcoes.Children.Count == 0)
        {
            _painelAcoes.Children.Add(TextoVazio("Nada pendente. Garantias, contas, manutenções e documentos estão em dia."));
        }
    }

    private static readonly string[] NomesGrupos = ["Vencido", "Próximos 7 dias", "Próximos 30 dias", "Mais adiante", "Sem prazo"];

    /// <summary>0 = vencido, 1 = próximos 7 dias, 2 = próximos 30 dias, 3 = depois, 4 = sem prazo.</summary>
    internal static int GrupoPrazo(DateOnly? prazo, DateOnly hoje) => prazo switch
    {
        null => 4,
        { } data when data < hoje => 0,
        { } data when data <= hoje.AddDays(7) => 1,
        { } data when data <= hoje.AddDays(30) => 2,
        _ => 3
    };

    private static TextBlock TituloGrupo(string texto) => new()
    {
        Text = texto.ToUpperInvariant(),
        FontFamily = (FontFamily)Application.Current.Resources["FontMono"],
        FontSize = 10.5,
        FontWeight = FontWeights.SemiBold,
        Foreground = (Brush)Application.Current.Resources["TextLabel"],
        Margin = new Thickness(14, 12, 14, 4)
    };

    private sealed record Prioridade(string Categoria, string Bem, string Detalhe, string ClasseCss, DateOnly? Prazo, int Severidade, string Modulo);

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
