using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;

namespace Assetly.Aplicacao.Repositorio;

public interface IRepositorioAlerta
{
    Alerta? BuscarPorId(AlertaId id);

    IReadOnlyList<Alerta> Listar();

    IReadOnlyList<Alerta> ListarPorBem(BemId bemId);

    void Salvar(Alerta alerta);

    void Excluir(AlertaId id);
}
