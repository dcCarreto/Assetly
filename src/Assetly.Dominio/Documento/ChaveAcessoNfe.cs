using System.Globalization;

namespace Assetly.Dominio.Documento;

/// <summary>
/// Chave de acesso de uma NF-e/NFC-e (44 dígitos), com as informações que ela mesma carrega:
/// UF (2), ano/mês de emissão (4), CNPJ do emitente (14), modelo (2: 55 = NF-e, 65 = NFC-e), série (3),
/// número (9), tipo de emissão (1), código numérico (8) e dígito verificador (1, módulo 11).
/// </summary>
public sealed record ChaveAcessoNfe
{
    private static readonly Dictionary<string, string> Ufs = new()
    {
        ["11"] = "RO", ["12"] = "AC", ["13"] = "AM", ["14"] = "RR", ["15"] = "PA", ["16"] = "AP", ["17"] = "TO",
        ["21"] = "MA", ["22"] = "PI", ["23"] = "CE", ["24"] = "RN", ["25"] = "PB", ["26"] = "PE", ["27"] = "AL",
        ["28"] = "SE", ["29"] = "BA", ["31"] = "MG", ["32"] = "ES", ["33"] = "RJ", ["35"] = "SP", ["41"] = "PR",
        ["42"] = "SC", ["43"] = "RS", ["50"] = "MS", ["51"] = "MT", ["52"] = "GO", ["53"] = "DF"
    };

    public string Valor { get; }

    private ChaveAcessoNfe(string valor) => Valor = valor;

    /// <summary>Aceita a chave com espaços, pontos ou outros separadores. Lança exceção se não for uma
    /// chave válida (44 dígitos, UF existente, mês válido e dígito verificador correto).</summary>
    public static ChaveAcessoNfe Criar(string? texto) =>
        TentarCriar(texto) ?? throw new ArgumentException("chave de acesso inválida: são 44 dígitos, com dígito verificador correto.");

    public static ChaveAcessoNfe? TentarCriar(string? texto)
    {
        var digitos = new string((texto ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digitos.Length != 44 || !Ufs.ContainsKey(digitos[..2]))
        {
            return null;
        }

        var mes = int.Parse(digitos.Substring(4, 2), CultureInfo.InvariantCulture);
        if (mes is < 1 or > 12 || DigitoVerificador(digitos[..43]) != digitos[43] - '0')
        {
            return null;
        }

        return new ChaveAcessoNfe(digitos);
    }

    public string Uf => Ufs[Valor[..2]];

    /// <summary>Primeiro dia do mês de emissão (a chave só traz ano e mês).</summary>
    public DateOnly MesEmissao => new(2000 + int.Parse(Valor.Substring(2, 2), CultureInfo.InvariantCulture), int.Parse(Valor.Substring(4, 2), CultureInfo.InvariantCulture), 1);

    public string CnpjEmitente => Valor.Substring(6, 14);

    public string CnpjFormatado => $"{CnpjEmitente[..2]}.{CnpjEmitente[2..5]}.{CnpjEmitente[5..8]}/{CnpjEmitente[8..12]}-{CnpjEmitente[12..]}";

    public string Modelo => Valor.Substring(20, 2) switch
    {
        "65" => "NFC-e",
        "55" => "NF-e",
        var outro => $"modelo {outro}"
    };

    public int Serie => int.Parse(Valor.Substring(22, 3), CultureInfo.InvariantCulture);

    public long Numero => long.Parse(Valor.Substring(25, 9), CultureInfo.InvariantCulture);

    /// <summary>Chave em blocos de 4 dígitos, como impressa no DANFE.</summary>
    public string Formatada => string.Join(' ', Enumerable.Range(0, 11).Select(i => Valor.Substring(i * 4, 4)));

    /// <summary>Dígito verificador (módulo 11) dos 43 primeiros dígitos da chave.</summary>
    public static int DigitoVerificador(string base43)
    {
        var soma = 0;
        var peso = 2;
        for (var i = base43.Length - 1; i >= 0; i--)
        {
            soma += (base43[i] - '0') * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
