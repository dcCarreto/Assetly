using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;

namespace Assetly.Aplicacao.Repositorio;

public interface IRepositorioAlerta
{
    Alerta? BuscarPorId(AlertaId id);

    IReadOnlyList<Alerta> Listar();

    IReadOnlyList<Alerta> ListarPorBem(BemId bemId);

    void Salvar(Alerta alerta);

    /// <summary>Salva vários alertas de uma vez — no banco, numa única transação (tudo ou nada).</summary>
    void SalvarTodos(IEnumerable<Alerta> alertas)
    {
        foreach (var alerta in alertas)
        {
            Salvar(alerta);
        }
    }

    void Excluir(AlertaId id);
}
