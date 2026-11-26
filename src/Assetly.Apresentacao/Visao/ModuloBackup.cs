using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Assetly.Aplicacao.CasoDeUso;
using Assetly.Aplicacao.Dados;
using Assetly.Dominio.Backup;
using static Assetly.Apresentacao.Visao.AcoesApoio;
using static Assetly.Apresentacao.Visao.DialogoApoio;
using static Assetly.Apresentacao.Visao.LayoutApoio;

namespace Assetly.Apresentacao.Visao;

internal sealed class ModuloBackup : IModulo
{
    private readonly ContextoAplicacao _contexto;
    private readonly Action _atualizarTela;
    private string? FalhaAutomatica { get => _contexto.FalhaBackupAutomatico; set => _contexto.FalhaBackupAutomatico = value; }
    private string? _falhaSincronizacao;
    private string? _avisoSincronizacao;

    /// <summary>Opções de quantos backups manter no destino (0 = todos).</summary>
    private static readonly IReadOnlyList<ItemValor<int>> OpcoesRetencao =
    [
        new(5, "os últimos 5"),
        new(10, "os últimos 10"),
        new(20, "os últimos 20"),
        new(30, "os últimos 30"),
        new(0, "todos (nunca apagar)")
    ];

    public string Titulo => "Backup";
    public string Descricao => "Cópia local dos seus dados";
    public bool ComContador => false;
    public UIElement Conteudo { get; }
    public int Contador => 0;

