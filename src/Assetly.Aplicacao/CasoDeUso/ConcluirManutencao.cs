using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Manutencao;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ConcluirManutencao(IRepositorioManutencao repositorioManutencao)
{
    private readonly IRepositorioManutencao _repositorioManutencao = repositorioManutencao ?? throw new ArgumentNullException(nameof(repositorioManutencao), "repositório de manutenções é obrigatório");

    public Manutencao Executar(ConcluirManutencaoComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        var manutencao = ApoioCasosUso.ExigirManutencao(_repositorioManutencao, comando.Id);
        manutencao.Concluir(comando.ConcluidaEm, ApoioCasosUso.DinheiroOpcional(comando.Custo, comando.CodigoMoeda));
        _repositorioManutencao.Salvar(manutencao);
        return manutencao;
    }
}
