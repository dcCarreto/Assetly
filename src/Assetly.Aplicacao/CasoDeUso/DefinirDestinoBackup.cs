using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Backup;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class DefinirDestinoBackup(IRepositorioConfiguracaoBackup repositorioConfiguracaoBackup)
{
    private readonly IRepositorioConfiguracaoBackup _repositorioConfiguracaoBackup = repositorioConfiguracaoBackup ?? throw new ArgumentNullException(nameof(repositorioConfiguracaoBackup), "repositório de configuração de backup é obrigatório");

    public ConfiguracaoBackup Executar(DefinirDestinoBackupComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        var configuracao = _repositorioConfiguracaoBackup.Obter();
        configuracao.DefinirDestino(comando.CaminhoDestino);
        _repositorioConfiguracaoBackup.Salvar(configuracao);
        return configuracao;
    }
}
