using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Manutencao;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ExcluirManutencao(IRepositorioManutencao repositorioManutencao)
{
    private readonly IRepositorioManutencao _repositorioManutencao = repositorioManutencao ?? throw new ArgumentNullException(nameof(repositorioManutencao), "repositório de manutenções é obrigatório");

    public void Executar(ManutencaoId id)
    {
        ApoioCasosUso.ExigirManutencao(_repositorioManutencao, id);
        _repositorioManutencao.Excluir(id);
    }
}
