using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Backup;

namespace Assetly.Aplicacao.CasoDeUso;

/// <summary>Quantos backups manter no destino (0 = todos): sem limite, o backup automático diário enche o
/// pendrive e passa a falhar.</summary>
public sealed class DefinirRetencaoBackup(IRepositorioConfiguracaoBackup repositorioConfiguracaoBackup)
{
    private readonly IRepositorioConfiguracaoBackup _repositorioConfiguracaoBackup = repositorioConfiguracaoBackup ?? throw new ArgumentNullException(nameof(repositorioConfiguracaoBackup), "repositório de configuração de backup é obrigatório");

    public ConfiguracaoBackup Executar(int manterUltimos)
    {
        var configuracao = _repositorioConfiguracaoBackup.Obter();
        configuracao.DefinirRetencao(manterUltimos);
        _repositorioConfiguracaoBackup.Salvar(configuracao);
        return configuracao;
    }
}
