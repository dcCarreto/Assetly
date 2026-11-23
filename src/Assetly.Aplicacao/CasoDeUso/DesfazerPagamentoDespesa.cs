using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Despesa;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class DesfazerPagamentoDespesa(IRepositorioDespesa repositorioDespesa)
{
    private readonly IRepositorioDespesa _repositorioDespesa = repositorioDespesa ?? throw new ArgumentNullException(nameof(repositorioDespesa));

    public Despesa Executar(DespesaId id)
    {
        var despesa = ApoioCasosUso.ExigirDespesa(_repositorioDespesa, id);
        despesa.DesfazerPagamento();
        _repositorioDespesa.Salvar(despesa);
        return despesa;
    }
}
