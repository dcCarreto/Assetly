using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Alerta;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class TomarCienciaAlerta(IRepositorioAlerta repositorioAlerta)
{
    private readonly IRepositorioAlerta _repositorioAlerta = repositorioAlerta ?? throw new ArgumentNullException(nameof(repositorioAlerta), "repositório de alertas é obrigatório");

    public Alerta Executar(AlertaId id)
    {
        var alerta = ApoioCasosUso.ExigirAlerta(_repositorioAlerta, id);
        alerta.TomarCiencia();
        _repositorioAlerta.Salvar(alerta);
        return alerta;
    }
}
