using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Assetly.Aplicacao.Dados;
using Assetly.Dominio.Preferencias;
using static Assetly.Apresentacao.Visao.AcoesApoio;
using static Assetly.Apresentacao.Visao.DialogoApoio;
using static Assetly.Apresentacao.Visao.LayoutApoio;

namespace Assetly.Apresentacao.Visao;

/// <summary>Módulo fixo na barra lateral (não aparece na lista reordenável/ocultável de
/// <see cref="ConfiguracaoPreferencias"/> — <see cref="TelaPrincipal"/> sempre o anexa por último).
/// Tema/cor/densidade pedem reconstrução da árvore visual para refletir em toda a janela (ver
/// AparenciaApoio); ordem/visibilidade/módulo inicial só valem no próximo lançamento; intervalo de
/// atualização aplica ao vivo — ver a nota de cada card abaixo.</summary>
internal sealed class ModuloConfiguracoes : IModulo
{
    private readonly ContextoAplicacao _contexto;
    private readonly Action _aplicarAparenciaEReconstruir;
    private readonly Action<TimeSpan> _aplicarIntervaloAtualizacao;
    private readonly ContentControl _modulosSlot = new();
    private readonly Action _atualizarTela;
    private readonly GerenciadorAtualizacoes? _atualizacoes;
    private Action? _atualizarVersao;

    public string Titulo => "Configurações";
    public string Descricao => "Aparência e módulos";
    public bool ComContador => false;
    public UIElement Conteudo { get; }
    public int Contador => 0;

