using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Manutencao;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ListarManutencoes(IRepositorioManutencao repositorioManutencao)
{
    private readonly IRepositorioManutencao _repositorioManutencao = repositorioManutencao ?? throw new ArgumentNullException(nameof(repositorioManutencao), "repositório de manutenções é obrigatório");

    public IReadOnlyList<Manutencao> Executar() => _repositorioManutencao.Listar();
}
