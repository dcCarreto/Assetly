using System.Threading;
using Assetly.Infraestrutura.Seguranca;
using static Assetly.Apresentacao.Visao.DialogoApoio;

namespace Assetly.Apresentacao.Visao;

/// <summary>Pede o PIN de abertura (ao abrir o programa, ao trazê-lo da bandeja e antes de trocar ou remover o PIN).</summary>
internal static class DesbloqueioDialogo
{
    /// <summary>Verdadeiro quando o PIN conferiu (ou não há bloqueio); falso se o usuário desistiu.</summary>
    internal static bool Pedir(BloqueioPin bloqueio, string titulo = "Assetly bloqueado")
    {
        if (!bloqueio.Ativo)
        {
            return true;
        }

        var pin = CampoSenha();
        var grade = GradeFormulario();
        AdicionarCampo(grade, 0, 0, "PIN *", pin, ocupaLinhaToda: true);
        var falhas = 0;
        return MostrarFormulario(titulo, grade, () =>
        {
            if (bloqueio.Conferir(pin.Password))
            {
                return;
            }

            falhas++;
            pin.Clear();
            if (falhas >= 3)
            {
                // Cada erro a mais demora um pouco mais: desestimula tentar PINs em sequência.
                Thread.Sleep(Math.Min(falhas, 10) * 500);
            }

            throw new ArgumentException("PIN incorreto.");
        }, largura: 420, textoSalvar: "Desbloquear");
    }

    /// <summary>Define (ou troca) o PIN. Devolve falso se o usuário cancelou.</summary>
    internal static bool Definir(BloqueioPin bloqueio)
    {
        var pin = CampoSenha();
        var confirmacao = CampoSenha();
        var grade = GradeFormulario();
        AdicionarCampo(grade, 0, 0, "Novo PIN *", pin);
        AdicionarCampo(grade, 0, 1, "Confirme o PIN *", confirmacao);
        var nota = UiApoio.Texto($"Mínimo de {BloqueioPin.TamanhoMinimo} caracteres (números ou letras). Ele é pedido ao abrir o Assetly e ao trazê-lo da bandeja.",
            11.5, System.Windows.FontWeights.Normal, (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["TextMuted"]);
        nota.TextWrapping = System.Windows.TextWrapping.Wrap;
        AdicionarCampo(grade, 1, 0, "Como funciona", nota, ocupaLinhaToda: true);
        return MostrarFormulario(bloqueio.Ativo ? "Trocar o PIN" : "Definir PIN de abertura", grade,
            () => bloqueio.Definir(pin.Password, confirmacao.Password), largura: 520);
    }
}
