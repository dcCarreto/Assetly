using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;
using Assetly.Dominio.Documento;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class EditarDocumento
{
    private readonly IRepositorioDocumento _repositorioDocumento;
    private readonly IArmazenamentoDocumento _armazenamentoDocumento;

    public EditarDocumento(IRepositorioDocumento repositorioDocumento)
        : this(repositorioDocumento, new ArmazenamentoIdentidade())
    {
    }

    public EditarDocumento(IRepositorioDocumento repositorioDocumento, IArmazenamentoDocumento armazenamentoDocumento)
    {
        _repositorioDocumento = repositorioDocumento ?? throw new ArgumentNullException(nameof(repositorioDocumento), "repositório de documentos é obrigatório");
        _armazenamentoDocumento = armazenamentoDocumento ?? throw new ArgumentNullException(nameof(armazenamentoDocumento), "armazenamento de documentos é obrigatório");
    }

    public Documento Executar(EditarDocumentoComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        var documento = ApoioCasosUso.ExigirDocumento(_repositorioDocumento, comando.Id);
        var caminhoArmazenado = _armazenamentoDocumento.Armazenar(comando.CaminhoLocal);

        documento.Renomear(new NomeDocumento(comando.Nome));
        documento.AlterarTipo(comando.Tipo);
        documento.SubstituirArquivo(new CaminhoDocumentoLocal(caminhoArmazenado), comando.RegistradoEm);

        _repositorioDocumento.Salvar(documento);
        return documento;
    }
}
