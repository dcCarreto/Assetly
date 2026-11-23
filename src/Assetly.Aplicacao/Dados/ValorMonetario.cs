using System.Globalization;

namespace Assetly.Aplicacao.Dados;

/// <summary>Interpreta valores monetários digitados pelo usuário, aceitando tanto o formato
/// brasileiro ("1.234,56", "R$ 89,90") quanto o internacional ("1,234.56", "89.90").</summary>
public static class ValorMonetario
{
    public static decimal? InterpretarOpcional(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var limpo = new string(texto.Where(c => char.IsDigit(c) || c is ',' or '.' or '-').ToArray());
        if (limpo.Length == 0 || limpo.Count(c => c == '-') > 1 || (limpo.Contains('-') && limpo[0] != '-'))
        {
            throw Invalido();
        }

        var ultimaVirgula = limpo.LastIndexOf(',');
        var ultimoPonto = limpo.LastIndexOf('.');
        var indiceDecimal = Math.Max(ultimaVirgula, ultimoPonto);

        string normalizado;
        if (indiceDecimal < 0)
        {
            normalizado = limpo;
        }
        else
        {
            var separador = limpo[indiceDecimal];
            var ocorrencias = limpo.Count(c => c == separador);
            var outroSeparadorPresente = limpo.Contains(separador == ',' ? '.' : ',');
            var digitosDepois = limpo.Length - indiceDecimal - 1;

            // "1.234" ou "1.234.567" (um único tipo de separador, grupos de 3) é milhar, não decimal.
            var ehMilhar = !outroSeparadorPresente && digitosDepois == 3 && (ocorrencias > 1 || separador == '.');
            if (ehMilhar || (ocorrencias > 1 && !outroSeparadorPresente))
            {
                normalizado = limpo.Replace(separador.ToString(), string.Empty);
            }
            else
            {
                var parteInteira = limpo[..indiceDecimal].Replace(",", string.Empty).Replace(".", string.Empty);
                normalizado = $"{parteInteira}.{limpo[(indiceDecimal + 1)..]}";
            }
        }

        if (!decimal.TryParse(normalizado, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var valor))
        {
            throw Invalido();
        }

        return valor;
    }

    private static ArgumentException Invalido() =>
        new("valor monetário inválido. Exemplos aceitos: 1234,56 · 1.234,56 · R$ 89,90.");
}
