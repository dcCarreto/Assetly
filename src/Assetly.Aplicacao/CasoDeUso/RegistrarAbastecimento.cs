using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Veiculo;

namespace Assetly.Aplicacao.CasoDeUso;

/// <summary>Registra o abastecimento e, se a quilometragem informada for maior que a da ficha,
/// atualiza a quilometragem do veículo: o hodômetro anotado no posto é a leitura mais frequente.</summary>
public sealed class RegistrarAbastecimento(IRepositorioVeiculo repositorioVeiculo, IRepositorioAbastecimento repositorioAbastecimento)
{
    private readonly IRepositorioVeiculo _repositorioVeiculo = repositorioVeiculo ?? throw new ArgumentNullException(nameof(repositorioVeiculo));
    private readonly IRepositorioAbastecimento _repositorioAbastecimento = repositorioAbastecimento ?? throw new ArgumentNullException(nameof(repositorioAbastecimento));

    public Abastecimento Executar(RegistrarAbastecimentoComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        var veiculo = ApoioCasosUso.ExigirVeiculo(_repositorioVeiculo, comando.BemId);
        var abastecimento = Abastecimento.Registrar(comando.BemId, comando.Data, comando.Quilometragem, comando.Litros,
            Dinheiro.De(comando.Valor, comando.CodigoMoeda), comando.Combustivel, comando.TanqueCheio, comando.Posto);
        _repositorioAbastecimento.Salvar(abastecimento);

        if (veiculo.AvancarQuilometragem(comando.Quilometragem))
        {
            _repositorioVeiculo.Salvar(veiculo);
        }

        return abastecimento;
    }
}
