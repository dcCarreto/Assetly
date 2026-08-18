using Assetly.Dominio.Garantia;

namespace Assetly.Aplicacao.Dados;

public sealed record EditarGarantiaComando(
    GarantiaId Id,
    TipoGarantia Tipo,
    string? Fornecedor,
    DateOnly IniciaEm,
    DateOnly TerminaEm,
    string? ContatoSuporte);
