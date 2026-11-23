using Assetly.Dominio.Preferencias;

namespace Assetly.Aplicacao.Repositorio;

public interface IRepositorioConfiguracaoPreferencias
{
    ConfiguracaoPreferencias Obter();

    void Salvar(ConfiguracaoPreferencias configuracao);
}
