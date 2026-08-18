namespace Assetly.Dominio.Compartilhado;

public static class Validacao
{
    public static string ExigirTexto(string? valor, string nomeCampo, int tamanhoMaximo)
    {
        if (valor is null)
        {
            throw new ArgumentNullException(nameof(valor), $"{nomeCampo} é obrigatório");
        }

        var normalizado = valor.Trim();
        if (normalizado.Length == 0)
        {
            throw new ArgumentException($"{nomeCampo} não pode ficar em branco");
        }

        if (normalizado.Length > tamanhoMaximo)
        {
            throw new ArgumentException($"{nomeCampo} não pode exceder {tamanhoMaximo} caracteres");
        }

        return normalizado;
    }

    public static string TextoOpcional(string? valor, string nomeCampo, int tamanhoMaximo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        return ExigirTexto(valor, nomeCampo, tamanhoMaximo);
    }

    public static DateOnly ExigirDataNaoFutura(DateOnly? valor, string nomeCampo, DateOnly hoje)
    {
        if (valor is null)
        {
            throw new ArgumentNullException(nameof(valor), $"{nomeCampo} é obrigatória");
        }

        if (valor.Value > hoje)
        {
            throw new ArgumentException($"{nomeCampo} não pode estar no futuro");
        }

        return valor.Value;
    }

    public static decimal ExigirNaoNegativo(decimal valor, string nomeCampo)
    {
        if (valor < 0)
        {
            throw new ArgumentException($"{nomeCampo} não pode ser negativo");
        }

        return valor;
    }
}
