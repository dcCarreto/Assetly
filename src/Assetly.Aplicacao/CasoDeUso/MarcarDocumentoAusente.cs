using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Documento;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class MarcarDocumentoAusente(IRepositorioDocumento repositorioDocumento)
{
    private readonly IRepositorioDocumento _repositorioDocumento = repositorioDocumento ?? throw new ArgumentNullException(nameof(repositorioDocumento), "repositório de documentos é obrigatório");

    public Documento Executar(DocumentoId id)
    {
        var documento = ApoioCasosUso.ExigirDocumento(_repositorioDocumento, id);
        documento.MarcarAusente();
        _repositorioDocumento.Salvar(documento);
        return documento;
    }
}
