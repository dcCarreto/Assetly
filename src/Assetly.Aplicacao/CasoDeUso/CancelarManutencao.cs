using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Manutencao;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class CancelarManutencao(IRepositorioManutencao repositorioManutencao)
{
    private readonly IRepositorioManutencao _repositorioManutencao = repositorioManutencao ?? throw new ArgumentNullException(nameof(repositorioManutencao), "repositório de manutenções é obrigatório");

    public Manutencao Executar(ManutencaoId id)
    {
        var manutencao = ApoioCasosUso.ExigirManutencao(_repositorioManutencao, id);
        manutencao.Cancelar();
        _repositorioManutencao.Salvar(manutencao);
        return manutencao;
    }
}
