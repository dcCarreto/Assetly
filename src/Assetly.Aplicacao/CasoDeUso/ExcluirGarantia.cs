using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Garantia;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ExcluirGarantia(IRepositorioGarantia repositorioGarantia)
{
    private readonly IRepositorioGarantia _repositorioGarantia = repositorioGarantia ?? throw new ArgumentNullException(nameof(repositorioGarantia), "repositório de garantias é obrigatório");

    public void Executar(GarantiaId id)
    {
        ApoioCasosUso.ExigirGarantia(_repositorioGarantia, id);
        _repositorioGarantia.Excluir(id);
    }
}
