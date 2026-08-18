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
        StatusDocumento status)
    {
        var documento = new Documento(id, bemId, tipo, nome, caminhoLocal, registradoEm)
        {
            Status = status
        };
        return documento;
    }

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

    public void MarcarAusente()
    {
        Status = StatusDocumento.Ausente;
    }

    public void Arquivar()
    {
        Status = StatusDocumento.Arquivado;
    }
}
