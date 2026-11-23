using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Backup;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class DesativarCriptografiaBackup(IRepositorioConfiguracaoBackup repositorio)
{
    private readonly IRepositorioConfiguracaoBackup _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));

    public ConfiguracaoBackup Executar()
    {
        var configuracao = _repositorio.Obter();
        configuracao.DesativarCriptografia();
        _repositorio.Salvar(configuracao);
        return configuracao;
    }
}
