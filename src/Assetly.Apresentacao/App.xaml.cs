using System.Windows;
using System.Windows.Media.Imaging;
using Assetly.Apresentacao.Visao;

namespace Assetly.Apresentacao;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var contexto = ContextoAplicacaoParaArgumentos(e.Args);
        var telaPrincipal = new TelaPrincipal(contexto);
        var titulo = contexto.AmbienteTeste ? "Assetly - Teste" : "Assetly";

        var janela = new Window
        {
            Title = titulo,
            Width = 1180,
            Height = 790,
            MinWidth = 980,
            MinHeight = 670,
            Icon = new BitmapImage(new Uri("pack://application:,,,/Assetly;component/Recursos/Imagens/assetly-icon-32.png"))
        };

        JanelaEscura.Decorar(janela, titulo, telaPrincipal.Raiz);
        MainWindow = janela;
        janela.Show();
    }

    private static ContextoAplicacao ContextoAplicacaoParaArgumentos(string[] argumentos) =>
        UsarAmbienteTeste(argumentos) ? ContextoAplicacao.TesteComDadosFicticios() : ContextoAplicacao.LocalPadrao();

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
