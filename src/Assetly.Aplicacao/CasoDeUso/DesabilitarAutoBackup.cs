using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Backup;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class DesabilitarAutoBackup(IRepositorioConfiguracaoBackup repositorioConfiguracaoBackup)
{
    private readonly IRepositorioConfiguracaoBackup _repositorioConfiguracaoBackup = repositorioConfiguracaoBackup ?? throw new ArgumentNullException(nameof(repositorioConfiguracaoBackup), "repositório de configuração de backup é obrigatório");

    public ConfiguracaoBackup Executar()
    {
        var configuracao = _repositorioConfiguracaoBackup.Obter();
        configuracao.DesabilitarAutoBackup();
        _repositorioConfiguracaoBackup.Salvar(configuracao);
        return configuracao;
    }
}
