using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Backup;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ConfigurarSincronizacao(IRepositorioConfiguracaoSincronizacao repositorio)
{
    private readonly IRepositorioConfiguracaoSincronizacao _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));

    public ConfiguracaoSincronizacao Obter() => _repositorio.Obter();

    public ConfiguracaoSincronizacao Ativar(string pasta)
    {
        var configuracao = _repositorio.Obter();
        configuracao.Ativar(pasta);
        _repositorio.Salvar(configuracao);
        return configuracao;
    }

    public ConfiguracaoSincronizacao Desativar()
    {
        var configuracao = _repositorio.Obter();
        configuracao.Desativar();
        _repositorio.Salvar(configuracao);
        return configuracao;
    }
}
