using Assetly.Dominio.Bem;
using Assetly.Dominio.Saude;

namespace Assetly.Aplicacao.Dados;

public sealed record RegistrarSaudeComando(
    BemId BemId,
    TipoSaude Tipo,
    string? Fornecedor,
    DateOnly IniciaEm,
    DateOnly TerminaEm,
    string? Contato);
