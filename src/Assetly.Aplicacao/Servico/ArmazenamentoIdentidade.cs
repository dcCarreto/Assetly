namespace Assetly.Aplicacao.Servico;

internal sealed class ArmazenamentoIdentidade : IArmazenamentoDocumento
{
    public string Armazenar(string arquivoOrigem) => arquivoOrigem;
}
