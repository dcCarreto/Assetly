using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Alerta;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ListarAlertas(IRepositorioAlerta repositorioAlerta)
{
    private readonly IRepositorioAlerta _repositorioAlerta = repositorioAlerta ?? throw new ArgumentNullException(nameof(repositorioAlerta), "repositório de alertas é obrigatório");

    public IReadOnlyList<Alerta> Executar() => _repositorioAlerta.Listar();
}
