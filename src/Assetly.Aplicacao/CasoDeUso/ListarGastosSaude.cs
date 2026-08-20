using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Saude;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ListarGastosSaude(IRepositorioGastoSaude repositorioGastoSaude)
{
    private readonly IRepositorioGastoSaude _repositorioGastoSaude = repositorioGastoSaude ?? throw new ArgumentNullException(nameof(repositorioGastoSaude), "repositório de gastos de saúde é obrigatório");

    public IReadOnlyList<GastoSaude> Executar() => _repositorioGastoSaude.Listar();
}
