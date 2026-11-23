namespace Assetly.Dominio.Despesa;

public static class TiposDespesa
{
    public static IReadOnlyList<TipoDespesa> ParaVeiculo { get; } =
    [
        TipoDespesa.Ipva, TipoDespesa.Licenciamento, TipoDespesa.SeguroVeiculo, TipoDespesa.Multa, TipoDespesa.Pedagio,
        TipoDespesa.Estacionamento, TipoDespesa.Lavagem, TipoDespesa.Financiamento, TipoDespesa.Outra
    ];

    public static IReadOnlyList<TipoDespesa> ParaImovel { get; } =
    [
        TipoDespesa.Iptu, TipoDespesa.Condominio, TipoDespesa.Agua, TipoDespesa.Energia, TipoDespesa.Gas, TipoDespesa.Internet,
        TipoDespesa.SeguroImovel, TipoDespesa.Aluguel, TipoDespesa.Reforma, TipoDespesa.Financiamento, TipoDespesa.Outra
    ];
}
