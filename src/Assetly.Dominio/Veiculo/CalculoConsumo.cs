namespace Assetly.Dominio.Veiculo;

/// <summary>
/// Consumo pelo método "tanque cheio a tanque cheio": a distância entre dois abastecimentos de tanque
/// cheio dividida por tudo o que foi abastecido depois do primeiro até o segundo (inclusive). Abastecimentos
/// parciais entram na soma de litros, mas só um tanque cheio fecha um trecho.
/// </summary>
public static class CalculoConsumo
{
    public sealed record Trecho(AbastecimentoId Fechamento, int Distancia, decimal Litros)
    {
        public decimal KmPorLitro => Math.Round(Distancia / Litros, 2, MidpointRounding.AwayFromZero);
    }

    public static IReadOnlyList<Trecho> Trechos(IEnumerable<Abastecimento> abastecimentos)
    {
        var trechos = new List<Trecho>();
        int? kmUltimoCheio = null;
        var litrosAcumulados = 0m;

        foreach (var abastecimento in abastecimentos.OrderBy(a => a.Quilometragem).ThenBy(a => a.Data))
        {
            if (kmUltimoCheio is not null)
            {
                litrosAcumulados += abastecimento.Litros;
            }

            if (!abastecimento.TanqueCheio)
            {
                continue;
            }

            if (kmUltimoCheio is not null && abastecimento.Quilometragem > kmUltimoCheio && litrosAcumulados > 0)
            {
                trechos.Add(new Trecho(abastecimento.Id, abastecimento.Quilometragem - kmUltimoCheio.Value, litrosAcumulados));
            }

            kmUltimoCheio = abastecimento.Quilometragem;
            litrosAcumulados = 0;
        }

        return trechos;
    }

    /// <summary>Média ponderada de todos os trechos (distância total / litros totais), ou nulo sem trechos.</summary>
    public static decimal? Medio(IEnumerable<Abastecimento> abastecimentos)
    {
        var trechos = Trechos(abastecimentos);
        var litros = trechos.Sum(t => t.Litros);
        return litros == 0 ? null : Math.Round(trechos.Sum(t => t.Distancia) / litros, 2, MidpointRounding.AwayFromZero);
    }
}
