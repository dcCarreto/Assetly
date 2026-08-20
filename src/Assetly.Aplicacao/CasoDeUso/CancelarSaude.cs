using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Saude;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class CancelarSaude(IRepositorioSaude repositorioSaude)
{
    private readonly IRepositorioSaude _repositorioSaude = repositorioSaude ?? throw new ArgumentNullException(nameof(repositorioSaude), "repositório de saúde é obrigatório");

    public Saude Executar(SaudeId id, DateOnly dataCancelamento)
    {
        var saude = ApoioCasosUso.ExigirSaude(_repositorioSaude, id);
        saude.Cancelar(dataCancelamento);
        _repositorioSaude.Salvar(saude);
        return saude;
    }
}
