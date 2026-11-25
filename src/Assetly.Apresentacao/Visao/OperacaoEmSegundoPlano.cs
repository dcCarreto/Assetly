using System.Runtime.ExceptionServices;

namespace Assetly.Apresentacao.Visao;

/// <summary>
/// Trabalho pesado (backup, restauração, sincronização: copiar documentos, compactar, derivar a chave da senha)
/// fora da thread da interface, para a janela não congelar. As operações rodam uma de cada vez, na ordem em que
/// foram pedidas — um backup manual pedido durante um backup automático espera o automático terminar.
/// <para><see cref="ComEspera"/> (ações do usuário e o que pode substituir os dados) mostra o painel de espera,
/// que bloqueia a tela e pausa a atualização automática; <see cref="Silenciosa"/> (tarefas automáticas que só
/// leem os dados) não bloqueia nada. O trabalho não pode tocar em controles: o que mexe na tela vai em
/// <c>concluir</c>, que volta para a thread da interface.</para>
/// </summary>
internal static class OperacaoEmSegundoPlano
{
    private static Task _atual = Task.CompletedTask;

    /// <summary>Mostra (texto) ou esconde (nulo) o painel de espera — registrado pela tela principal.</summary>
    internal static Action<string?>? AlternarEspera { get; set; }

    internal static bool EmAndamento => !_atual.IsCompleted;

    /// <summary>Termina quando a última operação pedida terminar (usado ao fechar o programa).</summary>
    internal static Task Atual => _atual;

    internal static async void ComEspera<T>(string mensagem, Func<T> trabalho, Action<T> concluir)
    {
        AlternarEspera?.Invoke(mensagem);
        T resultado;
        try
        {
            resultado = await Encadear(trabalho);
        }
        catch (Exception excecao) when (excecao is not OutOfMemoryException)
        {
            AlternarEspera?.Invoke(null);
            // Mesmo tratamento das ações síncronas: mensagem ao usuário e, se inesperado, registro no log.
            AcoesApoio.ExecutarComTratamento(() => ExceptionDispatchInfo.Capture(excecao).Throw());
            return;
        }

        AlternarEspera?.Invoke(null);
        AcoesApoio.ExecutarComTratamento(() => concluir(resultado));
    }

    /// <summary>Tarefa automática: não é iniciada se outra operação ainda estiver em andamento (fica para o
    /// próximo ciclo). O trabalho deve tratar as próprias falhas; uma exceção que escape só vai para o log.</summary>
    internal static async void Silenciosa<T>(Func<T> trabalho, Action<T> concluir)
    {
        if (EmAndamento)
        {
            return;
        }

        try
        {
            var resultado = await Encadear(trabalho);
            concluir(resultado);
        }
        catch (Exception excecao) when (excecao is not OutOfMemoryException)
        {
            RegistroErros.Registrar("Erro em tarefa automática", excecao);
        }
    }

    private static Task<T> Encadear<T>(Func<T> trabalho)
    {
        var tarefa = _atual.ContinueWith(_ => trabalho(), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
        _atual = tarefa;
        return tarefa;
    }
}
