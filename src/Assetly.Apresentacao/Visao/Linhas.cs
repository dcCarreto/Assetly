using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Manutencao;
using Assetly.Dominio.Saude;

namespace Assetly.Apresentacao.Visao;

internal sealed record BemLinha(
    BemId Id, string Nome, string Tipo, string Status, string StatusClasse, string LinhaClasse,
    string Compra, string Valor, string Observacoes, Bem Origem);

internal sealed record GarantiaLinha(
    GarantiaId Id, string Bem, string Tipo, string Fornecedor, string Inicio, string Fim,
    string Status, string StatusClasse, string LinhaClasse, string Suporte, Garantia Origem);

internal sealed record ManutencaoLinha(
    ManutencaoId Id, string Bem, string Tipo, string Descricao, string Agendada, string Concluida,
    string Custo, string Status, string StatusClasse, string LinhaClasse, Manutencao Origem);

internal sealed record DocumentoLinha(
    DocumentoId Id, string Bem, string Tipo, string Nome, string ArquivoLocal, string Registrado,
    string Status, string StatusClasse, string LinhaClasse, Documento Origem);

internal sealed record AlertaLinha(
    AlertaId Id, string Bem, string Tipo, string Severidade, string SeveridadeClasse, string Mensagem,
    string Criado, string Prazo, string Status, string StatusClasse, string LinhaClasse, Alerta Origem);

internal sealed record SaudeLinha(
    SaudeId Id, string Bem, string Tipo, string Fornecedor, string Inicio, string Fim,
    string Status, string StatusClasse, string LinhaClasse, string Contato, Saude Origem);

internal sealed record GastoSaudeLinha(
    GastoSaudeId Id, string Bem, string Tipo, string Descricao, string Data, string Valor, GastoSaude Origem);
