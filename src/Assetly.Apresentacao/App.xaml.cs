using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Assetly.Aplicacao.CasoDeUso;
using Assetly.Aplicacao.Servico;
using Assetly.Apresentacao.Visao;
using Assetly.Infraestrutura.Atualizacao;

namespace Assetly.Apresentacao;

public partial class App : Application
{
    private ContextoAplicacao _contexto = null!;
    private Window _janela = null!;
    private TelaPrincipal _telaPrincipal = null!;
    /// <summary>Nome conferido pelo instalador (<c>AppMutex</c> em instalador/Assetly.iss).</summary>
    internal const string NomeTravaInstalador = "AssetlyAplicacaoAberta";

    private Mutex? _instanciaUnica;
    private Mutex? _travaInstalador;
    private GerenciadorAtualizacoes? _atualizacoes;
    private System.Windows.Controls.Button? _botaoAtualizacao;
    private FechamentoJanela? _fechamento;
    private bool _avisoBandejaMostrado;
    private BandejaSistema? _bandeja;
    private EventWaitHandle? _pedidoMostrar;
    private readonly HashSet<Dominio.Alerta.AlertaId> _alertasAvisados = [];

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        RegistrarTratamentoGlobalDeErros();
        UsarCulturaBrasileira();

        if (e.Args.Contains(Autoteste.Argumento))
        {
            Shutdown(Autoteste.Executar(e.Args));
            return;
        }

        // Consultada pelo instalador (AppMutex do Inno Setup): ele espera o Assetly fechar antes de atualizar.
        _travaInstalador = new Mutex(initiallyOwned: false, NomeTravaInstalador);

#if DEBUG
        // Ferramenta de desenvolvimento (conferir o tema): não vai no executável publicado.
        if (e.Args.Contains("--tema"))
        {
            GaleriaTema.Mostrar();
            return;
        }
#endif

        var ambienteTeste = UsarAmbienteTeste(e.Args);
        if (ambienteTeste)
        {
            RegistroErros.DiretorioLogs = Infraestrutura.Armazenamento.CaminhosArmazenamentoLocal.DiretorioLogsTeste();
        }

        var iniciarNaBandeja = e.Args.Contains(InicioComWindows.ArgumentoBandeja);
        if (!ObterInstanciaUnica(ambienteTeste))
        {
            // Já aberto (talvez escondido na bandeja): pede para a outra instância mostrar a janela e sai.
            if (EventWaitHandle.TryOpenExisting(NomeInstancia(ambienteTeste) + "-Mostrar", out var outraInstancia))
            {
                if (!iniciarNaBandeja)
                {
                    outraInstancia.Set();
                }

                outraInstancia.Dispose();
                Shutdown();
                return;
            }

            MessageBox.Show(
                "O Assetly já está aberto neste ambiente. Use a janela que já está aberta (verifique a barra de tarefas).",
                "Assetly", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        try
        {
            _contexto = ambienteTeste ? ContextoAplicacao.TesteComDadosFicticios() : ContextoAplicacao.LocalPadrao();
            AparenciaApoio.Aplicar(_contexto.ObterPreferencias().Executar());
        }
        catch (Exception excecao) when (excecao is not OutOfMemoryException)
        {
            FalharInicializacao("Não foi possível abrir a base de dados local do Assetly.", excecao);
            return;
        }

        try
        {
            var titulo = _contexto.AmbienteTeste ? "Assetly - Teste" : "Assetly";
            _janela = new Window
            {
                Title = titulo,
                Width = 1180,
                Height = 790,
                MinWidth = 980,
                MinHeight = 670,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Icon = new BitmapImage(new Uri("pack://application:,,,/Assetly;component/Recursos/Imagens/assetly-icon-32.png"))
            };

            _atualizacoes = new GerenciadorAtualizacoes(CriarServicoAtualizacao(), () => _contexto.ObterPreferencias().Executar().AtualizarAutomaticamente);
            _atualizacoes.Mudou += AtualizarBotaoAtualizacao;
            MontarTelaPrincipal(titulo, selecaoForcada: null);

            MainWindow = _janela;
            _fechamento = new FechamentoJanela(_janela, () => _bandeja is not null,
                continuar => _telaPrincipal.FinalizarAntesDeFechar(continuar), AvisarQueContinuaNaBandeja);
            SessionEnding += (_, _) => _fechamento.SairDeVerdade = true;
            if (!_contexto.AmbienteTeste)
            {
                InicioComWindows.CorrigirCaminho();
            }

            AplicarBandeja();
            if (iniciarNaBandeja && _bandeja is not null)
            {
                // Aberto pelo Windows: fica na bandeja. Os alertas que já existem viram uma notificação.
                NotificarAlertas();
            }
            else
            {
                if (!DesbloqueioDialogo.Pedir(_contexto.Bloqueio))
                {
                    Shutdown();
                    return;
                }

                _janela.Show();
                // Com a janela à vista, os alertas atuais não viram notificação — só os que surgirem depois.
                NotificacaoAlertas.Novos(_contexto.ListarAlertas().Executar(), _alertasAvisados, _ => string.Empty);
            }

            _atualizacoes.Iniciar();
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () => _telaPrincipal.SincronizarAoAbrir());
        }
        catch (Exception excecao) when (excecao is not OutOfMemoryException)
        {
            // Sem janela principal, o processo ficaria vivo e invisível segurando a instância única:
            // encerra de vez, com o motivo.
            FalharInicializacao("Não foi possível montar a janela do Assetly.", excecao);
        }
    }

