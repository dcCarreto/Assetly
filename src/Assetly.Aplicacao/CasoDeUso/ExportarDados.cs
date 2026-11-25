using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Compartilhado;

namespace Assetly.Aplicacao.CasoDeUso;

/// <summary>
/// Exporta todos os dados do usuário em JSON legível (portabilidade: planilhas, outros programas,
/// arquivo pessoal). A mesma montagem gera a "impressão digital" usada pela sincronização: uma versão
/// canônica que ignora o que é derivado ou específico do computador (alertas automáticos, que cada
/// computador regenera; caminho completo dos arquivos, que muda de um perfil de usuário para outro).
/// </summary>
public sealed class ExportarDados(FonteDados dados)
{
    private static readonly JsonSerializerOptions Opcoes = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly FonteDados _dados = dados ?? throw new ArgumentNullException(nameof(dados));

    public string Executar() => JsonSerializer.Serialize(Montar(paraComparacao: false), Opcoes);

    /// <summary>SHA-256 do conteúdo canônico (ver resumo da classe), em hexadecimal.</summary>
    public string ImpressaoDigital()
    {
        var json = JsonSerializer.Serialize(Montar(paraComparacao: true), Opcoes);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    /// <summary>Verdadeiro quando ainda não há nenhum cadastro (instalação nova) — alertas automáticos não contam.</summary>
    public bool SemDados() =>
        _dados.Bens.Listar().Count == 0 && _dados.Documentos.Listar().Count == 0 && _dados.Garantias.Listar().Count == 0
        && _dados.Manutencoes.Listar().Count == 0 && _dados.Alertas.Listar().All(a => a.Automatico);

    /// <summary>Só o bem e tudo o que é ligado a ele, no formato de "dados" — usado pela lixeira para devolver o bem
    /// excluído com <see cref="ImportarDados"/>. Inclui quais bens tinham este (uma pessoa) como responsável.</summary>
    public string ExecutarDoBem(Dominio.Bem.BemId bemId)
    {
        var conteudo = (Dictionary<string, object?>)Montar(paraComparacao: false, soDoBem: bemId, envelope: false);
        conteudo["responsavelDe"] = _dados.Bens.Listar()
            .Where(b => b.ResponsavelId is { } responsavel && responsavel.Equals(bemId))
            .Select(b => new { bemId = b.Id.Valor, responsavelId = bemId.Valor })
            .ToList();
        return JsonSerializer.Serialize(conteudo, Opcoes);
    }

    private object Montar(bool paraComparacao, Dominio.Bem.BemId? soDoBem = null, bool envelope = true)
    {
        bool Do(Dominio.Bem.BemId id) => soDoBem is null || id.Equals(soDoBem.Value);

        static object? D(Dinheiro? valor) => valor is null ? null : new { valor = valor.Valor, moeda = valor.CodigoMoeda };

        var bens = _dados.Bens.Listar().Where(b => Do(b.Id)).OrderBy(b => b.Id.Valor).Select(b =>
        {
            var bem = new Dictionary<string, object?>
            {
                ["id"] = b.Id.Valor,
                ["nome"] = b.Nome.Valor,
                ["tipo"] = b.Tipo.ToString(),
                ["status"] = b.Status.ToString(),
                ["compradoEm"] = b.CompradoEm,
                ["precoCompra"] = D(b.PrecoCompra),
                ["observacoes"] = b.Observacoes,
                ["responsavelId"] = b.ResponsavelId?.Valor
            };

            // Campos mais novos só entram quando preenchidos: a impressão digital de quem não os usa não muda com a versão.
            Opcional(bem, "marca", b.Inventario.Marca);
            Opcional(bem, "modelo", b.Inventario.Modelo);
            Opcional(bem, "numeroSerie", b.Inventario.NumeroSerie);
            Opcional(bem, "loja", b.Inventario.Loja);
            Opcional(bem, "localizacao", b.Inventario.Localizacao);
            Opcional(bem, "valorInformado", D(b.ValorInformado));
            Opcional(bem, "valorInformadoEm", b.ValorInformado is null ? null : b.ValorInformadoEm);
            Opcional(bem, "taxaDepreciacao", b.TaxaDepreciacao);
            return bem;
        });

        var conteudo = new Dictionary<string, object?>
        {
            ["bens"] = bens,
            ["garantias"] = _dados.Garantias.Listar().Where(x => Do(x.BemId)).OrderBy(g => g.Id.Valor).Select(g => new
            {
                id = g.Id.Valor, bemId = g.BemId.Valor, tipo = g.Tipo.ToString(), fornecedor = g.Fornecedor,
                iniciaEm = g.Periodo.IniciaEm, terminaEm = g.Periodo.TerminaEm, contatoSuporte = g.ContatoSuporte
            }),
            ["manutencoes"] = _dados.Manutencoes.Listar().Where(x => Do(x.BemId)).OrderBy(m => m.Id.Valor).Select(m => new
            {
                id = m.Id.Valor, bemId = m.BemId.Valor, tipo = m.Tipo.ToString(), descricao = m.Descricao,
                agendadaPara = m.AgendadaPara, concluidaEm = m.ConcluidaEm, custo = D(m.Custo), status = m.Status.ToString()
            }),
            ["documentos"] = _dados.Documentos.Listar().Where(x => Do(x.BemId)).OrderBy(d => d.Id.Valor).Select(d =>
            {
                var documento = new Dictionary<string, object?>
                {
                    ["id"] = d.Id.Valor, ["bemId"] = d.BemId.Valor, ["tipo"] = d.Tipo.ToString(), ["nome"] = d.Nome.Valor,
                    ["arquivo"] = paraComparacao ? Path.GetFileName(d.CaminhoLocal.Valor) : d.CaminhoLocal.Valor,
                    ["registradoEm"] = d.RegistradoEm, ["status"] = d.Status.ToString(), ["chaveAcesso"] = d.ChaveAcesso, ["urlConsulta"] = d.UrlConsulta
                };
                Opcional(documento, "vinculoTipo", d.Vinculo?.Tipo.ToString());
                Opcional(documento, "vinculoId", d.Vinculo?.Id);
                return documento;
            }),
            ["alertas"] = _dados.Alertas.Listar().Where(a => Do(a.BemId) && (!paraComparacao || !a.Automatico)).OrderBy(a => a.Id.Valor).Select(a => new
            {
                id = a.Id.Valor, bemId = a.BemId.Valor, tipo = a.Tipo.ToString(), severidade = a.Severidade.ToString(),
                mensagem = a.Mensagem, criadoEm = a.CriadoEm, prazoEm = a.PrazoEm, status = a.Status.ToString(), automatico = a.Automatico
            }),
            ["convenios"] = _dados.Convenios.Listar().Where(x => Do(x.BemId)).OrderBy(s => s.Id.Valor).Select(s => new
            {
                id = s.Id.Valor, bemId = s.BemId.Valor, tipo = s.Tipo.ToString(), fornecedor = s.Fornecedor, contato = s.Contato,
                iniciaEm = s.Periodo.IniciaEm, terminaEm = s.Periodo.TerminaEm, canceladoEm = s.DataCancelamento
            }),
            ["gastosSaude"] = _dados.GastosSaude.Listar().Where(x => Do(x.BemId)).OrderBy(g => g.Id.Valor).Select(g =>
            {
                var gasto = new Dictionary<string, object?>
                {
                    ["id"] = g.Id.Valor, ["bemId"] = g.BemId.Valor, ["tipo"] = g.Tipo.ToString(), ["descricao"] = g.Descricao, ["data"] = g.Data, ["valor"] = D(g.Valor)
                };
                Opcional(gasto, "prestador", g.Prestador);
                Opcional(gasto, "documentoPrestador", g.DocumentoPrestador);
                return gasto;
            }),
            ["veiculos"] = _dados.Veiculos.Listar().Where(x => Do(x.BemId)).OrderBy(v => v.BemId.Valor).Select(v => new { bemId = v.BemId.Valor, ficha = v.Dados() }),
            ["abastecimentos"] = _dados.Abastecimentos.Listar().Where(x => Do(x.BemId)).OrderBy(a => a.Id.Valor).Select(a => new
            {
                id = a.Id.Valor, bemId = a.BemId.Valor, data = a.Data, quilometragem = a.Quilometragem, litros = a.Litros,
                valor = D(a.Valor), combustivel = a.Combustivel.ToString(), tanqueCheio = a.TanqueCheio, posto = a.Posto
            }),
            ["imoveis"] = _dados.Imoveis.Listar().Where(x => Do(x.BemId)).OrderBy(i => i.BemId.Valor).Select(i => new
            {
                bemId = i.BemId.Valor, tipo = i.Tipo.ToString(), situacao = i.Situacao.ToString(), endereco = i.EnderecoCompleto,
                logradouro = i.Logradouro, numero = i.Numero, complemento = i.Complemento, bairro = i.Bairro, cidade = i.Cidade, uf = i.Uf,
                cep = i.Cep, areaM2 = i.AreaM2, quartos = i.Quartos, banheiros = i.Banheiros, vagas = i.Vagas, matricula = i.Matricula,
                inscricaoIptu = i.InscricaoIptu, valorCondominio = D(i.ValorCondominio), seguradora = i.Seguradora, apolice = i.Apolice,
                vencimentoSeguro = i.VencimentoSeguro, inquilino = i.Inquilino, valorAluguel = D(i.ValorAluguel), fimContratoAluguel = i.FimContratoAluguel
            }),
            ["despesas"] = _dados.Despesas.Listar().Where(x => Do(x.BemId)).OrderBy(d => d.Id.Valor).Select(d =>
            {
                var despesa = new Dictionary<string, object?>
                {
                    ["id"] = d.Id.Valor, ["bemId"] = d.BemId.Valor, ["tipo"] = d.Tipo.ToString(), ["descricao"] = d.Descricao,
                    ["vencimento"] = d.Vencimento, ["valor"] = D(d.Valor), ["pagaEm"] = d.PagaEm
                };
                Opcional(despesa, "recorrente", d.Recorrente ? true : null);
                return despesa;
            })
        };

        if (!paraComparacao && envelope)
        {
            return new Dictionary<string, object?>
            {
                ["aplicativo"] = "Assetly",
                ["formato"] = 1,
                ["exportadoEm"] = DateTimeOffset.Now,
                ["dados"] = conteudo
            };
        }

        return conteudo;
    }

    private static void Opcional(Dictionary<string, object?> registro, string chave, object? valor)
    {
        if (valor is not (null or ""))
        {
            registro[chave] = valor;
        }
    }
}
