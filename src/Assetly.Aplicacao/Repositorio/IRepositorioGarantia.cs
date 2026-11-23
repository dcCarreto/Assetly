using Assetly.Dominio.Bem;
using Assetly.Dominio.Garantia;

namespace Assetly.Aplicacao.Repositorio;

public interface IRepositorioGarantia
{
    Garantia? BuscarPorId(GarantiaId id);

    IReadOnlyList<Garantia> Listar();

    IReadOnlyList<Garantia> ListarPorBem(BemId bemId);

    void Salvar(Garantia garantia);

    void Excluir(GarantiaId id);
}