    internal ModuloBackup(ContextoAplicacao contexto)
    {
        _contexto = contexto;
        var recursos = Application.Current.Resources;

        var destinoTexto = new TextBlock
        {
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var abrirPasta = BotaoSecundario("Abrir pasta", AbrirPastaDestino);
        abrirPasta.Margin = new Thickness(14, 0, 0, 0);
        var escolherPasta = BotaoSecundario("Escolher pasta…", EscolherPastaBackup);
        escolherPasta.Margin = new Thickness(8, 0, 0, 0);
        var linhaDestino = new Grid();
        linhaDestino.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        linhaDestino.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        linhaDestino.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(destinoTexto, 0);
        Grid.SetColumn(abrirPasta, 1);
        Grid.SetColumn(escolherPasta, 2);
        linhaDestino.Children.Add(destinoTexto);
        linhaDestino.Children.Add(abrirPasta);
        linhaDestino.Children.Add(escolherPasta);
        var dicaDestino = Nota("Prefira um pendrive, HD externo ou uma pasta sincronizada com a nuvem: um backup no mesmo disco não protege contra falha do disco.");
        var retencaoCombo = ApoioFichas.ComboItens(OpcoesRetencao);
        retencaoCombo.Width = 200;
        retencaoCombo.HorizontalAlignment = HorizontalAlignment.Left;
        var rotuloRetencao = UiApoio.Texto("Manter no destino:", 12.5, FontWeights.Normal, (Brush)recursos["TextBody"]);
        rotuloRetencao.VerticalAlignment = VerticalAlignment.Center;
        var notaRetencao = Nota("Depois de cada backup, os mais antigos são apagados do destino — assim o pendrive ou o disco não enche e o backup automático não para de funcionar.");
        notaRetencao.TextWrapping = TextWrapping.Wrap;
        var cardDestino = CardConfiguracao("Destino do backup",
            UiApoio.Coluna(10, linhaDestino, dicaDestino, UiApoio.Linha(10, rotuloRetencao, retencaoCombo), notaRetencao));

        var frequenciaCombo = Combo(FrequenciaBackup.Diaria);
        frequenciaCombo.Width = 160;
        var toggleAuto = BotaoSecundario("Ativar backup automático", () => AlternarAutoBackup(frequenciaCombo));
        var linhaAuto = UiApoio.Linha(12, toggleAuto, frequenciaCombo);
        var statusAuto = new TextBlock
        {
            FontSize = 11.5,
            Foreground = (Brush)recursos["TextMuted"],
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0)
        };
        var cardAuto = CardConfiguracao("Backup automático", UiApoio.Coluna(10, linhaAuto, statusAuto));

        var restaurarArquivo = BotaoSecundario("Restaurar de um arquivo .assetly…", RestaurarDeArquivo);
        var restaurar = BotaoSecundario("Restaurar de uma pasta…", RestaurarBackup);
        var testar = BotaoSecundario("Testar um backup…", TestarBackup);
        var notaRestaurar = Nota("Substitui os dados atuais pelos de um backup (arquivo \"Assetly-Backup-….assetly\" ou pasta \"Assetly-Backup-…\"). Antes, o estado atual é copiado automaticamente para a pasta de segurança — a restauração pode ser desfeita. " +
            "\"Testar um backup\" só abre e confere o backup (banco íntegro, todos os documentos presentes), sem mudar nada.");
        notaRestaurar.TextWrapping = TextWrapping.Wrap;
        var cardRestaurar = CardConfiguracao("Restaurar", UiApoio.Coluna(10, UiApoio.Linha(8, restaurarArquivo, restaurar, testar), notaRestaurar));

        // Proteção por senha (AES-256): vale para backups manuais, automáticos, cópias em arquivo e sincronização.
        var statusSenha = Nota(string.Empty);
        statusSenha.TextWrapping = TextWrapping.Wrap;
        var botaoSenha = BotaoSecundario("Proteger com senha…", AlternarSenha);
        var trocarSenha = BotaoSecundario("Trocar senha…", () => DefinirSenha(troca: true));
        var cardSenha = CardConfiguracao("Proteção por senha", UiApoio.Coluna(10, UiApoio.Linha(8, botaoSenha, trocarSenha), statusSenha));

        // Sincronização opcional por uma pasta já sincronizada com a nuvem (OneDrive, Google Drive, Dropbox...).
        var statusSync = Nota(string.Empty);
        statusSync.TextWrapping = TextWrapping.Wrap;
        var escolherSync = BotaoSecundario("Escolher pasta sincronizada…", EscolherPastaSincronizacao);
        var sincronizarAgora = BotaoSecundario("Sincronizar agora", () => Sincronizar(manual: true));
        var desligarSync = BotaoSecundario("Desligar", DesligarSincronizacao);
        var notaSync = Nota("Use uma pasta do OneDrive, Google Drive ou Dropbox nos dois computadores. O Assetly guarda lá uma cópia única (protegida pela senha dos backups, se houver), " +
            "envia suas alterações ao fechar e traz as do outro computador ao abrir. Se os dois lados mudarem, você escolhe qual manter. Seus dados não passam por nenhum servidor do Assetly.");
        notaSync.TextWrapping = TextWrapping.Wrap;
        var cardSync = CardConfiguracao("Sincronização entre computadores (opcional)",
            UiApoio.Coluna(10, UiApoio.Linha(8, escolherSync, sincronizarAgora, desligarSync), statusSync, notaSync));

        var ultimoTexto = new TextBlock
        {
            FontFamily = (FontFamily)recursos["FontMono"],
            FontSize = 11,
            Foreground = (Brush)recursos["TextMuted"],
            VerticalAlignment = VerticalAlignment.Center
        };
        var executarAgora = new Button { Content = "Fazer backup agora", Style = (Style)recursos["BotaoPrimarioStyle"] };
        executarAgora.Click += (_, _) => ExecutarComTratamento(FazerBackupAgora);

        // "Salvar cópia em arquivo" é um backup manual: fica junto de "Fazer backup agora" (antes estava no card da senha).
        var exportarArquivo = BotaoSecundario("Salvar cópia em arquivo…", SalvarCopiaEmArquivo);
        var direitaCabecalho = UiApoio.Linha(12, ultimoTexto, exportarArquivo, executarAgora);
        var cabecalho = CabecalhoTela("Backup", "Escolha onde e com que frequência seus dados são copiados.", direitaCabecalho);
        cabecalho.Margin = new Thickness(0, 0, 0, 14);

        // O risco mais importante da tela (nunca houve backup, backup antigo ou falhando) não pode ficar num texto cinza no canto.
        var aviso = new FaixaAviso();
        aviso.Raiz.Margin = new Thickness(0);
        var cartoes = UiApoio.Coluna(14, aviso.Raiz, cardDestino, cardAuto, cardSenha, cardSync, cardRestaurar);

        var raiz = new Grid { Margin = new Thickness(18, 16, 20, 18) };
        raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        raiz.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var rolagem = new ScrollViewer { Content = cartoes, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetRow(cabecalho, 0);
        Grid.SetRow(rolagem, 1);
        raiz.Children.Add(cabecalho);
        raiz.Children.Add(rolagem);
        Conteudo = raiz;

        var sincronizandoCombo = false;
        retencaoCombo.SelectionChanged += (_, _) =>
        {
            if (!sincronizandoCombo && retencaoCombo.SelectedItem is ItemValor<int> item)
            {
                ExecutarComTratamento(() => _contexto.DefinirRetencaoBackup().Executar(item.Valor));
            }
        };
        frequenciaCombo.SelectionChanged += (_, _) =>
        {
            if (sincronizandoCombo)
            {
                return;
            }

            ExecutarComTratamento(() =>
            {
                if (_contexto.ObterConfiguracaoBackup().Executar().AutoBackupHabilitado)
                {
                    _contexto.HabilitarAutoBackup().Executar(new HabilitarAutoBackupComando(ValorEnum<FrequenciaBackup>(frequenciaCombo)));
                    _atualizarTela?.Invoke();
                }
            });
        };

        _atualizarTela = () =>
        {
            var configuracao = _contexto.ObterConfiguracaoBackup().Executar();

            destinoTexto.Text = configuracao.CaminhoDestino ?? "Nenhuma pasta selecionada";
            destinoTexto.ToolTip = configuracao.CaminhoDestino;
            destinoTexto.Foreground = (Brush)recursos[configuracao.CaminhoDestino is null ? "TextMuted" : "TextBody"];
            abrirPasta.IsEnabled = configuracao.CaminhoDestino is not null;

            var lembrete = Aplicacao.CasoDeUso.LembreteBackup.Avaliar(configuracao, DateTimeOffset.Now,
                _contexto.RepositorioBem.Listar().Count > 0, FalhaAutomatica);
            aviso.Mostrar(lembrete?.Mensagem, lembrete?.Critico ?? false);
            ultimoTexto.Text = configuracao.UltimoBackupEm is null
                ? "nenhum backup realizado ainda"
                : $"último backup {Formatador.DataHora(configuracao.UltimoBackupEm.Value.LocalDateTime)}";

            toggleAuto.Content = configuracao.AutoBackupHabilitado ? "Desativar backup automático" : "Ativar backup automático";
            frequenciaCombo.IsEnabled = configuracao.CaminhoDestino is not null;
            sincronizandoCombo = true;
            var itens = (IEnumerable<ItemEnum<FrequenciaBackup>>)frequenciaCombo.ItemsSource;
            frequenciaCombo.SelectedItem = itens.First(item => item.Valor == configuracao.Frequencia);
            retencaoCombo.SelectedItem = OpcoesRetencao.FirstOrDefault(item => item.Valor == configuracao.ManterUltimos)
                ?? OpcoesRetencao.First(item => item.Valor == 0);
            sincronizandoCombo = false;

            statusAuto.Foreground = (Brush)recursos[FalhaAutomatica is null ? "TextMuted" : "StatusPerigoText"];
            statusAuto.Text = FalhaAutomatica is not null
                ? $"Último backup automático falhou: {FalhaAutomatica} Uma nova tentativa é feita no próximo ciclo."
                : !configuracao.AutoBackupHabilitado
                    ? configuracao.CaminhoDestino is null
                        ? "Sem pasta de destino: ao ativar, o Assetly pede para você escolher uma."
                        : "Backup automático desativado."
                    : $"Backup automático ativo — frequência {Formatador.TextoEnum(configuracao.Frequencia).ToLowerInvariant()}. Executado enquanto o Assetly estiver aberto.";

            botaoSenha.Content = configuracao.Criptografado ? "Remover senha…" : "Proteger com senha…";
            trocarSenha.Visibility = configuracao.Criptografado ? Visibility.Visible : Visibility.Collapsed;
            statusSenha.Foreground = (Brush)recursos[configuracao.Criptografado ? "StatusOkText" : "TextMuted"];
            statusSenha.Text = configuracao.Criptografado
                ? "Ativa: os backups são gravados num único arquivo .assetly criptografado (AES-256). Sem a senha não é possível restaurá-los — guarde-a num lugar seguro."
                : "Desativada: os backups ficam numa pasta comum, legível por quem tiver acesso ao destino. Ative para criptografar (recomendado em pendrive ou nuvem).";

            var sincronizacao = _contexto.ConfigurarSincronizacao().Obter();
            sincronizarAgora.IsEnabled = sincronizacao.Ativa;
            desligarSync.IsEnabled = sincronizacao.Ativa;
            statusSync.Foreground = (Brush)recursos[_falhaSincronizacao is not null ? "StatusPerigoText"
                : _avisoSincronizacao is not null && sincronizacao.Ativa ? "StatusAvisoText"
                : sincronizacao.Ativa ? "StatusOkText" : "TextMuted"];
            statusSync.Text = _falhaSincronizacao is not null
                ? $"Última sincronização falhou: {_falhaSincronizacao}"
                : _avisoSincronizacao is not null && sincronizacao.Ativa
                    ? $"Atenção: {_avisoSincronizacao}"
                : !sincronizacao.Ativa
                    ? "Desligada."
                    : $"Ligada — pasta {sincronizacao.Pasta}. " + (sincronizacao.UltimaSincronizacaoEm is { } quando
                        ? $"Última sincronização {Formatador.DataHora(quando.LocalDateTime)}."
                        : "Ainda não sincronizado.");
        };
        _atualizarTela();
    }

    /// <summary>Só redesenha o card: o backup automático e o envio da sincronização ficam em
    /// <see cref="ExecutarTarefasPeriodicas"/>, chamado pelo ciclo da tela principal.</summary>
    public void Atualizar() => _atualizarTela();

    /// <summary>Backup automático (quando devido) e envio da sincronização (a cada 10 minutos), fora da thread da
    /// interface e sem bloquear nada. Falhas não interrompem o usuário com um diálogo a cada ciclo: ficam registradas
    /// no card (e no log) e são tentadas de novo no ciclo seguinte.</summary>
    internal void ExecutarTarefasPeriodicas()
    {
        var agora = DateTimeOffset.Now;
        var sincronizacao = _contexto.SincronizacaoAutomatica;
        OperacaoEmSegundoPlano.Silenciosa(() =>
        {
            Exception? falhaBackup = null;
            try
            {
                _contexto.VerificarEExecutarBackupAutomatico().Executar(agora);
            }
            catch (Exception excecao) when (excecao is not OutOfMemoryException)
            {
                falhaBackup = excecao;
            }

            return (FalhaBackup: falhaBackup, Sincronizacao: sincronizacao.DuranteUso(agora));
        }, resultado =>
        {
            if (resultado.FalhaBackup is null)
            {
                FalhaAutomatica = null;
            }
            else
            {
                if (FalhaAutomatica is null)
                {
                    RegistroErros.Registrar("Falha no backup automático", resultado.FalhaBackup);
                }

                FalhaAutomatica = MensagemErro(resultado.FalhaBackup);
            }

            AplicarSincronizacao(resultado.Sincronizacao);
            _atualizarTela();
        });
    }

    /// <summary>Ao abrir o programa: envia ou recebe (com o painel de espera — receber substitui os dados) e, se os
    /// dois lados mudaram, pergunta ao usuário.</summary>
    internal void SincronizarAoAbrir()
    {
        var sincronizacao = _contexto.SincronizacaoAutomatica;
        if (!sincronizacao.Ativa)
        {
            return;
        }

        var agora = DateTimeOffset.Now;
        OperacaoEmSegundoPlano.ComEspera("Sincronizando com a pasta da nuvem…",
            () => sincronizacao.AoAbrir(agora, NaInterface<Aplicacao.Servico.MetadadosSincronizacao, EscolhaConflito>(PerguntarConflito)),
            situacao =>
            {
                AplicarSincronizacao(situacao);
                _atualizarTela();
            });
    }

    /// <summary>Ao fechar: espera a operação em andamento (um backup automático, por exemplo) terminar, envia as
    /// alterações para a pasta sincronizada e só então chama <paramref name="fechar"/> — que é sempre chamado,
    /// mesmo se a sincronização falhar.</summary>
    internal void FinalizarAntesDeFechar(Action fechar)
    {
        var sincronizacao = _contexto.SincronizacaoAutomatica;
        if (!sincronizacao.Ativa && !OperacaoEmSegundoPlano.EmAndamento)
        {
            fechar();
            return;
        }

        OperacaoEmSegundoPlano.ComEspera(sincronizacao.Ativa ? "Enviando suas alterações para a pasta sincronizada…" : "Concluindo o backup em andamento…",
            () =>
            {
                try
                {
                    return sincronizacao.AoFechar(DateTimeOffset.Now);
                }
                catch (Exception excecao) when (excecao is not OutOfMemoryException)
                {
                    return new SituacaoSincronizacao(false, null, excecao);
                }
            },
            situacao =>
            {
                if (situacao?.Falha is { } falha)
                {
                    RegistroErros.Registrar("Falha na sincronização ao fechar", falha);
                }

                fechar();
            });
    }

    private void AplicarSincronizacao(SituacaoSincronizacao? situacao)
    {
        if (situacao is null)
        {
            return;
        }

        if (situacao.Falha is not null)
        {
            if (_falhaSincronizacao is null)
            {
                RegistroErros.Registrar("Falha na sincronização automática", situacao.Falha);
            }

            _falhaSincronizacao = MensagemErro(situacao.Falha);
            return;
        }

        _falhaSincronizacao = null;
        _avisoSincronizacao = situacao.Aviso;
        if (situacao.DadosRecebidos)
        {
            _contexto.NotificarAlteracao();
        }
    }

    private void Sincronizar(bool manual)
    {
        OperacaoEmSegundoPlano.ComEspera("Sincronizando com a pasta da nuvem…",
            () => _contexto.SincronizarDados().Executar(NaInterface<Aplicacao.Servico.MetadadosSincronizacao, EscolhaConflito>(PerguntarConflito)),
            resultado =>
            {
                _falhaSincronizacao = null;
                _avisoSincronizacao = resultado.Acao == AcaoSincronizacao.Conflito && !resultado.Executada
                    ? SincronizacaoAutomatica.AvisoConflito
                    : null;
                if (resultado.Acao == AcaoSincronizacao.Receber && resultado.Executada)
                {
                    _contexto.NotificarAlteracao();
                }

                _atualizarTela();
                if (manual)
                {
                    MostrarInformacao("Sincronização", resultado.Acao switch
                    {
                        AcaoSincronizacao.Enviar => "Suas alterações foram enviadas para a pasta sincronizada.",
                        AcaoSincronizacao.Receber => $"Os dados de \"{resultado.Remoto?.Dispositivo}\" foram trazidos para este computador (o estado anterior foi guardado na pasta de segurança).",
                        AcaoSincronizacao.Conflito => "Sincronização cancelada: nada foi alterado.",
                        _ => "Tudo em dia: os dados deste computador e da pasta sincronizada são iguais."
                    });
                }
            });
    }

    /// <summary>Pergunta feita de dentro de uma operação em segundo plano: o diálogo precisa abrir na thread da interface.</summary>
    private static Func<TEntrada, TSaida> NaInterface<TEntrada, TSaida>(Func<TEntrada, TSaida> pergunta) =>
        entrada => Application.Current.Dispatcher.Invoke(() => pergunta(entrada));

    private static EscolhaConflito PerguntarConflito(Aplicacao.Servico.MetadadosSincronizacao remoto)
    {
        var escolha = MostrarEscolha("Sincronização — os dois lados mudaram",
            $"Os dados mudaram neste computador e também em \"{remoto.Dispositivo}\" (enviado em {Formatador.DataHora(remoto.GravadoEm.LocalDateTime)}) desde a última sincronização.\n\n" +
            "Escolha qual versão manter. A outra não é perdida: \"Manter deste computador\" substitui a cópia da pasta; \"Usar a da pasta\" guarda o estado atual na pasta de segurança antes de substituir.",
            "Manter deste computador", "Usar a da pasta", "Decidir depois");
        return escolha switch
        {
            0 => EscolhaConflito.ManterDesteComputador,
            1 => EscolhaConflito.UsarDaPasta,
            _ => EscolhaConflito.Cancelar
        };
    }

    private void EscolherPastaSincronizacao()
    {
        var atual = _contexto.ConfigurarSincronizacao().Obter().Pasta;
        var pasta = SelecionarPasta("Pasta sincronizada com a nuvem (OneDrive, Google Drive, Dropbox…)", atual);
        if (pasta is null)
        {
            return;
        }

        if (_contexto.DestinoBackupConflitaComDados(pasta))
        {
            throw new InvalidOperationException("a pasta de sincronização não pode ficar dentro da pasta de dados do Assetly.");
        }

        _contexto.ConfigurarSincronizacao().Ativar(pasta);
        Sincronizar(manual: true);
    }

    private void DesligarSincronizacao()
    {
        _contexto.ConfigurarSincronizacao().Desativar();
        _falhaSincronizacao = null;
        _avisoSincronizacao = null;
        _atualizarTela();
    }

    private void AlternarSenha()
    {
        var configuracao = _contexto.ObterConfiguracaoBackup().Executar();
        if (configuracao.Criptografado)
        {
            if (MostrarConfirmacao("Remover senha dos backups",
                    "Os próximos backups deixarão de ser criptografados. Backups antigos protegidos continuam exigindo a senha para restaurar. Deseja continuar?",
                    "Remover senha"))
            {
                _contexto.DesativarCriptografiaBackup().Executar();
                _atualizarTela();
            }

            return;
        }

        DefinirSenha(troca: false);
    }

    /// <summary>Liga a proteção ou troca a senha num passo só (antes era preciso remover e proteger de novo).</summary>
    private void DefinirSenha(bool troca)
    {
        var senha = CampoSenha();
        var confirmacao = CampoSenha();
        var texto = $"Mínimo de {Aplicacao.CasoDeUso.SenhaBackup.TamanhoMinimo} caracteres. A senha fica guardada neste computador protegida pelo Windows (só sua conta consegue lê-la), " +
            "para que os backups automáticos funcionem. Se você esquecer a senha, os backups criptografados não poderão ser restaurados.";
        if (troca)
        {
            texto += "\n\nOs backups já feitos continuam abrindo só com a senha antiga — guarde as duas enquanto eles existirem.";
        }

        if (_contexto.ConfigurarSincronizacao().Obter().Ativa)
        {
            texto += "\n\nA sincronização entre computadores usa esta mesma senha: no outro computador, defina a mesma senha em Backup → Proteção por senha, " +
                "senão ele não consegue abrir a cópia da pasta sincronizada (erro \"senha incorreta\").";
        }

        var aviso = Nota(texto);
        aviso.TextWrapping = TextWrapping.Wrap;
        var grade = GradeFormulario();
        AdicionarCampo(grade, 0, 0, "Senha *", senha);
        AdicionarCampo(grade, 0, 1, "Confirme a senha *", confirmacao);
        Grid.SetRow(aviso, 1);
        Grid.SetColumnSpan(aviso, 2);
        aviso.Margin = new Thickness(0, 14, 0, 0);
        grade.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grade.Children.Add(aviso);
        if (MostrarFormulario(troca ? "Trocar a senha dos backups" : "Proteger backups com senha", grade,
                () => _contexto.AtivarCriptografiaBackup().Executar(senha.Password, confirmacao.Password), largura: 560))
        {
            _atualizarTela();
        }
    }

    private void SalvarCopiaEmArquivo()
    {
        var protegido = _contexto.SenhaBackupSalva() is not null;
        var dialogo = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Salvar cópia completa do Assetly",
            FileName = $"Assetly-Backup-{DateTime.Now:yyyy-MM-dd_HH-mm}.assetly",
            DefaultExt = ".assetly",
            Filter = "Backup do Assetly (*.assetly)|*.assetly",
            InitialDirectory = DiretorioInicial(_contexto.ObterConfiguracaoBackup().Executar().CaminhoDestino)
        };
        if (dialogo.ShowDialog(Application.Current.MainWindow) != true)
        {
            return;
        }

        var destino = dialogo.FileName;
        OperacaoEmSegundoPlano.ComEspera("Salvando a cópia…",
            () =>
            {
                _contexto.GerarArquivoBackup(destino);
                return destino;
            },
            salvo => MostrarInformacao("Cópia salva", $"Banco de dados e documentos salvos em um único arquivo{(protegido ? ", criptografado com a senha dos backups" : " (sem senha)")}:\n\n{salvo}"));
    }

