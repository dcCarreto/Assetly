using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Manutencao;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ListarManutencoesDoBem(IRepositorioBem repositorioBem, IRepositorioManutencao repositorioManutencao)
{
    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem), "repositório de bens é obrigatório");
    private readonly IRepositorioManutencao _repositorioManutencao = repositorioManutencao ?? throw new ArgumentNullException(nameof(repositorioManutencao), "repositório de manutenções é obrigatório");

    public IReadOnlyList<Manutencao> Executar(BemId bemId)
    {
        ApoioCasosUso.ExigirBem(_repositorioBem, bemId);
        return _repositorioManutencao.ListarPorBem(bemId);
    }
}
