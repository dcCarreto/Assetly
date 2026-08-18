using Assetly.Dominio.Bem;

namespace Assetly.Aplicacao.Dados;

public sealed record CadastrarBemComando(
    string Nome,
    TipoBem Tipo,
    DateOnly? CompradoEm,
    decimal? PrecoCompra,
    string? CodigoMoeda,
    string? Observacoes);
