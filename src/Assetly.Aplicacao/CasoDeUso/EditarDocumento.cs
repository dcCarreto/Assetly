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

    /// <summary>Renomear, mudar o tipo ou a data não exige o arquivo: só quando o caminho informado é
    /// diferente do atual o novo arquivo é copiado e o documento volta a ficar disponível. Assim é
    /// possível corrigir o nome de um documento ausente sem ter o arquivo em mãos.</summary>
    public Documento Executar(EditarDocumentoComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        var documento = ApoioCasosUso.ExigirDocumento(_repositorioDocumento, comando.Id);

        documento.Renomear(new NomeDocumento(comando.Nome));
        documento.AlterarTipo(comando.Tipo);

        CaminhoDocumentoLocal? arquivoSubstituido = null;
        if (MesmoArquivo(comando.CaminhoLocal, documento.CaminhoLocal))
        {
            documento.AlterarDataRegistro(comando.RegistradoEm);
        }
        else
        {
            arquivoSubstituido = documento.CaminhoLocal;
            var caminhoArmazenado = _armazenamentoDocumento.Armazenar(comando.CaminhoLocal);
            documento.SubstituirArquivo(new CaminhoDocumentoLocal(caminhoArmazenado), comando.RegistradoEm);
        }

        documento.VincularNotaEletronica(string.IsNullOrWhiteSpace(comando.ChaveAcesso) ? null : ChaveAcessoNfe.Criar(comando.ChaveAcesso), comando.UrlConsulta);
        _repositorioDocumento.Salvar(documento);

        // A cópia privada do arquivo anterior ficaria órfã (e entraria em todo backup e sincronização).
        if (arquivoSubstituido is not null)
        {
            ApoioCasosUso.RemoverArquivoSemReferencia(_repositorioDocumento, _armazenamentoDocumento, arquivoSubstituido);
        }

        return documento;
    }

    private static bool MesmoArquivo(string? caminhoInformado, CaminhoDocumentoLocal caminhoAtual) =>
        string.IsNullOrWhiteSpace(caminhoInformado)
        || string.Equals(new CaminhoDocumentoLocal(caminhoInformado.Trim()).Valor, caminhoAtual.Valor, StringComparison.OrdinalIgnoreCase);
}
