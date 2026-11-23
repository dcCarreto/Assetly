using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Despesa;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class PagarDespesa(IRepositorioDespesa repositorioDespesa)
{
    private readonly IRepositorioDespesa _repositorioDespesa = repositorioDespesa ?? throw new ArgumentNullException(nameof(repositorioDespesa));

    public Despesa Executar(DespesaId id, DateOnly pagaEm)
    {
        var despesa = ApoioCasosUso.ExigirDespesa(_repositorioDespesa, id);
        despesa.Pagar(pagaEm);
        _repositorioDespesa.Salvar(despesa);
        return despesa;
    }
}
