using Assetly.Dominio.Bem;
using Assetly.Dominio.Veiculo;

namespace Assetly.Aplicacao.Repositorio;

public interface IRepositorioVeiculo
{
    Veiculo? BuscarPorBem(BemId bemId);

    IReadOnlyList<Veiculo> Listar();

    void Salvar(Veiculo veiculo);

    void Excluir(BemId bemId);
}