    /// <summary>Na bandeja, fechar a janela só a esconde: na primeira vez, avisa que o programa continua avisando os alertas.</summary>
    private void AvisarQueContinuaNaBandeja()
    {
        if (_avisoBandejaMostrado || _bandeja is null)
        {
            return;
        }

        _avisoBandejaMostrado = true;
        _bandeja.Notificar("O Assetly continua aberto na bandeja",
            "Ele avisa garantias, contas e documentos vencendo. Para fechar de vez: botão direito no ícone → Sair.", critico: false);
    }

    /// <summary>Fecha o programa de verdade (mesmo com a bandeja ligada): sincroniza e termina o que estiver em andamento antes.</summary>
    internal void Sair()
    {
        if (_fechamento is not null)
        {
            _fechamento.SairDeVerdade = true;
        }

        _janela.Close();
    }

    /// <summary>Traz a janela de volta (da bandeja ou minimizada), pedindo o PIN se ela estava escondida.</summary>
    internal void MostrarJanela()
    {
        if (_janela is null || _fechamento?.Finalizando == true)
        {
            return;
        }

        if (!_janela.IsVisible)
        {
            if (!DesbloqueioDialogo.Pedir(_contexto.Bloqueio))
            {
                return;
            }

            _janela.Show();
        }

        if (_janela.WindowState == WindowState.Minimized)
        {
            _janela.WindowState = WindowState.Normal;
        }

        _janela.Activate();
    }

    /// <summary>Liga ou desliga o ícone da bandeja conforme a opção "manter na bandeja e iniciar com o Windows".</summary>
    internal void AplicarBandeja()
    {
        var usar = !_contexto.AmbienteTeste && InicioComWindows.Ativo;
        if (usar && _bandeja is null)
        {
            _bandeja = new BandejaSistema(_janela.Title);
            _bandeja.AbrirSolicitado += () => AcoesApoio.ExecutarComTratamento(MostrarJanela);
            _bandeja.AlertasSolicitados += () => AcoesApoio.ExecutarComTratamento(() =>
            {
                MostrarJanela();
                if (_janela.IsVisible)
                {
                    _telaPrincipal.IrPara("Alertas");
                }
            });
            _bandeja.SairSolicitado += () => AcoesApoio.ExecutarComTratamento(Sair);
        }
        else if (!usar && _bandeja is not null)
        {
            _bandeja.Dispose();
            _bandeja = null;
        }
    }

    /// <summary>Alertas novos viram notificação do Windows quando o Assetly está na bandeja ou em segundo plano.</summary>
    private void NotificarAlertas()
    {
        var aviso = NotificacaoAlertas.Novos(_contexto.ListarAlertas().Executar(), _alertasAvisados, ContextoApoio.NomesBens(_contexto));
        if (aviso is not null && _bandeja is not null && !(_janela.IsVisible && _janela.IsActive))
        {
            _bandeja.Notificar(aviso.Titulo, aviso.Texto, aviso.Critico);
        }
    }

