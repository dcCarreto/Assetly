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
    private string? _falhaAutomatica;
    private string? _falhaSincronizacao;
    private DateTime _ultimoEnvioAutomatico = DateTime.Now;
    private static readonly TimeSpan IntervaloEnvioAutomatico = TimeSpan.FromMinutes(10);

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
        var cardDestino = CardConfiguracao("Destino do backup", UiApoio.Coluna(10, linhaDestino, dicaDestino));

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
        var notaRestaurar = Nota("Substitui os dados atuais pelos de um backup (arquivo \"Assetly-Backup-….assetly\" ou pasta \"Assetly-Backup-…\"). Antes, o estado atual é copiado automaticamente para a pasta de segurança — a restauração pode ser desfeita.");
        notaRestaurar.TextWrapping = TextWrapping.Wrap;
        var cardRestaurar = CardConfiguracao("Restaurar", UiApoio.Coluna(10, UiApoio.Linha(8, restaurarArquivo, restaurar), notaRestaurar));

        // Proteção por senha (AES-256): vale para backups manuais, automáticos, cópias em arquivo e sincronização.
        var statusSenha = Nota(string.Empty);
        statusSenha.TextWrapping = TextWrapping.Wrap;
        var botaoSenha = BotaoSecundario("Proteger com senha…", AlternarSenha);
        var exportarArquivo = BotaoSecundario("Salvar cópia em arquivo…", SalvarCopiaEmArquivo);
        var cardSenha = CardConfiguracao("Proteção por senha", UiApoio.Coluna(10, UiApoio.Linha(8, botaoSenha, exportarArquivo), statusSenha));

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

        var direitaCabecalho = UiApoio.Linha(12, ultimoTexto, executarAgora);
        var cabecalho = CabecalhoTela("Backup", "Escolha onde e com que frequência seus dados são copiados.", direitaCabecalho);
        cabecalho.Margin = new Thickness(0, 0, 0, 14);

        var cartoes = UiApoio.Coluna(14, cardDestino, cardAuto, cardSenha, cardSync, cardRestaurar);

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

            ultimoTexto.Text = configuracao.UltimoBackupEm is null
                ? "nenhum backup realizado ainda"
                : $"último backup {Formatador.DataHora(configuracao.UltimoBackupEm.Value.LocalDateTime)}";

            toggleAuto.Content = configuracao.AutoBackupHabilitado ? "Desativar backup automático" : "Ativar backup automático";
            frequenciaCombo.IsEnabled = configuracao.CaminhoDestino is not null;
            sincronizandoCombo = true;
            var itens = (IEnumerable<ItemEnum<FrequenciaBackup>>)frequenciaCombo.ItemsSource;
            frequenciaCombo.SelectedItem = itens.First(item => item.Valor == configuracao.Frequencia);
            sincronizandoCombo = false;

            statusAuto.Foreground = (Brush)recursos[_falhaAutomatica is null ? "TextMuted" : "StatusPerigoText"];
            statusAuto.Text = _falhaAutomatica is not null
                ? $"Último backup automático falhou: {_falhaAutomatica} Uma nova tentativa é feita no próximo ciclo."
                : !configuracao.AutoBackupHabilitado
                    ? configuracao.CaminhoDestino is null
                        ? "Escolha uma pasta de destino para poder ativar o backup automático."
                        : "Backup automático desativado."
                    : $"Backup automático ativo — frequência {Formatador.TextoEnum(configuracao.Frequencia).ToLowerInvariant()}. Executado enquanto o Assetly estiver aberto.";

            botaoSenha.Content = configuracao.Criptografado ? "Remover senha…" : "Proteger com senha…";
            statusSenha.Foreground = (Brush)recursos[configuracao.Criptografado ? "StatusOkText" : "TextMuted"];
            statusSenha.Text = configuracao.Criptografado
                ? "Ativa: os backups são gravados num único arquivo .assetly criptografado (AES-256). Sem a senha não é possível restaurá-los — guarde-a num lugar seguro."
                : "Desativada: os backups ficam numa pasta comum, legível por quem tiver acesso ao destino. Ative para criptografar (recomendado em pendrive ou nuvem).";

            var sincronizacao = _contexto.ConfigurarSincronizacao().Obter();
            sincronizarAgora.IsEnabled = sincronizacao.Ativa;
            desligarSync.IsEnabled = sincronizacao.Ativa;
            statusSync.Foreground = (Brush)recursos[_falhaSincronizacao is not null ? "StatusPerigoText" : sincronizacao.Ativa ? "StatusOkText" : "TextMuted"];
            statusSync.Text = _falhaSincronizacao is not null
                ? $"Última sincronização falhou: {_falhaSincronizacao}"
                : !sincronizacao.Ativa
                    ? "Desligada."
                    : $"Ligada — pasta {sincronizacao.Pasta}. " + (sincronizacao.UltimaSincronizacaoEm is { } quando
                        ? $"Última sincronização {Formatador.DataHora(quando.LocalDateTime)}."
                        : "Ainda não sincronizado.");
        };
        _atualizarTela();
    }

    /// <summary>Falha de backup automático (destino indisponível, sem permissão etc.) não interrompe o
    /// usuário com um diálogo a cada ciclo: fica registrada no card (e no log) e o backup é tentado de
    /// novo no próximo ciclo, sem ser marcado como concluído.</summary>
    public void Atualizar()
    {
        try
        {
            _contexto.VerificarEExecutarBackupAutomatico().Executar(DateTimeOffset.Now);
            _falhaAutomatica = null;
        }
        catch (Exception excecao) when (excecao is not OutOfMemoryException)
        {
            if (_falhaAutomatica is null)
            {
                RegistroErros.Registrar("Falha no backup automático", excecao);
            }

            _falhaAutomatica = MensagemErro(excecao);
        }

        // Durante o uso, só envia (nunca substitui os dados sem o usuário pedir): a cada 10 minutos, se algo mudou.
        if (DateTime.Now - _ultimoEnvioAutomatico >= IntervaloEnvioAutomatico)
        {
            _ultimoEnvioAutomatico = DateTime.Now;
            SincronizarAutomaticamente(somenteEnviar: true);
        }

        _atualizarTela();
    }

    /// <summary>Sincronização automática (ao abrir: envia ou recebe; durante o uso e ao fechar: só envia).
    /// Falhas não interrompem o usuário — ficam registradas no card. Conflito ao abrir pergunta ao usuário;
    /// nos demais momentos, fica para a próxima sincronização manual ou abertura.</summary>
    internal void SincronizarAutomaticamente(bool somenteEnviar)
    {
        if (!_contexto.ConfigurarSincronizacao().Obter().Ativa)
        {
            return;
        }

        try
        {
            var resultado = _contexto.SincronizarDados().Executar(remoto => somenteEnviar ? EscolhaConflito.Cancelar : PerguntarConflito(remoto));
            _falhaSincronizacao = resultado.Acao == AcaoSincronizacao.Conflito && !resultado.Executada && somenteEnviar
                ? "os dados mudaram aqui e no outro computador. Clique em \"Sincronizar agora\" para escolher qual versão manter."
                : null;
            if (resultado.Acao == AcaoSincronizacao.Receber && resultado.Executada)
            {
                _contexto.NotificarAlteracao();
            }
        }
        catch (Exception excecao) when (excecao is not OutOfMemoryException)
        {
            if (_falhaSincronizacao is null)
            {
                RegistroErros.Registrar("Falha na sincronização automática", excecao);
            }

            _falhaSincronizacao = MensagemErro(excecao);
        }

        _atualizarTela?.Invoke();
    }

    private void Sincronizar(bool manual)
    {
        _falhaSincronizacao = null;
        var resultado = _contexto.SincronizarDados().Executar(PerguntarConflito);
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
    }

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

        var senha = CampoSenha();
        var confirmacao = CampoSenha();
        var aviso = Nota($"Mínimo de {Aplicacao.CasoDeUso.SenhaBackup.TamanhoMinimo} caracteres. A senha fica guardada neste computador protegida pelo Windows (só sua conta consegue lê-la), " +
            "para que os backups automáticos funcionem. Se você esquecer a senha, os backups criptografados não poderão ser restaurados.");
        aviso.TextWrapping = TextWrapping.Wrap;
        var grade = GradeFormulario();
        AdicionarCampo(grade, 0, 0, "Senha *", senha);
        AdicionarCampo(grade, 0, 1, "Confirme a senha *", confirmacao);
        Grid.SetRow(aviso, 1);
        Grid.SetColumnSpan(aviso, 2);
        aviso.Margin = new Thickness(0, 14, 0, 0);
        grade.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grade.Children.Add(aviso);
        if (MostrarFormulario("Proteger backups com senha", grade, () => _contexto.AtivarCriptografiaBackup().Executar(senha.Password, confirmacao.Password), largura: 560))
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

        _contexto.GerarArquivoBackup(dialogo.FileName);
        MostrarInformacao("Cópia salva", $"Banco de dados e documentos salvos em um único arquivo{(protegido ? ", criptografado com a senha dos backups" : " (sem senha)")}:\n\n{dialogo.FileName}");
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

        var resultado = _contexto.RestaurarBackup().Executar(origem, senha);
        _contexto.NotificarAlteracao();

        var avisoDocumentos = resultado.DocumentosSemArquivo == 0
            ? string.Empty
            : $"\n\nAtenção: {resultado.DocumentosSemArquivo} documento(s) do backup não têm o arquivo correspondente — aparecem na lista, mas não abrem.";
        MostrarInformacao("Backup restaurado",
            $"Dados restaurados com sucesso.\n\nO estado anterior foi guardado em:\n{resultado.CaminhoCopiaSeguranca}" +
            (resultado.DocumentosRelocalizados > 0 ? $"\n\n{resultado.DocumentosRelocalizados} documento(s) tiveram o caminho ajustado para a pasta atual." : string.Empty) +
            avisoDocumentos);
    }

    private static TextBlock Nota(string texto) =>
        UiApoio.Texto(texto, 11.5, FontWeights.Normal, (Brush)Application.Current.Resources["TextMuted"]);

    private void FazerBackupAgora()
    {
        var caminho = _contexto.ExecutarBackupAgora().Executar();
        _falhaAutomatica = null;
        _atualizarTela();
        MostrarInformacao("Backup concluído", $"Banco de dados e documentos copiados para:\n\n{caminho}");
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
        _falhaAutomatica = null;
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
