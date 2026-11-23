using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Despesa;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ListarDespesas(IRepositorioDespesa repositorioDespesa)
{
    private readonly IRepositorioDespesa _repositorioDespesa = repositorioDespesa ?? throw new ArgumentNullException(nameof(repositorioDespesa));

    public IReadOnlyList<Despesa> Executar() => _repositorioDespesa.Listar();
}
