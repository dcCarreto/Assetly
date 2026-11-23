using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Veiculo;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ListarVeiculos(IRepositorioVeiculo repositorioVeiculo)
{
    private readonly IRepositorioVeiculo _repositorioVeiculo = repositorioVeiculo ?? throw new ArgumentNullException(nameof(repositorioVeiculo));

    public IReadOnlyList<Veiculo> Executar() => _repositorioVeiculo.Listar();
}
