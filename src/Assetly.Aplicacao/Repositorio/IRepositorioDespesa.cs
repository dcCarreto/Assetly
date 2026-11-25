using Assetly.Dominio.Bem;
using Assetly.Dominio.Despesa;

namespace Assetly.Aplicacao.Repositorio;

public interface IRepositorioDespesa
{
    Despesa? BuscarPorId(DespesaId id);

    IReadOnlyList<Despesa> Listar();

    IReadOnlyList<Despesa> ListarPorBem(BemId bemId);

    void Salvar(Despesa despesa);

    /// <summary>Salva várias despesas de uma vez — no banco, numa única transação (tudo ou nada).</summary>
    void SalvarTodos(IEnumerable<Despesa> despesas)
    {
        foreach (var despesa in despesas)
        {
            Salvar(despesa);
        }
    }

    void Excluir(DespesaId id);
}
