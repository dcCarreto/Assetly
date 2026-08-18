using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Documento;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ListarDocumentos(IRepositorioDocumento repositorioDocumento)
{
    private readonly IRepositorioDocumento _repositorioDocumento = repositorioDocumento ?? throw new ArgumentNullException(nameof(repositorioDocumento), "repositório de documentos é obrigatório");

    public IReadOnlyList<Documento> Executar() => _repositorioDocumento.Listar();
}
