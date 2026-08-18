using Assetly.Dominio.Documento;

namespace Assetly.Aplicacao.Dados;

public sealed record EditarDocumentoComando(
    DocumentoId Id,
    TipoDocumento Tipo,
    string? Nome,
    string CaminhoLocal,
    DateOnly RegistradoEm);
