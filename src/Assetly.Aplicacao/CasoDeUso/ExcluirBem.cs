using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;
using Assetly.Dominio.Bem;

namespace Assetly.Aplicacao.CasoDeUso;

/// <summary>Exclui o bem e, em cascata (chaves estrangeiras do banco), todas as garantias,
/// manutenções, documentos, alertas, convênios e gastos ligados a ele. As cópias privadas dos
/// arquivos dos documentos excluídos também são removidas.</summary>
public sealed class ExcluirBem(IRepositorioBem repositorioBem, IRepositorioDocumento repositorioDocumento, IArmazenamentoDocumento armazenamentoDocumento)
{
    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem), "repositório de bens é obrigatório");
    private readonly IRepositorioDocumento _repositorioDocumento = repositorioDocumento ?? throw new ArgumentNullException(nameof(repositorioDocumento), "repositório de documentos é obrigatório");
    private readonly IArmazenamentoDocumento _armazenamentoDocumento = armazenamentoDocumento ?? throw new ArgumentNullException(nameof(armazenamentoDocumento), "armazenamento de documentos é obrigatório");

    public void Executar(BemId id)
    {
        ApoioCasosUso.ExigirBem(_repositorioBem, id);
        var documentos = _repositorioDocumento.ListarPorBem(id);
        foreach (var documento in documentos)
        {
            _repositorioDocumento.Excluir(documento.Id);
        }

        _repositorioBem.Excluir(id);

        foreach (var documento in documentos)
        {
            ApoioCasosUso.RemoverArquivoSemReferencia(_repositorioDocumento, _armazenamentoDocumento, documento.CaminhoLocal);
        }
    }
}
