using Assetly.Dominio.Saude;

namespace Assetly.Aplicacao.Dados;

public sealed record EditarSaudeComando(
    SaudeId Id,
    TipoSaude Tipo,
    string? Fornecedor,
    DateOnly IniciaEm,
    DateOnly TerminaEm,
    string? Contato);
