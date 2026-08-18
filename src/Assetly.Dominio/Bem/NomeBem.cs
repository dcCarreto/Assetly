using Assetly.Dominio.Compartilhado;

namespace Assetly.Dominio.Bem;

public sealed record NomeBem
{
    public const int TamanhoMaximo = 120;

    public string Valor { get; }

    public NomeBem(string? valor)
    {
        Valor = Validacao.ExigirTexto(valor, "nome do bem", TamanhoMaximo);
    }
}