    internal ModuloConfiguracoes(ContextoAplicacao contexto, Action aplicarAparenciaEReconstruir, Action<TimeSpan> aplicarIntervaloAtualizacao,
        GerenciadorAtualizacoes? atualizacoes = null)
    {
        _contexto = contexto;
        _aplicarAparenciaEReconstruir = aplicarAparenciaEReconstruir;
        _aplicarIntervaloAtualizacao = aplicarIntervaloAtualizacao;
        _atualizacoes = atualizacoes;

        var preferenciasIniciais = _contexto.ObterPreferencias().Executar();

        var temaCombo = Combo(preferenciasIniciais.Tema);
        temaCombo.SelectionChanged += (_, _) => ExecutarComTratamento(() =>
        {
            _contexto.DefinirTema().Executar(new DefinirTemaComando(ValorEnum<TemaVisual>(temaCombo)));
            _aplicarAparenciaEReconstruir();
        });

        var corCombo = Combo(preferenciasIniciais.CorDestaque);
        corCombo.SelectionChanged += (_, _) => ExecutarComTratamento(() =>
        {
            _contexto.DefinirCorDestaque().Executar(new DefinirCorDestaqueComando(ValorEnum<CorDestaque>(corCombo)));
            _aplicarAparenciaEReconstruir();
        });

        var densidadeCombo = Combo(preferenciasIniciais.DensidadeTabela);
        densidadeCombo.SelectionChanged += (_, _) => ExecutarComTratamento(() =>
        {
            _contexto.DefinirDensidadeTabela().Executar(new DefinirDensidadeTabelaComando(ValorEnum<DensidadeTabela>(densidadeCombo)));
            _aplicarAparenciaEReconstruir();
        });

        var grade = GradeFormulario();
        AdicionarCampo(grade, 0, 0, "Tema", temaCombo);
        AdicionarCampo(grade, 0, 1, "Cor de destaque", corCombo);
        AdicionarCampo(grade, 1, 0, "Densidade da tabela", densidadeCombo);
        var cardAparencia = CardConfiguracao("Aparência", grade);

        var cardModulos = CardConfiguracao("Módulos", _modulosSlot);

        var intervaloCombo = Combo(preferenciasIniciais.IntervaloAtualizacao);
        intervaloCombo.SelectionChanged += (_, _) => ExecutarComTratamento(() =>
        {
            var atualizado = _contexto.DefinirIntervaloAtualizacao().Executar(new DefinirIntervaloAtualizacaoComando(ValorEnum<IntervaloAtualizacao>(intervaloCombo)));
            _aplicarIntervaloAtualizacao(atualizado.IntervaloComoTimeSpan());
        });
        intervaloCombo.Width = 200;
        intervaloCombo.HorizontalAlignment = HorizontalAlignment.Left;
        var cardAtualizacao = CardConfiguracao("Atualização dos dados na tela", intervaloCombo);

        var cabecalho = CabecalhoTela("Configurações", "Personalize aparência, navegação e comportamento do Assetly.", null);
        cabecalho.Margin = new Thickness(0, 0, 0, 14);

        var cartoes = UiApoio.Coluna(14, CardVersao(preferenciasIniciais.AtualizarAutomaticamente), cardAparencia, cardModulos, cardAtualizacao, CardDadosLocais());

        var rolagem = new ScrollViewer { Content = cartoes, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var raiz = new Grid { Margin = new Thickness(18, 16, 20, 18) };
        raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        raiz.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(cabecalho, 0);
        Grid.SetRow(rolagem, 1);
        raiz.Children.Add(cabecalho);
        raiz.Children.Add(rolagem);
        Conteudo = raiz;

        _atualizarTela = () => _modulosSlot.Content = PainelModulos(_contexto.ObterPreferencias().Executar());
        _atualizarTela();
    }

    public void Atualizar() => _atualizarVersao?.Invoke();

    /// <summary>Versão instalada e atualizações do programa: estado atual, verificação manual, reinício
    /// para aplicar uma versão baixada e a opção de desligar a atualização automática.</summary>
    private Border CardVersao(bool automaticaAtiva)
    {
        var recursos = Application.Current.Resources;
        var versao = new TextBlock
        {
            FontFamily = (FontFamily)recursos["FontMono"],
            FontSize = 12,
            Foreground = (Brush)recursos["TextBody"],
            Text = $"Versão instalada: {_atualizacoes?.VersaoAtual ?? "desenvolvimento"}"
        };
        var estado = UiApoio.Texto(string.Empty, 12, FontWeights.Normal, (Brush)recursos["TextSecondary"]);

        var automatica = CaixaMarcacao("Procurar e baixar versões novas automaticamente (ao abrir e a cada 6 horas)", automaticaAtiva);
        automatica.Click += (_, _) => ExecutarComTratamento(() => _contexto.DefinirAtualizacaoAutomatica().Executar(automatica.IsChecked == true));

        var procurar = BotaoSecundario("Procurar atualizações agora", () => { });
        procurar.Click += async (_, _) =>
        {
            if (_atualizacoes is null)
            {
                return;
            }

            procurar.IsEnabled = false;
            try
            {
                await _atualizacoes.VerificarAsync(manual: true);
                if (_atualizacoes.Estado == EstadoAtualizacao.Atualizado)
                {
                    MostrarInformacao("Atualizações", $"Você já está na versão mais recente ({_atualizacoes.VersaoAtual}).");
                }
            }
            catch (Exception excecao) when (excecao is not OutOfMemoryException)
            {
                MostrarErro(excecao);
            }
            finally
            {
                procurar.IsEnabled = true;
                _atualizarVersao?.Invoke();
            }
        };

        var reiniciar = new Button { Content = "Reiniciar e atualizar", Style = (Style)recursos["BotaoPrimarioStyle"] };
        reiniciar.Click += (_, _) => ExecutarComTratamento(() => ((App)Application.Current).ConfirmarReinicioParaAtualizar());

        var novidades = BotaoSecundario("Ver versões publicadas", () =>
            Process.Start(new ProcessStartInfo(GerenciadorAtualizacoes.PaginaReleases) { UseShellExecute = true }));

        var botoes = UiApoio.Linha(10, procurar, reiniciar, novidades);
        _atualizarVersao = () =>
        {
            var gerenciador = _atualizacoes;
            estado.Text = gerenciador?.DescricaoEstado() ?? "Atualização automática disponível apenas na versão instalada pelo instalador.";
            var disponivel = gerenciador?.Disponivel == true;
            automatica.IsEnabled = disponivel;
            procurar.IsEnabled = gerenciador is not null && gerenciador.Estado is not (EstadoAtualizacao.Procurando or EstadoAtualizacao.Baixando);
            reiniciar.Visibility = gerenciador?.Estado == EstadoAtualizacao.Pronta ? Visibility.Visible : Visibility.Collapsed;
        };
        _atualizarVersao();

        if (_atualizacoes is not null)
        {
            // O gerenciador vive mais que esta tela (a tela é recriada ao mudar a aparência), então a
            // inscrição é desfeita quando o card sai da árvore visual.
            Action aoMudar = () => _atualizarVersao?.Invoke();
            _atualizacoes.Mudou += aoMudar;
            estado.Unloaded += (_, _) => _atualizacoes.Mudou -= aoMudar;
            estado.Loaded += (_, _) =>
            {
                _atualizacoes.Mudou -= aoMudar;
                _atualizacoes.Mudou += aoMudar;
            };
        }

        return CardConfiguracao("Versão e atualizações", UiApoio.Coluna(10, versao, estado, automatica, botoes));
    }

    /// <summary>Onde os dados deste ambiente ficam no disco — para o usuário saber o que proteger,
    /// copiar ou apagar, sem precisar procurar na documentação.</summary>
    private Border CardDadosLocais()
    {
        var linhas = UiApoio.Coluna(8,
            LinhaPasta("Banco de dados", Path.GetDirectoryName(_contexto.ArquivoBanco)!, _contexto.ArquivoBanco),
            LinhaPasta("Documentos", _contexto.DiretorioDocumentos, _contexto.DiretorioDocumentos),
            LinhaPasta("Registro de erros", RegistroErros.DiretorioLogs, RegistroErros.DiretorioLogs));
        var nota = UiApoio.Texto($"Ambiente atual: {_contexto.RotuloAmbiente}. Nada é enviado para a internet — tudo fica neste computador.",
            11.5, FontWeights.Normal, (Brush)Application.Current.Resources["TextMuted"]);

        var exportar = BotaoSecundario("Exportar todos os dados (JSON)…", ExportarJson);
        exportar.HorizontalAlignment = HorizontalAlignment.Left;
        exportar.ToolTip = "Gera um arquivo JSON legível com todos os cadastros (bens, garantias, manutenções, documentos, saúde, veículos, imóveis, contas e alertas) — para planilhas, outros programas ou arquivo pessoal.";
        var importar = BotaoSecundario("Importar dados (JSON)…", () => ImportacaoApoio.ImportarJson(_contexto));
        importar.ToolTip = "Traz de volta um arquivo gerado por \"Exportar todos os dados\": registros novos são criados e os existentes, atualizados. Nada é apagado.";
        var botoes = UiApoio.Linha(8, exportar, importar);
        botoes.HorizontalAlignment = HorizontalAlignment.Left;
        return CardConfiguracao("Dados locais", UiApoio.Coluna(12, linhas, nota, botoes));
    }

    private void ExportarJson()
    {
        var dialogo = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Exportar dados do Assetly",
            FileName = $"Assetly - dados - {DateTime.Now:yyyy-MM-dd}.json",
            DefaultExt = ".json",
            Filter = "JSON (*.json)|*.json",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dialogo.ShowDialog(Application.Current.MainWindow) != true)
        {
            return;
        }

        File.WriteAllText(dialogo.FileName, _contexto.ExportarDados().Executar(), new System.Text.UTF8Encoding(false));
        MostrarInformacao("Exportação concluída", $"Todos os dados foram exportados para:\n{dialogo.FileName}\n\n" +
            "Os arquivos dos documentos não vão no JSON (só o nome) — para uma cópia completa, use o Backup.");
    }

