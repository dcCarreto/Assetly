using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Backup;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ObterConfiguracaoBackup(IRepositorioConfiguracaoBackup repositorioConfiguracaoBackup)
{
    private readonly IRepositorioConfiguracaoBackup _repositorioConfiguracaoBackup = repositorioConfiguracaoBackup ?? throw new ArgumentNullException(nameof(repositorioConfiguracaoBackup), "repositório de configuração de backup é obrigatório");

    public ConfiguracaoBackup Executar() => _repositorioConfiguracaoBackup.Obter();
}
