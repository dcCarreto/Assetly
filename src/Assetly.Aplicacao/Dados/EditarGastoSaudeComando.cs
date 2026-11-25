using Assetly.Dominio.Saude;

namespace Assetly.Aplicacao.Dados;

public sealed record EditarGastoSaudeComando(
    GastoSaudeId Id,
    TipoGastoSaude Tipo,
    string? Descricao,
    DateOnly Data,
    decimal Valor,
    string CodigoMoeda,
    string? Prestador = null,
    string? DocumentoPrestador = null);