    private static UIElement LinhaPasta(string rotulo, string pasta, string caminhoExibido)
    {
        var recursos = Application.Current.Resources;
        var rotuloTexto = new TextBlock { Text = rotulo.ToUpperInvariant(), Style = (Style)recursos["RotuloKpiStyle"], Width = 150, VerticalAlignment = VerticalAlignment.Center };
        var caminhoTexto = new TextBlock
        {
            Text = caminhoExibido,
            ToolTip = caminhoExibido,
            FontFamily = (FontFamily)recursos["FontMono"],
            FontSize = 11.5,
            Foreground = (Brush)recursos["TextBody"],
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };
        var abrir = BotaoSecundario("Abrir pasta", () =>
        {
            Directory.CreateDirectory(pasta);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{pasta}\"") { UseShellExecute = true });
        });
        abrir.Margin = new Thickness(12, 0, 0, 0);

        var grade = new Grid();
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(rotuloTexto, 0);
        Grid.SetColumn(caminhoTexto, 1);
        Grid.SetColumn(abrir, 2);
        grade.Children.Add(rotuloTexto);
        grade.Children.Add(caminhoTexto);
        grade.Children.Add(abrir);
        return grade;
    }

    private UIElement PainelModulos(ConfiguracaoPreferencias preferencias)
    {
        var linhas = new List<UIElement>();
        var ordem = preferencias.OrdemModulos;
        for (var indice = 0; indice < ordem.Count; indice++)
        {
            linhas.Add(LinhaModulo(preferencias, ordem[indice], indice, ordem.Count));
        }

        var listaModulos = UiApoio.Coluna(8, [.. linhas]);

        var rotuloInicial = new TextBlock { Text = "MÓDULO INICIAL AO ABRIR", Style = (Style)Application.Current.Resources["RotuloKpiStyle"] };
        var comboInicial = ComboModuloInicial(preferencias);
        var blocoInicial = UiApoio.Coluna(6, rotuloInicial, comboInicial);


        var nota = new TextBlock
        {
            Text = "Ordem e visibilidade são aplicadas na hora. O módulo inicial vale a partir da próxima abertura do Assetly. Configurações fica sempre visível.",
            FontSize = 11.5,
            Foreground = (Brush)Application.Current.Resources["TextMuted"],
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 12, 0, 0)
        };

        return UiApoio.Coluna(16, listaModulos, blocoInicial, nota);
    }

    private UIElement LinhaModulo(ConfiguracaoPreferencias preferencias, string titulo, int indice, int total)
    {
        var visivel = !preferencias.ModulosOcultos.Contains(titulo);

        var checkbox = new CheckBox
        {
            Style = (Style)Application.Current.Resources["CheckBoxStyle"],
            Content = titulo,
            IsChecked = visivel,
            VerticalAlignment = VerticalAlignment.Center
        };
        checkbox.Click += (_, _) =>
        {
            ExecutarComTratamento(() =>
            {
                if (checkbox.IsChecked == true)
                {
                    _contexto.MostrarModulo().Executar(new MostrarModuloComando(titulo));
                }
                else
                {
                    _contexto.OcultarModulo().Executar(new OcultarModuloComando(titulo));
                }
            });

            _aplicarAparenciaEReconstruir();
        };

        var subir = BotaoSecundario("▲", () =>
        {
            _contexto.MoverModuloParaCima().Executar(new MoverModuloComando(titulo));
            _aplicarAparenciaEReconstruir();
        });
        subir.IsEnabled = indice > 0;
        subir.Padding = new Thickness(10, 2, 10, 2);

        var descer = BotaoSecundario("▼", () =>
        {
            _contexto.MoverModuloParaBaixo().Executar(new MoverModuloComando(titulo));
            _aplicarAparenciaEReconstruir();
        });
        descer.IsEnabled = indice < total - 1;
        descer.Padding = new Thickness(10, 2, 10, 2);

        var grade = new Grid();
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(checkbox, 0);
        var botoes = UiApoio.Linha(6, subir, descer);
        Grid.SetColumn(botoes, 1);
        grade.Children.Add(checkbox);
        grade.Children.Add(botoes);
        return grade;
    }

    private ComboBox ComboModuloInicial(ConfiguracaoPreferencias preferencias)
    {
        var visiveis = preferencias.OrdemModulos.Where(titulo => !preferencias.ModulosOcultos.Contains(titulo)).ToList();
        var combo = new ComboBox
        {
            Style = (Style)Application.Current.Resources["ComboDialogoStyle"],
            ItemsSource = visiveis,
            SelectedItem = preferencias.ResolverModuloInicial(),
            Width = 260,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        combo.SelectionChanged += (_, _) => ExecutarComTratamento(() =>
        {
            _contexto.DefinirModuloInicial().Executar(new DefinirModuloInicialComando((string?)combo.SelectedItem));
        });
        return combo;
    }
}
