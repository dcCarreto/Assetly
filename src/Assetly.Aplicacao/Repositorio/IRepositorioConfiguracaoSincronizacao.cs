using Assetly.Dominio.Backup;

namespace Assetly.Aplicacao.Repositorio;

public interface IRepositorioConfiguracaoSincronizacao
{
    ConfiguracaoSincronizacao Obter();

    void Salvar(ConfiguracaoSincronizacao configuracao);
}
