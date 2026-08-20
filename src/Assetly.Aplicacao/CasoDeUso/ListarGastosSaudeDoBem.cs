using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Saude;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ListarGastosSaudeDoBem(IRepositorioBem repositorioBem, IRepositorioGastoSaude repositorioGastoSaude)
{
    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem), "repositório de bens é obrigatório");
    private readonly IRepositorioGastoSaude _repositorioGastoSaude = repositorioGastoSaude ?? throw new ArgumentNullException(nameof(repositorioGastoSaude), "repositório de gastos de saúde é obrigatório");

    public IReadOnlyList<GastoSaude> Executar(BemId bemId)
    {
        ApoioCasosUso.ExigirBem(_repositorioBem, bemId);
        return _repositorioGastoSaude.ListarPorBem(bemId);
    }
}
