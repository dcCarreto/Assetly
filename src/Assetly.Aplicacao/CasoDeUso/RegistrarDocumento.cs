using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;
using Assetly.Dominio.Documento;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class RegistrarDocumento
{
    private readonly IRepositorioBem _repositorioBem;
    private readonly IRepositorioDocumento _repositorioDocumento;
    private readonly IArmazenamentoDocumento _armazenamentoDocumento;

    public RegistrarDocumento(IRepositorioBem repositorioBem, IRepositorioDocumento repositorioDocumento)
        : this(repositorioBem, repositorioDocumento, new ArmazenamentoIdentidade())
    {
    }

    public RegistrarDocumento(
        IRepositorioBem repositorioBem,
        IRepositorioDocumento repositorioDocumento,
        IArmazenamentoDocumento armazenamentoDocumento)
    {
        _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem), "repositório de bens é obrigatório");
        _repositorioDocumento = repositorioDocumento ?? throw new ArgumentNullException(nameof(repositorioDocumento), "repositório de documentos é obrigatório");
        _armazenamentoDocumento = armazenamentoDocumento ?? throw new ArgumentNullException(nameof(armazenamentoDocumento), "armazenamento de documentos é obrigatório");
    }

    public Documento Executar(RegistrarDocumentoComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ApoioCasosUso.ExigirBem(_repositorioBem, comando.BemId);
        var caminhoArmazenado = _armazenamentoDocumento.Armazenar(comando.CaminhoLocal);

        var documento = Documento.Registrar(
            comando.BemId,
            comando.Tipo,
            new NomeDocumento(comando.Nome),
            new CaminhoDocumentoLocal(caminhoArmazenado),
            comando.RegistradoEm);

        documento.VincularNotaEletronica(string.IsNullOrWhiteSpace(comando.ChaveAcesso) ? null : ChaveAcessoNfe.Criar(comando.ChaveAcesso), comando.UrlConsulta);
        _repositorioDocumento.Salvar(documento);
        return documento;
    }
}
