using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;

namespace Assetly.Aplicacao.Dados;

public sealed record RegistrarDocumentoComando(
    BemId BemId,
    TipoDocumento Tipo,
    string? Nome,
    string CaminhoLocal,
    DateOnly RegistradoEm,
    string? ChaveAcesso = null,
    string? UrlConsulta = null,
    VinculoDocumento? Vinculo = null);
