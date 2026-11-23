using Assetly.Dominio.Bem;

namespace Assetly.Aplicacao.Dados;

public sealed record EditarBemComando(
    BemId Id,
    string Nome,
    TipoBem Tipo,
    DateOnly? CompradoEm,
    decimal? PrecoCompra,
    string? CodigoMoeda,
    string? Observacoes,
    BemId? Responsavel = null);
