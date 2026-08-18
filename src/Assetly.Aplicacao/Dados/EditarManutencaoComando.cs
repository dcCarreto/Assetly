using Assetly.Dominio.Manutencao;

namespace Assetly.Aplicacao.Dados;

public sealed record EditarManutencaoComando(
    ManutencaoId Id,
    TipoManutencao Tipo,
    string? Descricao,
    DateOnly AgendadaPara);
