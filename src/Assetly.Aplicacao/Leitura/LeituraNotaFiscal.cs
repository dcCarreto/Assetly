using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Assetly.Dominio.Documento;

namespace Assetly.Aplicacao.Leitura;

/// <summary>O que foi possível extrair de uma nota fiscal (QR Code e/ou texto reconhecido por OCR).
/// Todos os campos são opcionais: o usuário confere e completa no formulário.</summary>
public sealed record DadosNotaFiscal(
    ChaveAcessoNfe? Chave,
    string? UrlConsulta,
    DateOnly? Emissao,
    decimal? ValorTotal,
    string? CnpjEmitente)
{
    public static DadosNotaFiscal Vazio { get; } = new(null, null, null, null, null);

    public bool TemAlgo => Chave is not null || Emissao is not null || ValorTotal is not null || CnpjEmitente is not null;

    /// <summary>Junta duas leituras: o que esta tiver vence; o que faltar vem da outra.</summary>
    public DadosNotaFiscal Completar(DadosNotaFiscal outra) => new(
        Chave ?? outra.Chave,
        UrlConsulta ?? outra.UrlConsulta,
        Emissao ?? outra.Emissao,
        ValorTotal ?? outra.ValorTotal,
        CnpjEmitente ?? outra.CnpjEmitente ?? (Chave ?? outra.Chave)?.CnpjFormatado);
}

/// <summary>
/// Interpreta o conteúdo do QR Code da NFC-e (versões 1 e 2 do padrão nacional) e o texto de uma nota
/// lido por OCR. Funções puras — a leitura da imagem em si fica na infraestrutura/interface.
/// </summary>
public static partial class LeituraNotaFiscal
{
    private static readonly CultureInfo Invariante = CultureInfo.InvariantCulture;

    /// <summary>QR Code da NFC-e: URL da Sefaz com <c>p=chave|versão|ambiente|...</c> (v2) ou
    /// <c>chNFe=...&amp;vNF=...&amp;dhEmi=...</c> (v1). Na v2 em contingência, traz também dia e valor.</summary>
    public static DadosNotaFiscal InterpretarQrCode(string? conteudo, DateOnly hoje)
    {
        if (string.IsNullOrWhiteSpace(conteudo))
        {
            return DadosNotaFiscal.Vazio;
        }

        var texto = conteudo.Trim();
        var url = texto.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? texto : null;
        var parametros = Parametros(texto);

        ChaveAcessoNfe? chave = null;
        DateOnly? emissao = null;
        decimal? valor = null;

        if (parametros.TryGetValue("p", out var p))
        {
            var partes = p.Split('|');
            chave = ChaveAcessoNfe.TentarCriar(partes[0]);
            // v2 em contingência (offline): chave|versão|ambiente|dia|valor|digVal|idCSC|hash
            if (chave is not null && partes.Length >= 8 && int.TryParse(partes[3], NumberStyles.Integer, Invariante, out var dia)
                && dia is >= 1 and <= 31)
            {
                emissao = DiaNoMesDaChave(chave, dia);
                valor = Decimal(partes[4]);
            }
        }
        else if (parametros.TryGetValue("chNFe", out var chNfe))
        {
            chave = ChaveAcessoNfe.TentarCriar(chNfe);
            valor = parametros.TryGetValue("vNF", out var vNf) ? Decimal(vNf) : null;
            emissao = parametros.TryGetValue("dhEmi", out var dhEmi) ? DataHexadecimal(dhEmi) : null;
        }
        else
        {
            chave = ChaveAcessoNfe.TentarCriar(texto);
        }

        if (emissao > hoje)
        {
            emissao = null;
        }

        return new DadosNotaFiscal(chave, chave is null ? null : url, emissao, valor, chave?.CnpjFormatado);
    }

