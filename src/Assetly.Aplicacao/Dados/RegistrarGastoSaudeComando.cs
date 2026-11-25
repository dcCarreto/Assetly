using Assetly.Dominio.Bem;
using Assetly.Dominio.Saude;

namespace Assetly.Aplicacao.Dados;

public sealed record RegistrarGastoSaudeComando(
    BemId BemId,
    TipoGastoSaude Tipo,
    string? Descricao,
    DateOnly Data,
    decimal Valor,
    string CodigoMoeda,
    string? Prestador = null,
    string? DocumentoPrestador = null);
