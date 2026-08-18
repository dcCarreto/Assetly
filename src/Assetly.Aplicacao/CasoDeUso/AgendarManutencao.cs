using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Manutencao;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class AgendarManutencao(IRepositorioBem repositorioBem, IRepositorioManutencao repositorioManutencao)
{
    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem), "repositório de bens é obrigatório");
    private readonly IRepositorioManutencao _repositorioManutencao = repositorioManutencao ?? throw new ArgumentNullException(nameof(repositorioManutencao), "repositório de manutenções é obrigatório");

    public Manutencao Executar(AgendarManutencaoComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ApoioCasosUso.ExigirBem(_repositorioBem, comando.BemId);

        var manutencao = Manutencao.Agendar(comando.BemId, comando.Tipo, comando.Descricao, comando.AgendadaPara);
        _repositorioManutencao.Salvar(manutencao);
        return manutencao;
    }
}
