using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Saude;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class EditarSaude(IRepositorioSaude repositorioSaude)
{
    private readonly IRepositorioSaude _repositorioSaude = repositorioSaude ?? throw new ArgumentNullException(nameof(repositorioSaude), "repositório de saúde é obrigatório");

    public Saude Executar(EditarSaudeComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        var saude = ApoioCasosUso.ExigirSaude(_repositorioSaude, comando.Id);

        saude.AlterarTipo(comando.Tipo);
        saude.AlterarFornecedor(comando.Fornecedor);
        saude.AlterarPeriodo(comando.IniciaEm, comando.TerminaEm);
        saude.AtualizarContato(comando.Contato);

        _repositorioSaude.Salvar(saude);
        return saude;
    }
}
