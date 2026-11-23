using System.Globalization;

namespace Assetly.Aplicacao.Atualizacao;

/// <summary>
/// O Assetly não usa número de versão: cada release é identificada pela data em que foi publicada —
/// <c>AAAA.MM.DD</c>, com um sequencial quando sai mais de uma no mesmo dia (<c>AAAA.MM.DD.2</c>). O formato
/// é comparável como versão numérica (o Windows e o instalador também a entendem).
/// </summary>
public sealed record VersaoPorData(DateOnly Data, int Sequencia) : IComparable<VersaoPorData>
{
    public static VersaoPorData? TentarLer(string? texto)
    {
        var limpo = (texto ?? string.Empty).Trim().TrimStart('v', 'V').Split('+', '-')[0];
        var partes = limpo.Split('.');
        if (partes.Length is < 3 or > 4 || !partes.All(p => p.Length > 0 && p.All(char.IsDigit)))
        {
            return null;
        }

        var numeros = partes.Select(p => int.Parse(p, CultureInfo.InvariantCulture)).ToArray();
        if (numeros[0] < 2000 || numeros[1] is < 1 or > 12 || numeros[2] < 1 || numeros[2] > DateTime.DaysInMonth(numeros[0], numeros[1]))
        {
            return null;
        }

        var sequencia = partes.Length == 4 ? numeros[3] : 1;
        return sequencia < 1 ? null : new VersaoPorData(new DateOnly(numeros[0], numeros[1], numeros[2]), sequencia);
    }

    /// <summary>Identificador usado na tag e no instalador: "2026.11.23" ou "2026.11.23.2".</summary>
    public string Identificador => Sequencia == 1
        ? Data.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture)
        : $"{Data.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture)}.{Sequencia}";

    /// <summary>Para o usuário: "23/11/2026" ou "23/11/2026 (2ª do dia)".</summary>
    public string Exibicao => Sequencia == 1
        ? Data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
        : $"{Data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)} ({Sequencia}ª do dia)";

    /// <summary>Próxima versão para publicar hoje, dadas as que já existem.</summary>
    public static VersaoPorData Proxima(DateOnly hoje, IEnumerable<string> existentes)
    {
        var doDia = existentes.Select(TentarLer).Where(v => v is not null && v.Data == hoje).Select(v => v!.Sequencia).DefaultIfEmpty(0).Max();
        return new VersaoPorData(hoje, doDia + 1);
    }

    public int CompareTo(VersaoPorData? outra) =>
        outra is null ? 1 : Data != outra.Data ? Data.CompareTo(outra.Data) : Sequencia.CompareTo(outra.Sequencia);

    public static bool operator >(VersaoPorData a, VersaoPorData b) => a.CompareTo(b) > 0;

    public static bool operator <(VersaoPorData a, VersaoPorData b) => a.CompareTo(b) < 0;

    public override string ToString() => Identificador;
}
