using Assetly.Dominio.Compartilhado;

namespace Assetly.Dominio.Bem;

/// <summary>Dados de inventário do bem — os que o seguro, o boletim de ocorrência e o pedido de garantia pedem.
/// Todos opcionais; texto vazio = não informado.</summary>
public sealed record InventarioBem
{
    public const int TamanhoMaximo = 120;

    public static InventarioBem Vazio { get; } = new(null, null, null, null, null);

    public string Marca { get; }
    public string Modelo { get; }
    public string NumeroSerie { get; }
    public string Loja { get; }

    /// <summary>Onde o bem fica (cômodo, endereço, garagem...).</summary>
    public string Localizacao { get; }

    public InventarioBem(string? marca, string? modelo, string? numeroSerie, string? loja, string? localizacao)
    {
        Marca = Validacao.TextoOpcional(marca, "marca", TamanhoMaximo);
        Modelo = Validacao.TextoOpcional(modelo, "modelo", TamanhoMaximo);
        NumeroSerie = Validacao.TextoOpcional(numeroSerie, "número de série", TamanhoMaximo);
        Loja = Validacao.TextoOpcional(loja, "loja", TamanhoMaximo);
        Localizacao = Validacao.TextoOpcional(localizacao, "localização", TamanhoMaximo);
    }
}
