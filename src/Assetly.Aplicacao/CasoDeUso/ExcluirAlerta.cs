using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Alerta;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ExcluirAlerta(IRepositorioAlerta repositorioAlerta)
{
    private readonly IRepositorioAlerta _repositorioAlerta = repositorioAlerta ?? throw new ArgumentNullException(nameof(repositorioAlerta), "repositório de alertas é obrigatório");

    public void Executar(AlertaId id)
    {
        ApoioCasosUso.ExigirAlerta(_repositorioAlerta, id);
        _repositorioAlerta.Excluir(id);
    }
}
