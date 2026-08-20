using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Saude;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class RegistrarSaude(IRepositorioBem repositorioBem, IRepositorioSaude repositorioSaude)
{
    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem), "repositório de bens é obrigatório");
    private readonly IRepositorioSaude _repositorioSaude = repositorioSaude ?? throw new ArgumentNullException(nameof(repositorioSaude), "repositório de saúde é obrigatório");

    public Saude Executar(RegistrarSaudeComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ApoioCasosUso.ExigirBem(_repositorioBem, comando.BemId);

        var saude = Saude.Criar(comando.BemId, comando.Tipo, comando.Fornecedor, comando.IniciaEm, comando.TerminaEm, comando.Contato);
        _repositorioSaude.Salvar(saude);
        return saude;
    }
}
