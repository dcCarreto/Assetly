using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Despesa;

namespace Assetly.Aplicacao.CasoDeUso;

/// <summary>Registra o pagamento. Numa conta recorrente, gera a do mês seguinte (uma só vez, mesmo que o pagamento
/// seja desfeito e refeito).</summary>
public sealed class PagarDespesa(IRepositorioDespesa repositorioDespesa)
{
    private readonly IRepositorioDespesa _repositorioDespesa = repositorioDespesa ?? throw new ArgumentNullException(nameof(repositorioDespesa));

    public Despesa Executar(DespesaId id, DateOnly pagaEm)
    {
        var despesa = ApoioCasosUso.ExigirDespesa(_repositorioDespesa, id);
        despesa.Pagar(pagaEm);
        var proxima = despesa.Recorrente && !_repositorioDespesa.ListarPorBem(despesa.BemId).Any(despesa.EhProximaRecorrencia)
            ? despesa.ProximaRecorrencia()
            : null;
        _repositorioDespesa.SalvarTodos(proxima is null ? [despesa] : [despesa, proxima]);
        return despesa;
    }
}