    private static IServicoAtualizacao CriarServicoAtualizacao()
    {
        try
        {
            return new ServicoAtualizacaoGithub();
        }
        catch (Exception excecao) when (excecao is not OutOfMemoryException)
        {
            // Problema no ambiente de atualização nunca pode impedir o programa de abrir.
            RegistroErros.Registrar("Atualização automática indisponível", excecao);
            return new ServicoAtualizacaoIndisponivel();
        }
    }

    /// <summary>Barra de título: botão "Atualizar para X" (só aparece com versão nova baixada) + selo do ambiente.</summary>
    private UIElement ExtraBarraTitulo(UIElement seloAmbiente)
    {
        _botaoAtualizacao = new System.Windows.Controls.Button
        {
            Style = (Style)Resources["BotaoPrimarioStyle"],
            Height = 24,
            Padding = new Thickness(10, 0, 10, 0),
            Margin = new Thickness(0, 0, 14, 0),
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "Uma versão nova do Assetly foi baixada. Clique para reiniciar e atualizar (seus dados não são alterados)."
        };
        System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(_botaoAtualizacao, true);
        _botaoAtualizacao.Click += (_, _) => AcoesApoio.ExecutarComTratamento(ConfirmarReinicioParaAtualizar);
        AtualizarBotaoAtualizacao();

        var painel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        painel.Children.Add(_botaoAtualizacao);
        painel.Children.Add(seloAmbiente);
        return painel;
    }

    private void AtualizarBotaoAtualizacao()
    {
        if (_botaoAtualizacao is null || _atualizacoes is null)
        {
            return;
        }

        var pronta = _atualizacoes.Estado == EstadoAtualizacao.Pronta;
        _botaoAtualizacao.Visibility = pronta ? Visibility.Visible : Visibility.Collapsed;
        _botaoAtualizacao.Content = $"Atualizar para {_atualizacoes.VersaoNova}";
    }

    internal void ConfirmarReinicioParaAtualizar()
    {
        if (_atualizacoes is null)
        {
            return;
        }

        if (DialogoApoio.MostrarConfirmacao("Atualizar o Assetly",
                $"A versão {_atualizacoes.VersaoNova} já foi baixada. O Assetly será fechado, atualizado e aberto de novo em alguns segundos. " +
                "Seus dados não são alterados (uma cópia de segurança do banco é feita se a versão nova mudar a estrutura).",
                "Reiniciar e atualizar"))
        {
            _atualizacoes.ReiniciarEAtualizar();
        }
    }

    private void FalharInicializacao(string mensagem, Exception excecao)
    {
        RegistroErros.Registrar("Falha ao iniciar", excecao);
        MessageBox.Show(
            $"{mensagem}\n\n{DialogoApoio.MensagemErro(excecao)}\n\nDetalhes técnicos foram gravados em:\n{RegistroErros.DiretorioLogs}",
            "Assetly", MessageBoxButton.OK, MessageBoxImage.Error);
        Shutdown(1);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _atualizacoes?.AplicarAoFecharSePronta();
        _bandeja?.Dispose();
        _pedidoMostrar?.Dispose();
        _instanciaUnica?.Dispose();
        _travaInstalador?.Dispose();
        base.OnExit(e);
    }

    private void MontarTelaPrincipal(string titulo, string? selecaoForcada)
    {
        _telaPrincipal = new TelaPrincipal(_contexto, ReconstruirAparencia, selecaoForcada, _atualizacoes);
        _telaPrincipal.AlertasAtualizados += () => AcoesApoio.ExecutarComTratamento(NotificarAlertas);
        JanelaEscura.Decorar(_janela, titulo, _telaPrincipal.Raiz, ExtraBarraTitulo(_telaPrincipal.SeloAmbiente));
    }

    /// <summary>Callback passado a ModuloConfiguracoes: tema/cor/densidade e ordem/visibilidade dos
    /// módulos pedem reconstrução da árvore visual inteira para refletir de forma consistente (ver
    /// AparenciaApoio) — mais barato e mais seguro do que caçar os pontos que fazem leitura única de
    /// recurso pelo app inteiro. Devolve o usuário à própria tela de Configurações.</summary>
    private void ReconstruirAparencia()
    {
        AparenciaApoio.Aplicar(_contexto.ObterPreferencias().Executar());
        _telaPrincipal.Encerrar();
        MontarTelaPrincipal(_janela.Title, "Configurações");
    }

