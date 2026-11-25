using System.Windows;
using System.Windows.Threading;
using Assetly.Aplicacao.Servico;

namespace Assetly.Apresentacao.Visao;

internal enum EstadoAtualizacao
{
    /// <summary>Não instalado pelo instalador (Visual Studio, dotnet run, executável avulso).</summary>
    Indisponivel,
    Aguardando,
    Procurando,
    Baixando,
    /// <summary>Versão nova baixada, esperando o usuário reiniciar (ou será aplicada ao fechar).</summary>
    Pronta,
    Atualizado,
    Falhou
}

/// <summary>
/// Orquestra as atualizações do programa: procura ao abrir e a cada 6 horas (se a preferência estiver
/// ligada), baixa em segundo plano e deixa pronta para aplicar. Nunca reinicia sozinho no meio do uso:
/// oferece "reiniciar e atualizar" e, se o usuário não quiser, aplica quando o programa for fechado.
/// </summary>
internal sealed class GerenciadorAtualizacoes
{
    internal const string PaginaReleases = Infraestrutura.Atualizacao.ServicoAtualizacaoGithub.PaginaReleases;
    private static readonly TimeSpan IntervaloVerificacao = TimeSpan.FromHours(6);

    private readonly IServicoAtualizacao _servico;
    private readonly Func<bool> _automaticoAtivo;
    private readonly DispatcherTimer _temporizador;
    private bool _emAndamento;
    private bool _aplicacaoAgendada;
    private bool _reabrirDepois;

    internal EstadoAtualizacao Estado { get; private set; }
    internal string VersaoAtual => _servico.VersaoAtual;
    internal string? VersaoNova { get; private set; }
    internal int Progresso { get; private set; }
    internal string? Erro { get; private set; }
    internal DateTime? UltimaVerificacao { get; private set; }

    /// <summary>Disparado (na thread da interface) sempre que o estado muda.</summary>
    internal event Action? Mudou;

    internal GerenciadorAtualizacoes(IServicoAtualizacao servico, Func<bool> automaticoAtivo)
    {
        _servico = servico;
        _automaticoAtivo = automaticoAtivo;
        Estado = servico.Disponivel ? EstadoAtualizacao.Aguardando : EstadoAtualizacao.Indisponivel;
        _temporizador = new DispatcherTimer { Interval = IntervaloVerificacao };
        _temporizador.Tick += (_, _) => VerificarEmSegundoPlano();
    }

    internal bool Disponivel => _servico.Disponivel;

    /// <summary>Chamado depois que a janela principal aparece: a verificação não atrasa a abertura.</summary>
    internal void Iniciar()
    {
        if (!Disponivel)
        {
            return;
        }

        _temporizador.Start();
        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, VerificarEmSegundoPlano);
    }

    private void VerificarEmSegundoPlano()
    {
        if (_automaticoAtivo())
        {
            _ = VerificarAsync(manual: false);
        }
    }

    /// <summary>Procura e, se houver versão nova, já baixa. Na verificação manual, erros sobem para quem
    /// chamou mostrar ao usuário; na automática, ficam registrados (sem internet não é motivo de alarme).</summary>
    internal async Task VerificarAsync(bool manual)
    {
        if (!Disponivel)
        {
            if (manual)
            {
                throw new InvalidOperationException(
                    "a atualização automática só funciona no Assetly instalado pelo instalador (Assetly-Setup-….exe). " +
                    $"Esta cópia está rodando pela Visual Studio ou como executável avulso. As versões publicadas ficam em:\n{PaginaReleases}");
            }

            return;
        }

        if (_emAndamento || Estado == EstadoAtualizacao.Pronta)
        {
            return;
        }

        _emAndamento = true;
        try
        {
            Erro = null;
            Definir(EstadoAtualizacao.Procurando);
            var versao = await _servico.ProcurarAsync();
            UltimaVerificacao = DateTime.Now;
            if (versao is null)
            {
                Definir(EstadoAtualizacao.Atualizado);
                return;
            }

            VersaoNova = versao;
            Progresso = 0;
            Definir(EstadoAtualizacao.Baixando);
            await _servico.BaixarAsync(percentual => Application.Current.Dispatcher.BeginInvoke(() =>
            {
                Progresso = percentual;
                Mudou?.Invoke();
            }));
            Definir(EstadoAtualizacao.Pronta);
        }
        catch (Exception excecao) when (excecao is not OutOfMemoryException)
        {
            Erro = DialogoApoio.MensagemErro(excecao);
            RegistroErros.Registrar("Falha ao procurar/baixar atualização", excecao);
            Definir(EstadoAtualizacao.Falhou);
            if (manual)
            {
                throw new InvalidOperationException($"não foi possível verificar atualizações (verifique a conexão com a internet): {Erro}", excecao);
            }
        }
        finally
        {
            _emAndamento = false;
        }
    }

    internal void ReiniciarEAtualizar()
    {
        if (Estado != EstadoAtualizacao.Pronta)
        {
            throw new InvalidOperationException("nenhuma versão nova foi baixada ainda.");
        }

        // Fecha a janela como o usuário faria (o envio da sincronização e um backup em andamento terminam antes);
        // o instalador é iniciado ao sair, em AplicarAoFecharSePronta, e reabre o programa depois.
        _reabrirDepois = true;
        Application.Current.MainWindow?.Close();
    }

    /// <summary>Chamado ao fechar o programa: se há versão baixada, instala logo após o fechamento.</summary>
    internal void AplicarAoFecharSePronta()
    {
        if (Estado != EstadoAtualizacao.Pronta || _aplicacaoAgendada)
        {
            return;
        }

        try
        {
            _servico.IniciarInstalacao(_reabrirDepois);
            _aplicacaoAgendada = true;
        }
        catch (Exception excecao) when (excecao is not OutOfMemoryException)
        {
            RegistroErros.Registrar("Falha ao agendar atualização ao fechar", excecao);
        }
    }

    /// <summary>Texto curto do estado, para a tela de Configurações.</summary>
    internal string DescricaoEstado() => Estado switch
    {
        EstadoAtualizacao.Indisponivel => "Atualização automática disponível apenas na versão instalada pelo instalador.",
        EstadoAtualizacao.Aguardando => "Aguardando a primeira verificação.",
        EstadoAtualizacao.Procurando => "Procurando versão nova…",
        EstadoAtualizacao.Baixando => $"Baixando a versão {VersaoNova}… {Progresso}%",
        EstadoAtualizacao.Pronta => $"Versão {VersaoNova} pronta. Reinicie para atualizar — ou ela será instalada quando você fechar o Assetly.",
        EstadoAtualizacao.Atualizado => $"Você está na versão mais recente (verificado {Formatador.DataHora(UltimaVerificacao ?? DateTime.Now)}).",
        _ => $"A última verificação falhou: {Erro} Nova tentativa automática em algumas horas."
    };

    private void Definir(EstadoAtualizacao estado)
    {
        Estado = estado;
        Mudou?.Invoke();
    }
}
