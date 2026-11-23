using Assetly.Dominio.Bem;
using Assetly.Dominio.Imovel;

namespace Assetly.Aplicacao.Repositorio;

public interface IRepositorioImovel
{
    Imovel? BuscarPorBem(BemId bemId);

    IReadOnlyList<Imovel> Listar();

    void Salvar(Imovel imovel);

    void Excluir(BemId bemId);
}
