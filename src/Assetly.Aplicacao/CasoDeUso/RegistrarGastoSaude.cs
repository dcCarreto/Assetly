using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Saude;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class RegistrarGastoSaude(IRepositorioBem repositorioBem, IRepositorioGastoSaude repositorioGastoSaude)
{
    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem), "repositório de bens é obrigatório");
    private readonly IRepositorioGastoSaude _repositorioGastoSaude = repositorioGastoSaude ?? throw new ArgumentNullException(nameof(repositorioGastoSaude), "repositório de gastos de saúde é obrigatório");

    public GastoSaude Executar(RegistrarGastoSaudeComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ApoioCasosUso.ExigirBem(_repositorioBem, comando.BemId);

        var gasto = GastoSaude.Registrar(comando.BemId, comando.Tipo, comando.Descricao, comando.Data, Dinheiro.De(comando.Valor, comando.CodigoMoeda));
        gasto.DefinirPrestador(comando.Prestador, comando.DocumentoPrestador);
        _repositorioGastoSaude.Salvar(gasto);
        return gasto;
    }
}