    /// <summary>Texto de nota/cupom lido por OCR: procura chave de acesso, CNPJ, data de emissão e valor
    /// total (linha com "TOTAL", "VALOR A PAGAR"...). Tolerante a acentos perdidos e espaços extras.</summary>
    public static DadosNotaFiscal InterpretarTexto(string? texto, DateOnly hoje)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return DadosNotaFiscal.Vazio;
        }

        var normalizado = SemAcentos(texto).ToUpperInvariant();

        ChaveAcessoNfe? chave = null;
        foreach (Match candidato in RegexChave().Matches(normalizado))
        {
            chave = ChaveAcessoNfe.TentarCriar(candidato.Value);
            if (chave is not null)
            {
                break;
            }
        }

        var cnpj = RegexCnpj().Match(normalizado) is { Success: true } achado ? FormatarCnpj(achado.Value) : null;

        DateOnly? emissao = null;
        foreach (Match data in RegexData().Matches(normalizado))
        {
            if (DateOnly.TryParseExact(data.Value.Replace('-', '/').Replace('.', '/'), ["dd/MM/yyyy", "dd/MM/yy"], Invariante, DateTimeStyles.None, out var lida)
                && lida <= hoje && lida.Year >= 2000)
            {
                emissao = lida;
                break;
            }
        }

        return new DadosNotaFiscal(chave, null, emissao, ValorTotal(normalizado), cnpj ?? chave?.CnpjFormatado);
    }

    private static decimal? ValorTotal(string texto)
    {
        var linhas = texto.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string[] palavras = ["VALOR A PAGAR", "VALOR TOTAL", "TOTAL A PAGAR", "TOTAL R$", "VL TOTAL", "TOTAL"];
        foreach (var palavra in palavras)
        {
            for (var i = 0; i < linhas.Length; i++)
            {
                if (!linhas[i].Contains(palavra, StringComparison.Ordinal) || linhas[i].Contains("TRIBUTO", StringComparison.Ordinal)
                    || linhas[i].Contains("ITENS", StringComparison.Ordinal) || linhas[i].Contains("DESCONTO", StringComparison.Ordinal))
                {
                    continue;
                }

                // O valor costuma estar na mesma linha ou, no OCR de cupons em colunas, na linha seguinte.
                var alvo = RegexValor().Match(linhas[i]).Success ? linhas[i] : i + 1 < linhas.Length ? linhas[i + 1] : string.Empty;
                var valores = RegexValor().Matches(alvo).Select(m => Decimal(m.Value)).Where(v => v is > 0).ToList();
                if (valores.Count > 0)
                {
                    return valores[^1];
                }
            }
        }

        return null;
    }

    private static Dictionary<string, string> Parametros(string texto)
    {
        var resultado = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var inicio = texto.IndexOf('?');
        if (inicio < 0)
        {
            return resultado;
        }

        foreach (var par in texto[(inicio + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separador = par.IndexOf('=');
            if (separador > 0)
            {
                resultado[par[..separador]] = Uri.UnescapeDataString(par[(separador + 1)..]);
            }
        }

        return resultado;
    }

    private static DateOnly? DiaNoMesDaChave(ChaveAcessoNfe chave, int dia)
    {
        var mes = chave.MesEmissao;
        return dia <= DateTime.DaysInMonth(mes.Year, mes.Month) ? new DateOnly(mes.Year, mes.Month, dia) : null;
    }

    /// <summary>Na v1 do QR, dhEmi vem em hexadecimal dos caracteres ASCII de uma data ISO.</summary>
    private static DateOnly? DataHexadecimal(string hexadecimal)
    {
        try
        {
            var texto = hexadecimal.Length % 2 == 0 && hexadecimal.All(Uri.IsHexDigit)
                ? Encoding.ASCII.GetString(Convert.FromHexString(hexadecimal))
                : hexadecimal;
            return DateTimeOffset.TryParse(texto, Invariante, DateTimeStyles.None, out var momento) ? DateOnly.FromDateTime(momento.Date) : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static decimal? Decimal(string texto)
    {
        try
        {
            return Dados.ValorMonetario.InterpretarOpcional(texto);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string FormatarCnpj(string texto)
    {
        var d = new string(texto.Where(char.IsDigit).ToArray());
        return $"{d[..2]}.{d[2..5]}.{d[5..8]}/{d[8..12]}-{d[12..]}";
    }

    private static string SemAcentos(string texto)
    {
        var decomposto = texto.Normalize(NormalizationForm.FormD);
        return new string(decomposto.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }

    [GeneratedRegex(@"(?<!\d)(?:\d[\s.]?){43}\d(?!\d)")]
    private static partial Regex RegexChave();

    [GeneratedRegex(@"\d{2}\.?\d{3}\.?\d{3}\s?/\s?\d{4}\s?-?\s?\d{2}")]
    private static partial Regex RegexCnpj();

    [GeneratedRegex(@"(?<!\d)\d{2}[/.-]\d{2}[/.-](?:\d{4}|\d{2})(?!\d)")]
    private static partial Regex RegexData();

    [GeneratedRegex(@"(?<![\d,.])\d{1,3}(?:\.\d{3})*,\d{2}(?![\d])|(?<![\d,.])\d+,\d{2}(?!\d)")]
    private static partial Regex RegexValor();
}
