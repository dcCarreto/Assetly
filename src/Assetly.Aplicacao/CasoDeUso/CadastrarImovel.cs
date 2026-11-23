using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Imovel;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class CadastrarImovel(IRepositorioBem repositorioBem, IRepositorioImovel repositorioImovel)
{
    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem));
    private readonly IRepositorioImovel _repositorioImovel = repositorioImovel ?? throw new ArgumentNullException(nameof(repositorioImovel));

    public Imovel Executar(CadastrarImovelComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ArgumentNullException.ThrowIfNull(comando.Dados);
        if (comando.BemExistente is not null && _repositorioImovel.BuscarPorBem(comando.BemExistente.Value) is not null)
        {
            throw new InvalidOperationException("este bem já tem uma ficha de imóvel");
        }

        var nomePadrao = string.IsNullOrWhiteSpace(comando.Dados.Logradouro)
            ? "Imóvel"
            : string.Join(", ", new[] { comando.Dados.Logradouro, comando.Dados.Numero }.Where(p => !string.IsNullOrWhiteSpace(p)));
        var bem = CriacaoBemComFicha.ObterOuCriar(_repositorioBem, comando.BemExistente, TipoBem.Imovel, comando.Nome, nomePadrao,
            comando.CompradoEm, comando.PrecoCompra, comando.CodigoMoeda);

        var imovel = Imovel.Criar(bem.Id, comando.Dados);
        if (comando.BemExistente is null)
        {
            _repositorioBem.Salvar(bem);
        }

        _repositorioImovel.Salvar(imovel);
        return imovel;
    }
}
