using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Garantia;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ListarGarantias(IRepositorioGarantia repositorioGarantia)
{
    private readonly IRepositorioGarantia _repositorioGarantia = repositorioGarantia ?? throw new ArgumentNullException(nameof(repositorioGarantia), "repositório de garantias é obrigatório");

    public IReadOnlyList<Garantia> Executar() => _repositorioGarantia.Listar();
}
