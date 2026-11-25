using Assetly.Dominio.Bem;

namespace Assetly.Dominio.Documento;

public sealed class Documento
{
    public DocumentoId Id { get; }
    public BemId BemId { get; }
    public TipoDocumento Tipo { get; private set; }
    public NomeDocumento Nome { get; private set; }
    public CaminhoDocumentoLocal CaminhoLocal { get; private set; }
    public DateOnly RegistradoEm { get; private set; }
    public StatusDocumento Status { get; private set; }

    /// <summary>Chave de acesso (44 dígitos) quando o documento é uma nota fiscal eletrônica.</summary>
    public string ChaveAcesso { get; private set; } = string.Empty;

    /// <summary>Endereço de consulta da nota na Sefaz (lido do QR Code), quando houver.</summary>
    public string UrlConsulta { get; private set; } = string.Empty;

    /// <summary>Manutenção, despesa ou abastecimento de que este documento é o comprovante (nulo = só do bem).</summary>
    public VinculoDocumento? Vinculo { get; private set; }

    private Documento(
        DocumentoId id,
        BemId bemId,
        TipoDocumento tipo,
        NomeDocumento nome,
        CaminhoDocumentoLocal caminhoLocal,
        DateOnly registradoEm)
    {
        Id = id;
        BemId = bemId;
        Tipo = tipo;
        Nome = nome ?? throw new ArgumentNullException(nameof(nome), "nome do documento é obrigatório");
        CaminhoLocal = caminhoLocal ?? throw new ArgumentNullException(nameof(caminhoLocal), "caminho do documento é obrigatório");
        RegistradoEm = registradoEm;
        Status = StatusDocumento.Disponivel;
    }

    public static Documento Registrar(
        BemId bemId,
        TipoDocumento tipo,
        NomeDocumento nome,
        CaminhoDocumentoLocal caminhoLocal,
        DateOnly registradoEm) =>
        new(DocumentoId.Novo(), bemId, tipo, nome, caminhoLocal, registradoEm);

    public static Documento Restaurar(
        DocumentoId id,
        BemId bemId,
        TipoDocumento tipo,
        NomeDocumento nome,
        CaminhoDocumentoLocal caminhoLocal,
        DateOnly registradoEm,
        StatusDocumento status,
        string? chaveAcesso = null,
        string? urlConsulta = null,
        VinculoDocumento? vinculo = null)
    {
        var documento = new Documento(id, bemId, tipo, nome, caminhoLocal, registradoEm)
        {
            Status = status,
            ChaveAcesso = chaveAcesso ?? string.Empty,
            UrlConsulta = urlConsulta ?? string.Empty,
            Vinculo = vinculo
        };
        return documento;
    }

    public void VincularRegistro(VinculoDocumento? vinculo) => Vinculo = vinculo;

    public void Renomear(NomeDocumento nome)
    {
        Nome = nome ?? throw new ArgumentNullException(nameof(nome), "nome do documento é obrigatório");
    }

    public void AlterarTipo(TipoDocumento tipo)
    {
        Tipo = tipo;
    }

    public void SubstituirArquivo(CaminhoDocumentoLocal caminhoLocal, DateOnly registradoEm)
    {
        CaminhoLocal = caminhoLocal ?? throw new ArgumentNullException(nameof(caminhoLocal), "caminho do documento é obrigatório");
        RegistradoEm = registradoEm;
        Status = StatusDocumento.Disponivel;
    }

    /// <summary>Associa (ou remove, com nulo) a chave de acesso e o link de consulta de uma nota eletrônica.</summary>
    public void VincularNotaEletronica(ChaveAcessoNfe? chave, string? urlConsulta)
    {
        ChaveAcesso = chave?.Valor ?? string.Empty;
        var url = (urlConsulta ?? string.Empty).Trim();
        if (url.Length > 0 && !(url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("o link de consulta da nota deve começar com http:// ou https://");
        }

        UrlConsulta = chave is null ? string.Empty : url;
    }

    public void AlterarDataRegistro(DateOnly registradoEm)
    {
        RegistradoEm = registradoEm;
    }

    /// <summary>Atualiza só o caminho do arquivo (mesmo arquivo, outro local — ex.: após restaurar um
    /// backup em outro perfil de usuário), sem mexer em status nem data de registro.</summary>
    public void RelocalizarArquivo(CaminhoDocumentoLocal caminhoLocal)
    {
        CaminhoLocal = caminhoLocal ?? throw new ArgumentNullException(nameof(caminhoLocal), "caminho do documento é obrigatório");
    }

    public void MarcarAusente()
    {
        Status = StatusDocumento.Ausente;
    }

    public void Arquivar()
    {
        Status = StatusDocumento.Arquivado;
    }
}
