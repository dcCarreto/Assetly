using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Assetly.Apresentacao.Visao;

/// <summary>
/// Cinco categorias semânticas de status usadas em toda a aplicação. Tanto o badge de uma célula
/// quanto o tingimento da linha da tabela resolvem para uma destas categorias antes de buscar cor —
/// única fonte de verdade para a paleta de status (ver Temas/Cores.xaml, tokens "Status*").
/// </summary>
internal enum StatusSemantica
{
    Ok,
    Informativo,
    Aviso,
    Perigo,
    Neutro
}

internal static class EtiquetaApoio
{
    internal static (Brush Fundo, Brush Borda, Brush Texto) Cores(string classeEtiqueta) =>
        Cores(ClassificarEtiqueta(classeEtiqueta));

    /// <summary>
    /// Fundo de linha de tabela — tingimento sutil (tokens "Status*RowTint"), deliberadamente mais
    /// discreto que o fundo do badge para que a maioria das linhas (status OK) não vire um bloco de
    /// cor só; OK resolve para transparente.
    /// </summary>
    internal static Brush LinhaFundo(string? classeLinha)
    {
        var prefixo = PrefixoStatus(ClassificarLinha(classeLinha));
        return (Brush)Application.Current.Resources[prefixo + "RowTint"];
    }

    internal static Brush LinhaBorda(string? classeLinha) => Cores(ClassificarLinha(classeLinha)).Borda;

    private static StatusSemantica ClassificarEtiqueta(string classeEtiqueta) => classeEtiqueta switch
    {
        "etiqueta-ativo" or "etiqueta-ativa" or "etiqueta-disponivel" or "etiqueta-concluida" or "etiqueta-resolvido"
            or "etiqueta-paga" or "etiqueta-em-dia" or "etiqueta-moradia-propria"
            => StatusSemantica.Ok,
        "etiqueta-informativo" or "etiqueta-aberto" or "etiqueta-agendada" or "etiqueta-pendente"
            or "etiqueta-alugado-para-terceiros" or "etiqueta-uso-comercial"
            => StatusSemantica.Informativo,
        "etiqueta-aviso" or "etiqueta-ciente" or "etiqueta-nao-iniciada" or "etiqueta-perto-do-vencimento"
            or "etiqueta-a-vencer" or "etiqueta-atencao" or "etiqueta-em-construcao"
            or "etiqueta-em-manutencao" or "etiqueta-ausente"
            => StatusSemantica.Aviso,
        "etiqueta-vencida" or "etiqueta-atrasada" or "etiqueta-critico" or "etiqueta-pendencia"
            => StatusSemantica.Perigo,
        // Cancelar ou descartar é uma decisão do usuário, não um problema: cinza, como arquivado.
        "etiqueta-arquivado" or "etiqueta-cancelada" or "etiqueta-descartado" => StatusSemantica.Neutro,
        _ => StatusSemantica.Neutro
    };

    private static StatusSemantica ClassificarLinha(string? classeLinha) => classeLinha switch
    {
        "linha-ok" => StatusSemantica.Ok,
        "linha-informativa" => StatusSemantica.Informativo,
        "linha-aviso" => StatusSemantica.Aviso,
        "linha-perigo" => StatusSemantica.Perigo,
        _ => StatusSemantica.Neutro
    };

    private static string PrefixoStatus(StatusSemantica semantica) => semantica switch
    {
        StatusSemantica.Ok => "StatusOk",
        StatusSemantica.Informativo => "StatusInfo",
        StatusSemantica.Aviso => "StatusAviso",
        StatusSemantica.Perigo => "StatusPerigo",
        _ => "StatusNeutro"
    };

    private static (Brush Fundo, Brush Borda, Brush Texto) Cores(StatusSemantica semantica)
    {
        var recursos = Application.Current.Resources;
        var prefixo = PrefixoStatus(semantica);
        return (
            (Brush)recursos[prefixo + "Bg"],
            (Brush)recursos[prefixo + "Border"],
            (Brush)recursos[prefixo + "Text"]);
    }

    internal static Border CriarEtiqueta(string texto, string classeCss)
    {
        var (fundo, borda, corTexto) = Cores(classeCss);
        return new Border
        {
            Style = (Style)Application.Current.Resources["BadgeStyle"],
            Background = fundo,
            BorderBrush = borda,
            Child = new TextBlock
            {
                Text = texto,
                Style = (Style)Application.Current.Resources["BadgeTextoStyle"],
                Foreground = corTexto
            }
        };
    }
}