    /// <summary>Abre e confere um backup sem restaurar: o último do destino, um arquivo .assetly ou uma pasta.</summary>
    private void TestarBackup()
    {
        var destino = _contexto.ObterConfiguracaoBackup().Executar().CaminhoDestino;
        var ultimo = UltimoBackupDoDestino(destino);
        var opcoes = ultimo is null
            ? new[] { "Escolher arquivo .assetly…", "Escolher pasta…" }
            : new[] { "Testar o último backup", "Escolher arquivo .assetly…", "Escolher pasta…" };
        var escolha = MostrarEscolha("Testar um backup",
            ultimo is null
                ? "Qual backup você quer conferir? Nada é alterado: o backup só é aberto e verificado."
                : $"Qual backup você quer conferir? Nada é alterado: o backup só é aberto e verificado.\n\nÚltimo backup no destino:\n{ultimo}",
            opcoes);
        if (escolha < 0)
        {
            return;
        }

        var caminho = opcoes[escolha] switch
        {
            "Testar o último backup" => ultimo,
            "Escolher pasta…" => SelecionarPasta("Selecione a pasta do backup (Assetly-Backup-…)", destino),
            _ => SelecionarArquivoBackup(destino)
        };
        if (caminho is null)
        {
            return;
        }

        string? senha = null;
        if (File.Exists(caminho) && Infraestrutura.Armazenamento.ArquivoProtegido.EhProtegidoPorSenha(caminho))
        {
            // Sem usar a senha guardada: o teste também confirma que você ainda sabe a senha deste backup.
            senha = PedirSenha("Backup protegido por senha", "Este backup é criptografado. Digite a senha usada quando ele foi criado — o teste também confirma que você ainda lembra dela.");
            if (senha is null)
            {
                return;
            }
        }

        OperacaoEmSegundoPlano.ComEspera("Conferindo o backup…", () => _contexto.TestarBackup().Executar(caminho, senha), resultado =>
            MostrarInformacao(resultado.Completo ? "Backup em ordem" : "Backup incompleto",
                (resultado.Completo ? "O backup abre normalmente e está completo.\n\n" : $"O backup abre, mas {resultado.DocumentosSemArquivo} documento(s) estão sem o arquivo.\n\n") +
                $"{resultado.Bens} bem(ns) e {resultado.Documentos} documento(s) cadastrados · banco íntegro (versão {resultado.VersaoBanco}).\n\n{caminho}"));
    }

