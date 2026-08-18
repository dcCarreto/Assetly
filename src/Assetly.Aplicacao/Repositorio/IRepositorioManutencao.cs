using Assetly.Dominio.Bem;
using Assetly.Dominio.Manutencao;

namespace Assetly.Aplicacao.Repositorio;

public interface IRepositorioManutencao
{
    Manutencao? BuscarPorId(ManutencaoId id);

    IReadOnlyList<Manutencao> Listar();

    IReadOnlyList<Manutencao> ListarPorBem(BemId bemId);

    void Salvar(Manutencao manutencao);
}
