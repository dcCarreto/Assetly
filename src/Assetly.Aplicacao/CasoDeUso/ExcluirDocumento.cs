using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;
using Assetly.Dominio.Documento;

namespace Assetly.Aplicacao.CasoDeUso;

/// <summary>Exclui o registro do documento e a cópia privada do arquivo — a não ser que outro
/// documento aponte para o mesmo arquivo armazenado.</summary>
public sealed class ExcluirDocumento(IRepositorioDocumento repositorioDocumento, IArmazenamentoDocumento armazenamentoDocumento)
{
    private readonly IRepositorioDocumento _repositorioDocumento = repositorioDocumento ?? throw new ArgumentNullException(nameof(repositorioDocumento), "repositório de documentos é obrigatório");
    private readonly IArmazenamentoDocumento _armazenamentoDocumento = armazenamentoDocumento ?? throw new ArgumentNullException(nameof(armazenamentoDocumento), "armazenamento de documentos é obrigatório");

    public void Executar(DocumentoId id)
    {
        var documento = ApoioCasosUso.ExigirDocumento(_repositorioDocumento, id);
        _repositorioDocumento.Excluir(id);
        ApoioCasosUso.RemoverArquivoSemReferencia(_repositorioDocumento, _armazenamentoDocumento, documento.CaminhoLocal);
    }
}
