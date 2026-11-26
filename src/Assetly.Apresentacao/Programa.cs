using System.Windows;

namespace Assetly.Apresentacao;

/// <summary>Ponto de entrada. Mostra a tela de abertura (splash) antes de qualquer carregamento pesado —
/// o WPF a fecha sozinho quando a janela principal aparece. No modo <c>--autoteste</c> (usado pelos testes
/// de release) não há splash nem janela.</summary>
public static class Programa
{
    [STAThread]
    public static int Main(string[] argumentos)
    {
        if (!argumentos.Contains(Autoteste.Argumento) && !argumentos.Contains(Visao.InicioComWindows.ArgumentoBandeja))
        {
            new SplashScreen("Recursos/Imagens/splash.png").Show(autoClose: true, topMost: false);
        }

        var app = new App();
        app.InitializeComponent();
        return app.Run();
    }
}
