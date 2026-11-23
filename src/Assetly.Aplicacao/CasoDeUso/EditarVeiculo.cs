using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Veiculo;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class EditarVeiculo(IRepositorioVeiculo repositorioVeiculo)
{
    private readonly IRepositorioVeiculo _repositorioVeiculo = repositorioVeiculo ?? throw new ArgumentNullException(nameof(repositorioVeiculo));

    public Veiculo Executar(EditarVeiculoComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        var veiculo = ApoioCasosUso.ExigirVeiculo(_repositorioVeiculo, comando.BemId);
        veiculo.Atualizar(comando.Dados);
        _repositorioVeiculo.Salvar(veiculo);
        return veiculo;
    }
}
