using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Garantia;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class EditarGarantia(IRepositorioGarantia repositorioGarantia)
{
    private readonly IRepositorioGarantia _repositorioGarantia = repositorioGarantia ?? throw new ArgumentNullException(nameof(repositorioGarantia), "repositório de garantias é obrigatório");

    public Garantia Executar(EditarGarantiaComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        var garantia = ApoioCasosUso.ExigirGarantia(_repositorioGarantia, comando.Id);

        garantia.AlterarTipo(comando.Tipo);
        garantia.AlterarFornecedor(comando.Fornecedor);
        garantia.AlterarPeriodo(comando.IniciaEm, comando.TerminaEm);
        garantia.AtualizarContatoSuporte(comando.ContatoSuporte);

        _repositorioGarantia.Salvar(garantia);
        return garantia;
    }
}
