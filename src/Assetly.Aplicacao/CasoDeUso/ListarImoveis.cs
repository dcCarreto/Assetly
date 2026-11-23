using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Imovel;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ListarImoveis(IRepositorioImovel repositorioImovel)
{
    private readonly IRepositorioImovel _repositorioImovel = repositorioImovel ?? throw new ArgumentNullException(nameof(repositorioImovel));

    public IReadOnlyList<Imovel> Executar() => _repositorioImovel.Listar();
}
