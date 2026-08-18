using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Garantia;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class RegistrarGarantia(IRepositorioBem repositorioBem, IRepositorioGarantia repositorioGarantia)
{
    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem), "repositório de bens é obrigatório");
    private readonly IRepositorioGarantia _repositorioGarantia = repositorioGarantia ?? throw new ArgumentNullException(nameof(repositorioGarantia), "repositório de garantias é obrigatório");

    public Garantia Executar(RegistrarGarantiaComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ApoioCasosUso.ExigirBem(_repositorioBem, comando.BemId);

        var garantia = Garantia.Criar(comando.BemId, comando.Tipo, comando.Fornecedor, comando.IniciaEm, comando.TerminaEm);
        garantia.AtualizarContatoSuporte(comando.ContatoSuporte);
        _repositorioGarantia.Salvar(garantia);
        return garantia;
    }
}
