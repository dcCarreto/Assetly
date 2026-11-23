using Assetly.Dominio.Bem;
using Assetly.Dominio.Veiculo;

namespace Assetly.Aplicacao.Repositorio;

public interface IRepositorioAbastecimento
{
    Abastecimento? BuscarPorId(AbastecimentoId id);

    IReadOnlyList<Abastecimento> Listar();

    IReadOnlyList<Abastecimento> ListarPorBem(BemId bemId);

    void Salvar(Abastecimento abastecimento);

    void Excluir(AbastecimentoId id);
}
