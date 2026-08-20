using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Saude;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class EditarGastoSaude(IRepositorioGastoSaude repositorioGastoSaude)
{
    private readonly IRepositorioGastoSaude _repositorioGastoSaude = repositorioGastoSaude ?? throw new ArgumentNullException(nameof(repositorioGastoSaude), "repositório de gastos de saúde é obrigatório");

    public GastoSaude Executar(EditarGastoSaudeComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        var gasto = ApoioCasosUso.ExigirGastoSaude(_repositorioGastoSaude, comando.Id);

        gasto.AlterarTipo(comando.Tipo);
        gasto.AtualizarDescricao(comando.Descricao);
        gasto.AlterarData(comando.Data);
        gasto.AlterarValor(Dinheiro.De(comando.Valor, comando.CodigoMoeda));

        _repositorioGastoSaude.Salvar(gasto);
        return gasto;
    }
}
