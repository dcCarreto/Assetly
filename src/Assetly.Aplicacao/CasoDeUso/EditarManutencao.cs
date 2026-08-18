using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Manutencao;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class EditarManutencao(IRepositorioManutencao repositorioManutencao)
{
    private readonly IRepositorioManutencao _repositorioManutencao = repositorioManutencao ?? throw new ArgumentNullException(nameof(repositorioManutencao), "repositório de manutenções é obrigatório");

    public Manutencao Executar(EditarManutencaoComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        var manutencao = ApoioCasosUso.ExigirManutencao(_repositorioManutencao, comando.Id);

        manutencao.AlterarTipo(comando.Tipo);
        manutencao.AtualizarDescricao(comando.Descricao);
        manutencao.Reagendar(comando.AgendadaPara);

        _repositorioManutencao.Salvar(manutencao);
        return manutencao;
    }
}
