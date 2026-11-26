using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;
using Assetly.Apresentacao;

namespace Assetly.Testes.Apresentacao;

/// <summary>Uma thread STA com a aplicação do Assetly carregada (tema e estilos de App.xaml), sem janela — os testes da
/// camada de interface rodam nela, como o autoteste do programa.</summary>
internal static class InterfaceWpf
{
    private static readonly Lazy<Dispatcher> Despachante = new(Iniciar);

    internal static void Executar(Action acao)
    {
        ExceptionDispatchInfo? falha = null;
        Despachante.Value.Invoke(() =>
        {
            try
            {
                acao();
            }
            catch (Exception excecao)
            {
                falha = ExceptionDispatchInfo.Capture(excecao);
            }
        });
        falha?.Throw();
    }

    private static Dispatcher Iniciar()
    {
        Dispatcher? despachante = null;
        using var pronto = new ManualResetEventSlim();
        var thread = new Thread(() =>
        {
            var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.InitializeComponent();
            despachante = Dispatcher.CurrentDispatcher;
            pronto.Set();
            Dispatcher.Run();
        })
        {
            IsBackground = true,
            Name = "Testes de interface (STA)"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        pronto.Wait();
        return despachante!;
    }
}
