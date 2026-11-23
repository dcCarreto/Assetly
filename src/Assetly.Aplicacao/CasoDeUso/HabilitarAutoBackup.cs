using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Backup;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class HabilitarAutoBackup(IRepositorioConfiguracaoBackup repositorioConfiguracaoBackup)
{
    private readonly IRepositorioConfiguracaoBackup _repositorioConfiguracaoBackup = repositorioConfiguracaoBackup ?? throw new ArgumentNullException(nameof(repositorioConfiguracaoBackup), "repositório de configuração de backup é obrigatório");

    public ConfiguracaoBackup Executar(HabilitarAutoBackupComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        var configuracao = _repositorioConfiguracaoBackup.Obter();
        configuracao.HabilitarAutoBackup(comando.Frequencia);
        _repositorioConfiguracaoBackup.Salvar(configuracao);
        return configuracao;
    }
}
