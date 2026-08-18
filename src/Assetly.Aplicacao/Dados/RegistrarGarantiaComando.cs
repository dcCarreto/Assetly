using Assetly.Dominio.Bem;
using Assetly.Dominio.Garantia;

namespace Assetly.Aplicacao.Dados;

public sealed record RegistrarGarantiaComando(
    BemId BemId,
    TipoGarantia Tipo,
    string? Fornecedor,
    DateOnly IniciaEm,
    DateOnly TerminaEm,
    string? ContatoSuporte);
