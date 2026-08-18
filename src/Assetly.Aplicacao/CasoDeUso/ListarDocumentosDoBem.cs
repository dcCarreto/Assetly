using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ListarDocumentosDoBem(IRepositorioBem repositorioBem, IRepositorioDocumento repositorioDocumento)
{
    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem), "repositório de bens é obrigatório");
    private readonly IRepositorioDocumento _repositorioDocumento = repositorioDocumento ?? throw new ArgumentNullException(nameof(repositorioDocumento), "repositório de documentos é obrigatório");

    public IReadOnlyList<Documento> Executar(BemId bemId)
    {
        ApoioCasosUso.ExigirBem(_repositorioBem, bemId);
        return _repositorioDocumento.ListarPorBem(bemId);
    }
}
