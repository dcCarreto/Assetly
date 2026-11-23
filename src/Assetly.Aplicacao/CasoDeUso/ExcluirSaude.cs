using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Saude;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ExcluirSaude(IRepositorioSaude repositorioSaude)
{
    private readonly IRepositorioSaude _repositorioSaude = repositorioSaude ?? throw new ArgumentNullException(nameof(repositorioSaude), "repositório de saúde é obrigatório");

    public void Executar(SaudeId id)
    {
        ApoioCasosUso.ExigirSaude(_repositorioSaude, id);
        _repositorioSaude.Excluir(id);
    }
}
