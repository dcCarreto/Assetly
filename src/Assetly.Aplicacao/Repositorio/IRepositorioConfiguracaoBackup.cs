using Assetly.Dominio.Backup;

namespace Assetly.Aplicacao.Repositorio;

public interface IRepositorioConfiguracaoBackup
{
    ConfiguracaoBackup Obter();

    void Salvar(ConfiguracaoBackup configuracao);
}
