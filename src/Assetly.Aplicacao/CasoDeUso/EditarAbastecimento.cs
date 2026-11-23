using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Veiculo;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class EditarAbastecimento(IRepositorioVeiculo repositorioVeiculo, IRepositorioAbastecimento repositorioAbastecimento)
{
    private readonly IRepositorioVeiculo _repositorioVeiculo = repositorioVeiculo ?? throw new ArgumentNullException(nameof(repositorioVeiculo));
    private readonly IRepositorioAbastecimento _repositorioAbastecimento = repositorioAbastecimento ?? throw new ArgumentNullException(nameof(repositorioAbastecimento));

    public Abastecimento Executar(EditarAbastecimentoComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        var abastecimento = ApoioCasosUso.ExigirAbastecimento(_repositorioAbastecimento, comando.Id);
        abastecimento.Atualizar(comando.Data, comando.Quilometragem, comando.Litros, Dinheiro.De(comando.Valor, comando.CodigoMoeda),
            comando.Combustivel, comando.TanqueCheio, comando.Posto);
        _repositorioAbastecimento.Salvar(abastecimento);

        var veiculo = _repositorioVeiculo.BuscarPorBem(abastecimento.BemId);
        if (veiculo is not null && veiculo.AvancarQuilometragem(comando.Quilometragem))
        {
            _repositorioVeiculo.Salvar(veiculo);
        }

        return abastecimento;
    }
}
