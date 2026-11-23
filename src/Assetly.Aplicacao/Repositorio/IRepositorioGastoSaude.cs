using Assetly.Dominio.Bem;
using Assetly.Dominio.Saude;

namespace Assetly.Aplicacao.Repositorio;

public interface IRepositorioGastoSaude
{
    GastoSaude? BuscarPorId(GastoSaudeId id);

    IReadOnlyList<GastoSaude> Listar();

    IReadOnlyList<GastoSaude> ListarPorBem(BemId bemId);

    void Salvar(GastoSaude gastoSaude);

    void Excluir(GastoSaudeId id);
}
