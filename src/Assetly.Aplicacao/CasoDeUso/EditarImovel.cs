using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Imovel;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class EditarImovel(IRepositorioImovel repositorioImovel)
{
    private readonly IRepositorioImovel _repositorioImovel = repositorioImovel ?? throw new ArgumentNullException(nameof(repositorioImovel));

    public Imovel Executar(EditarImovelComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        var imovel = ApoioCasosUso.ExigirImovel(_repositorioImovel, comando.BemId);
        imovel.Atualizar(comando.Dados);
        _repositorioImovel.Salvar(imovel);
        return imovel;
    }
}
