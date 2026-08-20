using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Saude;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ListarSaudeDoBem(IRepositorioBem repositorioBem, IRepositorioSaude repositorioSaude)
{
    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem), "repositório de bens é obrigatório");
    private readonly IRepositorioSaude _repositorioSaude = repositorioSaude ?? throw new ArgumentNullException(nameof(repositorioSaude), "repositório de saúde é obrigatório");

    public IReadOnlyList<Saude> Executar(BemId bemId)
    {
        ApoioCasosUso.ExigirBem(_repositorioBem, bemId);
        return _repositorioSaude.ListarPorBem(bemId);
    }
}
