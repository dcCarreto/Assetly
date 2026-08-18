using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Alerta;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class CriarAlerta(IRepositorioBem repositorioBem, IRepositorioAlerta repositorioAlerta)
{
    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem), "repositório de bens é obrigatório");
    private readonly IRepositorioAlerta _repositorioAlerta = repositorioAlerta ?? throw new ArgumentNullException(nameof(repositorioAlerta), "repositório de alertas é obrigatório");

    public Alerta Executar(CriarAlertaComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ApoioCasosUso.ExigirBem(_repositorioBem, comando.BemId);

        var alerta = Alerta.Criar(comando.BemId, comando.Tipo, comando.Severidade, comando.Mensagem, comando.CriadoEm, comando.PrazoEm);
        _repositorioAlerta.Salvar(alerta);
        return alerta;
    }
}
