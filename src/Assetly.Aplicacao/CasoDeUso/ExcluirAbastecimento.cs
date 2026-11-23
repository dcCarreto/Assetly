using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Veiculo;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ExcluirAbastecimento(IRepositorioAbastecimento repositorioAbastecimento)
{
    private readonly IRepositorioAbastecimento _repositorioAbastecimento = repositorioAbastecimento ?? throw new ArgumentNullException(nameof(repositorioAbastecimento));

    public void Executar(AbastecimentoId id)
    {
        ApoioCasosUso.ExigirAbastecimento(_repositorioAbastecimento, id);
        _repositorioAbastecimento.Excluir(id);
    }
}
