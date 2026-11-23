using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Veiculo;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class AtualizarQuilometragem(IRepositorioVeiculo repositorioVeiculo)
{
    private readonly IRepositorioVeiculo _repositorioVeiculo = repositorioVeiculo ?? throw new ArgumentNullException(nameof(repositorioVeiculo));

    public Veiculo Executar(BemId bemId, int quilometragem)
    {
        var veiculo = ApoioCasosUso.ExigirVeiculo(_repositorioVeiculo, bemId);
        veiculo.RegistrarQuilometragem(quilometragem);
        _repositorioVeiculo.Salvar(veiculo);
        return veiculo;
    }
}
