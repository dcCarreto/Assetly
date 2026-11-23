using Assetly.Dominio.Bem;
using Assetly.Dominio.Saude;

namespace Assetly.Aplicacao.Repositorio;

public interface IRepositorioSaude
{
    Saude? BuscarPorId(SaudeId id);

    IReadOnlyList<Saude> Listar();

    IReadOnlyList<Saude> ListarPorBem(BemId bemId);

    void Salvar(Saude saude);

    void Excluir(SaudeId id);
}
