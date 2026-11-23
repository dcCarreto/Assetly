using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Preferencias;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class DefinirCorDestaque(IRepositorioConfiguracaoPreferencias repositorioConfiguracaoPreferencias)
{
    private readonly IRepositorioConfiguracaoPreferencias _repositorioConfiguracaoPreferencias = repositorioConfiguracaoPreferencias ?? throw new ArgumentNullException(nameof(repositorioConfiguracaoPreferencias), "repositório de preferências é obrigatório");

    public ConfiguracaoPreferencias Executar(DefinirCorDestaqueComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        var configuracao = _repositorioConfiguracaoPreferencias.Obter();
        configuracao.DefinirCorDestaque(comando.CorDestaque);
        _repositorioConfiguracaoPreferencias.Salvar(configuracao);
        return configuracao;
    }
}