    /// <summary>A interface é toda em português: formatação e interpretação de números e datas seguem
    /// pt-BR independentemente do idioma/região do Windows. O idioma dos controles WPF (ex.: seletor
    /// de data) é definido em cada janela por <c>JanelaEscura</c> — e não com
    /// <c>LanguageProperty.OverrideMetadata</c>, que lança exceção se a propriedade já tiver sido lida
    /// (acontecia ao executar pela Visual Studio e impedia o app de abrir).</summary>
    private static void UsarCulturaBrasileira()
    {
        var cultura = CultureInfo.GetCultureInfo("pt-BR");
        CultureInfo.DefaultThreadCurrentCulture = cultura;
        CultureInfo.DefaultThreadCurrentUICulture = cultura;
        Thread.CurrentThread.CurrentCulture = cultura;
        Thread.CurrentThread.CurrentUICulture = cultura;
    }

    /// <summary>Uma única instância por ambiente: duas janelas sobre o mesmo banco gerariam alertas e
    /// backups automáticos em dobro. Produção e teste podem ficar abertos lado a lado.</summary>
    private bool ObterInstanciaUnica(bool ambienteTeste)
    {
        var nome = NomeInstancia(ambienteTeste);
        _instanciaUnica = new Mutex(initiallyOwned: true, nome, out var criadaAgora);
        if (!criadaAgora)
        {
            _instanciaUnica.Dispose();
            _instanciaUnica = null;
            return false;
        }

        // Abrir o Assetly de novo (atalho, menu Iniciar) só traz esta janela para a frente — inclusive da bandeja.
        _pedidoMostrar = new EventWaitHandle(false, EventResetMode.AutoReset, nome + "-Mostrar");
        ThreadPool.RegisterWaitForSingleObject(_pedidoMostrar,
            (_, _) => Dispatcher.BeginInvoke(() => AcoesApoio.ExecutarComTratamento(MostrarJanela)), null, Timeout.Infinite, executeOnlyOnce: false);
        return true;
    }

    private static string NomeInstancia(bool ambienteTeste) => ambienteTeste ? @"Local\Assetly-Teste" : @"Local\Assetly-Producao";

    /// <summary>Último recurso para exceções que escapam de um tratamento local: registra no log e
    /// mostra uma mensagem em vez de fechar o programa sem explicação.</summary>
    private void RegistrarTratamentoGlobalDeErros()
    {
        DispatcherUnhandledException += (_, evento) =>
        {
            RegistroErros.Registrar("Erro não tratado na interface", evento.Exception);
            evento.Handled = true;
            try
            {
                DialogoApoio.MostrarErro(evento.Exception);
            }
            catch (Exception excecaoDialogo) when (excecaoDialogo is not OutOfMemoryException)
            {
                MessageBox.Show(DialogoApoio.MensagemErro(evento.Exception), "Assetly", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        AppDomain.CurrentDomain.UnhandledException += (_, evento) =>
        {
            if (evento.ExceptionObject is Exception excecao)
            {
                RegistroErros.Registrar("Erro fatal", excecao);
            }
        };
        TaskScheduler.UnobservedTaskException += (_, evento) =>
        {
            RegistroErros.Registrar("Erro em tarefa em segundo plano", evento.Exception);
            evento.SetObserved();
        };
    }

    private static bool UsarAmbienteTeste(string[] argumentos)
    {
        var ambienteVariavel = Environment.GetEnvironmentVariable("ASSETLY_AMBIENTE") ?? string.Empty;
        return argumentos.Contains("--teste")
            || EhAmbienteTeste(ValorArgumentoAmbiente(argumentos))
            || EhAmbienteTeste(ambienteVariavel);
    }

    private static string ValorArgumentoAmbiente(string[] argumentos)
    {
        for (var indice = 0; indice < argumentos.Length; indice++)
        {
            var argumento = argumentos[indice];
            if (argumento.StartsWith("--ambiente=", StringComparison.Ordinal))
            {
                return argumento["--ambiente=".Length..];
            }

            if (argumento == "--ambiente" && indice + 1 < argumentos.Length)
            {
                return argumentos[indice + 1];
            }
        }

        return string.Empty;
    }

    private static bool EhAmbienteTeste(string valor) =>
        string.Equals(valor.Trim(), "teste", StringComparison.OrdinalIgnoreCase);
}
