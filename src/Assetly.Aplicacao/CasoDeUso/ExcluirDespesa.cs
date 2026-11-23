using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Despesa;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ExcluirDespesa(IRepositorioDespesa repositorioDespesa)
{
    private readonly IRepositorioDespesa _repositorioDespesa = repositorioDespesa ?? throw new ArgumentNullException(nameof(repositorioDespesa));

    public void Executar(DespesaId id)
    {
        ApoioCasosUso.ExigirDespesa(_repositorioDespesa, id);
        _repositorioDespesa.Excluir(id);
    }
}
