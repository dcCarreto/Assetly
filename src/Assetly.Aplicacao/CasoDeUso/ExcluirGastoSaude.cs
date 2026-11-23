using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Saude;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ExcluirGastoSaude(IRepositorioGastoSaude repositorioGastoSaude)
{
    private readonly IRepositorioGastoSaude _repositorioGastoSaude = repositorioGastoSaude ?? throw new ArgumentNullException(nameof(repositorioGastoSaude), "repositório de gastos de saúde é obrigatório");

    public void Executar(GastoSaudeId id)
    {
        ApoioCasosUso.ExigirGastoSaude(_repositorioGastoSaude, id);
        _repositorioGastoSaude.Excluir(id);
    }
}
