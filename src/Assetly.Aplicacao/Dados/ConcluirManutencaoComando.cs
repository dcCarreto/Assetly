using Assetly.Dominio.Manutencao;

namespace Assetly.Aplicacao.Dados;

public sealed record ConcluirManutencaoComando(
    ManutencaoId Id,
    DateOnly ConcluidaEm,
    decimal? Custo,
    string? CodigoMoeda);
