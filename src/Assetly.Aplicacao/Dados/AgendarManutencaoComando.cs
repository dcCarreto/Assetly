using Assetly.Dominio.Bem;
using Assetly.Dominio.Manutencao;

namespace Assetly.Aplicacao.Dados;

public sealed record AgendarManutencaoComando(
    BemId BemId,
    TipoManutencao Tipo,
    string? Descricao,
    DateOnly AgendadaPara);
