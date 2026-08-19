using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Assetly.Apresentacao.Visao;

internal static class EtiquetaApoio
{
    internal static (Brush Fundo, Brush Borda, Brush Texto) Cores(string classeEtiqueta)
    {
        var recursos = Application.Current.Resources;
        return classeEtiqueta switch
        {
            "etiqueta-ativo" or "etiqueta-ativa" or "etiqueta-disponivel" or "etiqueta-concluida" or "etiqueta-resolvido"
                => ((Brush)recursos["EtiquetaOkFundo"], (Brush)recursos["EtiquetaOkBorda"], (Brush)recursos["EtiquetaOkTexto"]),
            "etiqueta-informativo" or "etiqueta-aberto" or "etiqueta-agendada"
                => ((Brush)recursos["EtiquetaInfoFundo"], (Brush)recursos["EtiquetaInfoBorda"], (Brush)recursos["EtiquetaInfoTexto"]),
            "etiqueta-aviso" or "etiqueta-ciente" or "etiqueta-nao-iniciada" or "etiqueta-perto-do-vencimento"
                or "etiqueta-em-manutencao" or "etiqueta-ausente"
                => ((Brush)recursos["EtiquetaAvisoFundo"], (Brush)recursos["EtiquetaAvisoBorda"], (Brush)recursos["EtiquetaAvisoTexto"]),
            "etiqueta-vencida" or "etiqueta-atrasada" or "etiqueta-cancelada" or "etiqueta-descartado" or "etiqueta-critico"
                => ((Brush)recursos["EtiquetaPerigoFundo"], (Brush)recursos["EtiquetaPerigoBorda"], (Brush)recursos["EtiquetaPerigoTexto"]),
            "etiqueta-arquivado"
                => ((Brush)recursos["EtiquetaArquivadoFundo"], (Brush)recursos["EtiquetaArquivadoBorda"], (Brush)recursos["EtiquetaArquivadoTexto"]),
            _ => ((Brush)recursos["EtiquetaNeutraFundo"], (Brush)recursos["EtiquetaNeutraBorda"], (Brush)recursos["EtiquetaNeutraTexto"])
        };
    }

    internal static Brush LinhaFundo(string? classeLinha)
    {
        var recursos = Application.Current.Resources;
        return classeLinha switch
        {
            "linha-ok" => (Brush)recursos["LinhaOkBrush"],
            "linha-informativa" => (Brush)recursos["LinhaInformativaBrush"],
            "linha-aviso" => (Brush)recursos["LinhaAvisoBrush"],
            "linha-perigo" => (Brush)recursos["LinhaPerigoBrush"],
            "linha-neutra" => (Brush)recursos["LinhaNeutraBrush"],
            _ => (Brush)recursos["CardFundoBrush"]
        };
    }

    internal static Border CriarEtiqueta(string texto, string classeCss)
    {
        var (fundo, borda, corTexto) = Cores(classeCss);
        return new Border
        {
            Style = (Style)Application.Current.Resources["EtiquetaStyle"],
            Background = fundo,
            BorderBrush = borda,
            Child = new TextBlock
            {
                Text = texto,
                Style = (Style)Application.Current.Resources["EtiquetaTextoStyle"],
                Foreground = corTexto
            }
        };
    }
}
