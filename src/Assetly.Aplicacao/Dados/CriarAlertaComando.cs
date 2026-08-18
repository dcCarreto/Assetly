using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;

namespace Assetly.Aplicacao.Dados;

public sealed record CriarAlertaComando(
    BemId BemId,
    TipoAlerta Tipo,
    SeveridadeAlerta Severidade,
    string? Mensagem,
    DateOnly CriadoEm,
    DateOnly? PrazoEm);
