namespace Assetly.Dominio.Saude;

public sealed record PeriodoSaude
{
    public DateOnly IniciaEm { get; }
    public DateOnly TerminaEm { get; }

    public PeriodoSaude(DateOnly iniciaEm, DateOnly terminaEm)
    {
        if (terminaEm < iniciaEm)
        {
            throw new ArgumentException("data final do período de saúde não pode ser anterior à data inicial");
        }

        IniciaEm = iniciaEm;
        TerminaEm = terminaEm;
    }

    public bool Contem(DateOnly data) => data >= IniciaEm && data <= TerminaEm;

    public bool EstaVencidaEm(DateOnly data) => data > TerminaEm;

    public bool VenceEmAte(DateOnly data, int dias)
    {
        if (dias < 0)
        {
            throw new ArgumentException("dias não pode ser negativo");
        }

        return Contem(data) && TerminaEm <= data.AddDays(dias);
    }
}
