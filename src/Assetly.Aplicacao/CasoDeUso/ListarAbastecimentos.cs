using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Veiculo;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ListarAbastecimentos(IRepositorioAbastecimento repositorioAbastecimento)
{
    private readonly IRepositorioAbastecimento _repositorioAbastecimento = repositorioAbastecimento ?? throw new ArgumentNullException(nameof(repositorioAbastecimento));

    public IReadOnlyList<Abastecimento> Executar() => _repositorioAbastecimento.Listar();
}
