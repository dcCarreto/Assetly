using Assetly.Dominio.Compartilhado;

namespace Assetly.Dominio.Documento;

public sealed record NomeDocumento
{
    public const int TamanhoMaximo = 160;

    public string Valor { get; }

    public NomeDocumento(string valor)
    {
        Valor = Validacao.ExigirTexto(valor, "nome do documento", TamanhoMaximo);
    }
}
