using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Garantia;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ListarGarantiasDoBem(IRepositorioBem repositorioBem, IRepositorioGarantia repositorioGarantia)
{
    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem), "repositório de bens é obrigatório");
    private readonly IRepositorioGarantia _repositorioGarantia = repositorioGarantia ?? throw new ArgumentNullException(nameof(repositorioGarantia), "repositório de garantias é obrigatório");

    public IReadOnlyList<Garantia> Executar(BemId bemId)
    {
        ApoioCasosUso.ExigirBem(_repositorioBem, bemId);
        return _repositorioGarantia.ListarPorBem(bemId);
    }
}