    private static string? UltimoBackupDoDestino(string? destino)
    {
        if (string.IsNullOrWhiteSpace(destino) || !Directory.Exists(destino))
        {
            return null;
        }

        return Directory.EnumerateFileSystemEntries(destino, "Assetly-Backup-*")
            .Where(caminho => Directory.Exists(caminho) || caminho.EndsWith(Infraestrutura.Armazenamento.ArquivoProtegido.Extensao, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private void RestaurarDeArquivo()
    {
        var arquivo = SelecionarArquivoBackup(_contexto.ObterConfiguracaoBackup().Executar().CaminhoDestino);
        if (arquivo is null)
        {
            return;
        }

        string? senha = null;
        if (Infraestrutura.Armazenamento.ArquivoProtegido.EhProtegidoPorSenha(arquivo))
        {
            senha = PedirSenha("Backup protegido por senha", "Este backup é criptografado. Digite a senha usada quando ele foi criado.");
            if (senha is null)
            {
                return;
            }
        }

        ConfirmarERestaurar(arquivo, senha);
    }

    private void ConfirmarERestaurar(string origem, string? senha)
    {
        if (!MostrarConfirmacao("Restaurar backup",
                $"Os dados atuais do ambiente \"{_contexto.RotuloAmbiente}\" serão substituídos pelos do backup:\n\n{origem}\n\n" +
                "Uma cópia de segurança do estado atual será feita antes. Deseja continuar?",
                "Restaurar"))
        {
            return;
        }

        OperacaoEmSegundoPlano.ComEspera("Restaurando o backup…", () => _contexto.RestaurarBackup().Executar(origem, senha), resultado =>
        {
            _contexto.NotificarAlteracao();

            var avisoDocumentos = resultado.DocumentosSemArquivo == 0
                ? string.Empty
                : $"\n\nAtenção: {resultado.DocumentosSemArquivo} documento(s) do backup não têm o arquivo correspondente — aparecem na lista, mas não abrem.";
            MostrarInformacao("Backup restaurado",
                $"Dados restaurados com sucesso.\n\nO estado anterior foi guardado em:\n{resultado.CaminhoCopiaSeguranca}" +
                (resultado.DocumentosRelocalizados > 0 ? $"\n\n{resultado.DocumentosRelocalizados} documento(s) tiveram o caminho ajustado para a pasta atual." : string.Empty) +
                avisoDocumentos);
        });
    }

    private static TextBlock Nota(string texto) =>
        UiApoio.Texto(texto, 11.5, FontWeights.Normal, (Brush)Application.Current.Resources["TextMuted"]);

    /// <summary>Sem pasta de destino, o primeiro clique em "Fazer backup agora" ou "Ativar backup automático" abre a escolha
    /// da pasta (antes, só mostrava um erro). Falso se o usuário desistiu.</summary>
    private bool GarantirDestino()
    {
        if (_contexto.ObterConfiguracaoBackup().Executar().CaminhoDestino is not null)
        {
            return true;
        }

        EscolherPastaBackup();
        return _contexto.ObterConfiguracaoBackup().Executar().CaminhoDestino is not null;
    }

    private void FazerBackupAgora()
    {
        if (!GarantirDestino())
        {
            return;
        }

        OperacaoEmSegundoPlano.ComEspera("Fazendo o backup…", () => _contexto.ExecutarBackupAgora().Executar(), caminho =>
        {
            FalhaAutomatica = null;
            _atualizarTela();
            MostrarInformacao("Backup concluído", $"Banco de dados e documentos copiados para:\n\n{caminho}");
        });
    }

    private void AlternarAutoBackup(ComboBox frequenciaCombo)
    {
        var configuracao = _contexto.ObterConfiguracaoBackup().Executar();
        if (configuracao.AutoBackupHabilitado)
        {
            _contexto.DesabilitarAutoBackup().Executar();
        }
        else
        {
            if (!GarantirDestino())
            {
                return;
            }

            _contexto.HabilitarAutoBackup().Executar(new HabilitarAutoBackupComando(ValorEnum<FrequenciaBackup>(frequenciaCombo)));
        }

        _atualizarTela();
    }

    private void EscolherPastaBackup()
    {
        var configuracao = _contexto.ObterConfiguracaoBackup().Executar();
        var pasta = SelecionarPasta("Pasta onde os backups serão salvos", configuracao.CaminhoDestino);
        if (pasta is null)
        {
            return;
        }

        if (_contexto.DestinoBackupConflitaComDados(pasta))
        {
            throw new InvalidOperationException("o destino do backup não pode ficar dentro da pasta de dados do Assetly. Escolha outra pasta (de preferência em outro disco ou pendrive).");
        }

        _contexto.DefinirDestinoBackup().Executar(new DefinirDestinoBackupComando(pasta));
        FalhaAutomatica = null;
        _atualizarTela();
    }

    private void AbrirPastaDestino()
    {
        var destino = _contexto.ObterConfiguracaoBackup().Executar().CaminhoDestino;
        if (string.IsNullOrWhiteSpace(destino) || !Directory.Exists(destino))
        {
            throw new InvalidOperationException($"a pasta de destino não está acessível no momento: {destino}");
        }

        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{destino}\"") { UseShellExecute = true });
    }

    private void RestaurarBackup()
    {
        var destino = _contexto.ObterConfiguracaoBackup().Executar().CaminhoDestino;
        var pasta = SelecionarPasta("Selecione a pasta do backup (Assetly-Backup-…)", destino);
        if (pasta is null)
        {
            return;
        }

        ConfirmarERestaurar(pasta, senha: null);
    }
}
