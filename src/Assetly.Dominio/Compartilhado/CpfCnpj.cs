namespace Assetly.Dominio.Compartilhado;

/// <summary>CPF (11 dígitos) ou CNPJ (14 dígitos) com os dígitos verificadores conferidos — guardado só com os números.</summary>
public static class CpfCnpj
{
    /// <summary>Só os dígitos de um CPF/CNPJ válido; vazio quando não informado. Lança se o número não confere.</summary>
    public static string Normalizar(string? texto, string nomeCampo)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var digitos = new string(texto.Where(char.IsAsciiDigit).ToArray());
        var valido = digitos.Length switch
        {
            11 => CpfValido(digitos),
            14 => CnpjValido(digitos),
            _ => false
        };
        return valido ? digitos : throw new ArgumentException($"{nomeCampo}: CPF ou CNPJ inválido (confira os números).");
    }

    public static string Formatar(string digitos) => digitos.Length switch
    {
        11 => $"{digitos[..3]}.{digitos[3..6]}.{digitos[6..9]}-{digitos[9..]}",
        14 => $"{digitos[..2]}.{digitos[2..5]}.{digitos[5..8]}/{digitos[8..12]}-{digitos[12..]}",
        _ => digitos
    };

    private static bool CpfValido(string d)
    {
        if (d.Distinct().Count() == 1)
        {
            return false;
        }

        return Digito(d[..9], [10, 9, 8, 7, 6, 5, 4, 3, 2]) == d[9] - '0'
            && Digito(d[..10], [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]) == d[10] - '0';
    }

    private static bool CnpjValido(string d)
    {
        if (d.Distinct().Count() == 1)
        {
            return false;
        }

        return Digito(d[..12], [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]) == d[12] - '0'
            && Digito(d[..13], [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]) == d[13] - '0';
    }

    private static int Digito(string numeros, int[] pesos)
    {
        var resto = numeros.Select((c, i) => (c - '0') * pesos[i]).Sum() % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
