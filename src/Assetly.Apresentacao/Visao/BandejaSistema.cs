using System.Windows;
using Microsoft.Win32;
using Formularios = System.Windows.Forms;

namespace Assetly.Apresentacao.Visao;

/// <summary>Ícone na área de notificação (bandeja) e notificações do Windows — no Windows 10/11 a notificação do ícone
/// aparece como as demais notificações do sistema. Usado quando o Assetly fica aberto em segundo plano.</summary>
internal sealed class BandejaSistema : IDisposable
{
    private readonly Formularios.NotifyIcon _icone;

    internal event Action? AbrirSolicitado;
    internal event Action? AlertasSolicitados;
    internal event Action? SairSolicitado;

    internal BandejaSistema(string titulo)
    {
        var menu = new Formularios.ContextMenuStrip();
        menu.Items.Add("Abrir o Assetly", null, (_, _) => AbrirSolicitado?.Invoke());
        menu.Items.Add("Ver alertas", null, (_, _) => AlertasSolicitados?.Invoke());
        menu.Items.Add(new Formularios.ToolStripSeparator());
        menu.Items.Add("Sair", null, (_, _) => SairSolicitado?.Invoke());

        using var recurso = Application.GetResourceStream(new Uri("pack://application:,,,/Assetly;component/Recursos/Imagens/assetly.ico"))!.Stream;
        _icone = new Formularios.NotifyIcon
        {
            Text = titulo.Length > 63 ? titulo[..63] : titulo,
            Icon = new System.Drawing.Icon(recurso),
            ContextMenuStrip = menu,
            Visible = true
        };
        _icone.DoubleClick += (_, _) => AbrirSolicitado?.Invoke();
        _icone.BalloonTipClicked += (_, _) => AlertasSolicitados?.Invoke();
    }

    internal void Notificar(string titulo, string texto, bool critico) =>
        _icone.ShowBalloonTip(10_000, Limitar(titulo, 63), Limitar(texto, 255), critico ? Formularios.ToolTipIcon.Warning : Formularios.ToolTipIcon.Info);

    public void Dispose()
    {
        _icone.Visible = false;
        _icone.ContextMenuStrip?.Dispose();
        _icone.Icon?.Dispose();
        _icone.Dispose();
    }

    private static string Limitar(string texto, int maximo) => texto.Length <= maximo ? texto : texto[..(maximo - 1)] + "…";
}

/// <summary>Abrir com o Windows, já na bandeja: valor "Assetly" na chave Run do usuário (o desinstalador o remove).</summary>
internal static class InicioComWindows
{
    internal const string ArgumentoBandeja = "--bandeja";
    private const string Chave = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string Nome = "Assetly";

    private static string Comando => $"\"{Environment.ProcessPath}\" {ArgumentoBandeja}";

    internal static bool Ativo
    {
        get
        {
            using var chave = Registry.CurrentUser.OpenSubKey(Chave);
            return chave?.GetValue(Nome) is string;
        }
    }

    internal static void Definir(bool ativo)
    {
        using var chave = Registry.CurrentUser.CreateSubKey(Chave);
        if (ativo)
        {
            chave.SetValue(Nome, Comando);
        }
        else
        {
            chave.DeleteValue(Nome, throwOnMissingValue: false);
        }
    }

    /// <summary>Se o programa mudou de pasta (reinstalado em outro lugar), aponta a entrada para o executável atual.</summary>
    internal static void CorrigirCaminho()
    {
        using var chave = Registry.CurrentUser.OpenSubKey(Chave);
        if (chave?.GetValue(Nome) is string atual && atual != Comando)
        {
            Definir(true);
        }
    }
}
