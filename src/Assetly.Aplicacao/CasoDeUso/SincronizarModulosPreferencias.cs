using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Preferencias;

namespace Assetly.Aplicacao.CasoDeUso;

/// <summary>Chamado uma vez a cada lançamento do app, antes de qualquer leitura de preferências
/// pela UI — reconcilia a ordem/visibilidade/módulo-inicial salvos com os módulos que existem hoje
/// (mesmo padrão de "verificação a cada atualização" de VerificarEExecutarBackupAutomatico, mas
/// disparado só na inicialização, não a cada tick).</summary>
public sealed class SincronizarModulosPreferencias(IRepositorioConfiguracaoPreferencias repositorioConfiguracaoPreferencias)
{
    private readonly IRepositorioConfiguracaoPreferencias _repositorioConfiguracaoPreferencias = repositorioConfiguracaoPreferencias ?? throw new ArgumentNullException(nameof(repositorioConfiguracaoPreferencias), "repositório de preferências é obrigatório");

    public ConfiguracaoPreferencias Executar(IReadOnlyList<string> titulosAtuais)
    {
        var configuracao = _repositorioConfiguracaoPreferencias.Obter();
        configuracao.SincronizarModulos(titulosAtuais);
        _repositorioConfiguracaoPreferencias.Salvar(configuracao);
        return configuracao;
    }
}
