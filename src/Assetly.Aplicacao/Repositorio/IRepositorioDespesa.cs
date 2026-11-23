using Assetly.Dominio.Bem;
using Assetly.Dominio.Despesa;

namespace Assetly.Aplicacao.Repositorio;

public interface IRepositorioDespesa
{
    Despesa? BuscarPorId(DespesaId id);

    IReadOnlyList<Despesa> Listar();

    IReadOnlyList<Despesa> ListarPorBem(BemId bemId);

    void Salvar(Despesa despesa);

    void Excluir(DespesaId id);
}
