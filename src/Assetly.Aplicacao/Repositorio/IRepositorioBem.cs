using Assetly.Dominio.Bem;

namespace Assetly.Aplicacao.Repositorio;

public interface IRepositorioBem
{
    Bem? BuscarPorId(BemId id);

    IReadOnlyList<Bem> Listar();

    void Salvar(Bem bem);

    void Excluir(BemId id);
}
