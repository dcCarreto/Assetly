using System.Globalization;
using System.Text.Json;
using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Despesa;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Imovel;
using Assetly.Dominio.Manutencao;
using Assetly.Dominio.Saude;
using Assetly.Dominio.Veiculo;

namespace Assetly.Aplicacao.CasoDeUso;

/// <summary>Quanto entrou numa importação. <see cref="Ignorados"/>: registros ligados a um bem que não está no arquivo
/// nem no Assetly. <see cref="DocumentosSemArquivo"/>: documentos cujo arquivo não foi encontrado (ficam como ausentes).</summary>
public sealed record ResultadoImportacao(int Bens, int Registros, int Ignorados, int DocumentosSemArquivo);

/// <summary>
/// Importa o JSON gerado por <see cref="ExportarDados"/> (o arquivo completo ou só a parte "dados"). Cada registro
/// entra pelo seu id: o que já existe é atualizado, o que não existe é criado — importar o mesmo arquivo duas vezes
/// não duplica nada. Todos os registros são lidos e validados antes de gravar o primeiro: um arquivo com erro não
/// entra pela metade. Alertas automáticos não são importados (o Assetly os recalcula).
/// </summary>
public sealed class ImportarDados(FonteDados dados, IArmazenamentoDocumento armazenamento)
{
    private static readonly JsonSerializerOptions Opcoes = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly FonteDados _dados = dados ?? throw new ArgumentNullException(nameof(dados));
    private readonly IArmazenamentoDocumento _armazenamento = armazenamento ?? throw new ArgumentNullException(nameof(armazenamento));

    public ResultadoImportacao Executar(string json)
    {
        JsonDocument documento;
        try
        {
            documento = JsonDocument.Parse(json);
        }
        catch (JsonException excecao)
        {
            throw new ArgumentException("o arquivo não é um JSON válido.", excecao);
        }

        using (documento)
        {
            var raiz = documento.RootElement;
            if (raiz.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("o arquivo não é uma exportação do Assetly.");
            }

            if (raiz.TryGetProperty("aplicativo", out var aplicativo) && aplicativo.GetString() != "Assetly")
            {
                throw new ArgumentException("o arquivo não é uma exportação do Assetly.");
            }

            var conteudo = raiz.TryGetProperty("dados", out var interno) ? interno : raiz;
            if (!conteudo.TryGetProperty("bens", out _))
            {
                throw new ArgumentException("o arquivo não é uma exportação do Assetly (falta a lista de bens).");
            }

            return Gravar(Ler(conteudo));
        }
    }

    private sealed class Lote
    {
        public List<Bem> Bens { get; } = [];
        public List<Garantia> Garantias { get; } = [];
        public List<Manutencao> Manutencoes { get; } = [];
        public List<(Documento Documento, bool Encontrado)> Documentos { get; } = [];
        public List<Alerta> Alertas { get; } = [];
        public List<Saude> Convenios { get; } = [];
        public List<GastoSaude> GastosSaude { get; } = [];
        public List<Veiculo> Veiculos { get; } = [];
        public List<Abastecimento> Abastecimentos { get; } = [];
        public List<Imovel> Imoveis { get; } = [];
        public List<Despesa> Despesas { get; } = [];
        public List<(BemId Bem, BemId Responsavel)> ResponsavelDe { get; } = [];
    }

    private Lote Ler(JsonElement conteudo)
    {
        var lote = new Lote();
        Cada(conteudo, "bens", e => lote.Bens.Add(Bem.Restaurar(
            new BemId(G(e, "id")), new NomeBem(S(e, "nome")), E<TipoBem>(e, "tipo"), E<StatusBem>(e, "status"), D(e, "compradoEm"), M(e, "precoCompra"),
            S(e, "observacoes") ?? string.Empty, GOpcional(e, "responsavelId") is { } responsavel ? new BemId(responsavel) : null,
            new InventarioBem(S(e, "marca"), S(e, "modelo"), S(e, "numeroSerie"), S(e, "loja"), S(e, "localizacao")),
            M(e, "valorInformado"), D(e, "valorInformadoEm"), Dec(e, "taxaDepreciacao"))));
        Cada(conteudo, "garantias", e => lote.Garantias.Add(Garantia.Restaurar(
            new GarantiaId(G(e, "id")), new BemId(G(e, "bemId")), E<TipoGarantia>(e, "tipo"), S(e, "fornecedor"),
            ExigirValor(D(e, "iniciaEm"), "iniciaEm"), ExigirValor(D(e, "terminaEm"), "terminaEm"), S(e, "contatoSuporte"))));
        Cada(conteudo, "manutencoes", e => lote.Manutencoes.Add(Manutencao.Restaurar(
            new ManutencaoId(G(e, "id")), new BemId(G(e, "bemId")), E<TipoManutencao>(e, "tipo"), S(e, "descricao"),
            ExigirValor(D(e, "agendadaPara"), "agendadaPara"), D(e, "concluidaEm"), M(e, "custo"), E<StatusManutencao>(e, "status"))));
        Cada(conteudo, "documentos", e =>
        {
            var registrado = S(e, "arquivo") ?? string.Empty;
            var encontrado = registrado.Length == 0 ? null : _armazenamento.Localizar(registrado);
            var vinculo = S(e, "vinculoTipo") is { Length: > 0 } tipoVinculo
                ? new VinculoDocumento(Enum.Parse<TipoVinculoDocumento>(tipoVinculo), G(e, "vinculoId"))
                : null;
            var status = encontrado is null ? StatusDocumento.Ausente : E<StatusDocumento>(e, "status");
            lote.Documentos.Add((Documento.Restaurar(new DocumentoId(G(e, "id")), new BemId(G(e, "bemId")), E<TipoDocumento>(e, "tipo"),
                new NomeDocumento(S(e, "nome")), CaminhoDocumentoLocal.De(encontrado ?? (registrado.Length > 0 ? registrado : "arquivo-ausente")),
                ExigirValor(D(e, "registradoEm"), "registradoEm"), status, S(e, "chaveAcesso"), S(e, "urlConsulta"), vinculo), encontrado is not null));
        });
        Cada(conteudo, "alertas", e =>
        {
            if (!(e.TryGetProperty("automatico", out var automatico) && automatico.ValueKind == JsonValueKind.True))
            {
                lote.Alertas.Add(Alerta.Restaurar(new AlertaId(G(e, "id")), new BemId(G(e, "bemId")), E<TipoAlerta>(e, "tipo"),
                    E<SeveridadeAlerta>(e, "severidade"), S(e, "mensagem"), ExigirValor(D(e, "criadoEm"), "criadoEm"), D(e, "prazoEm"), E<StatusAlerta>(e, "status")));
            }
        });
        Cada(conteudo, "convenios", e => lote.Convenios.Add(Saude.Restaurar(
            new SaudeId(G(e, "id")), new BemId(G(e, "bemId")), E<TipoSaude>(e, "tipo"), S(e, "fornecedor"),
            ExigirValor(D(e, "iniciaEm"), "iniciaEm"), ExigirValor(D(e, "terminaEm"), "terminaEm"), S(e, "contato"), D(e, "canceladoEm"))));
        Cada(conteudo, "gastosSaude", e => lote.GastosSaude.Add(GastoSaude.Restaurar(
            new GastoSaudeId(G(e, "id")), new BemId(G(e, "bemId")), E<TipoGastoSaude>(e, "tipo"), S(e, "descricao"),
            ExigirValor(D(e, "data"), "data"), Exigir(M(e, "valor"), "valor"), S(e, "prestador"), S(e, "documentoPrestador"))));
        Cada(conteudo, "veiculos", e => lote.Veiculos.Add(Veiculo.Restaurar(new BemId(G(e, "bemId")),
            e.GetProperty("ficha").Deserialize<DadosVeiculo>(Opcoes) ?? throw new ArgumentException("ficha do veículo vazia"))));
        Cada(conteudo, "abastecimentos", e => lote.Abastecimentos.Add(Abastecimento.Restaurar(
            new AbastecimentoId(G(e, "id")), new BemId(G(e, "bemId")), ExigirValor(D(e, "data"), "data"), ExigirValor(I(e, "quilometragem"), "quilometragem"),
            ExigirValor(Dec(e, "litros"), "litros"), Exigir(M(e, "valor"), "valor"), E<TipoCombustivel>(e, "combustivel"),
            e.TryGetProperty("tanqueCheio", out var cheio) && cheio.ValueKind == JsonValueKind.True, S(e, "posto"))));
        Cada(conteudo, "imoveis", e => lote.Imoveis.Add(Imovel.Restaurar(new BemId(G(e, "bemId")), new DadosImovel(
            E<TipoImovel>(e, "tipo"), E<SituacaoImovel>(e, "situacao"), S(e, "logradouro"), S(e, "numero"), S(e, "complemento"), S(e, "bairro"),
            S(e, "cidade"), S(e, "uf"), S(e, "cep"), Dec(e, "areaM2"), I(e, "quartos"), I(e, "banheiros"), I(e, "vagas"), S(e, "matricula"),
            S(e, "inscricaoIptu"), M(e, "valorCondominio"), S(e, "seguradora"), S(e, "apolice"), D(e, "vencimentoSeguro"), S(e, "inquilino"),
            M(e, "valorAluguel"), D(e, "fimContratoAluguel")))));
        Cada(conteudo, "despesas", e => lote.Despesas.Add(Despesa.Restaurar(
            new DespesaId(G(e, "id")), new BemId(G(e, "bemId")), E<TipoDespesa>(e, "tipo"), S(e, "descricao"),
            ExigirValor(D(e, "vencimento"), "vencimento"), Exigir(M(e, "valor"), "valor"), D(e, "pagaEm"),
            e.TryGetProperty("recorrente", out var recorrente) && recorrente.ValueKind == JsonValueKind.True)));
        Cada(conteudo, "responsavelDe", e => lote.ResponsavelDe.Add((new BemId(G(e, "bemId")), new BemId(G(e, "responsavelId")))));
        return lote;
    }

    private ResultadoImportacao Gravar(Lote lote)
    {
        var existentes = _dados.Bens.Listar().Select(b => b.Id).ToHashSet();
        var importados = lote.Bens.Select(b => b.Id).ToHashSet();
        bool Conhecido(BemId id) => existentes.Contains(id) || importados.Contains(id);

        // Responsável que não está em lugar nenhum vira "sem responsável"; quem tem responsável entra depois dele.
        foreach (var bem in lote.Bens.Where(b => b.ResponsavelId is { } responsavel && !Conhecido(responsavel)))
        {
            bem.DefinirResponsavel(null);
        }

        var pendentes = lote.Bens.ToList();
        while (pendentes.Count > 0)
        {
            var prontos = pendentes.Where(b => b.ResponsavelId is not { } responsavel || existentes.Contains(responsavel)).ToList();
            if (prontos.Count == 0)
            {
                prontos = pendentes;
                prontos.ForEach(b => b.DefinirResponsavel(null));
            }

            foreach (var bem in prontos)
            {
                _dados.Bens.Salvar(bem);
                existentes.Add(bem.Id);
            }

            pendentes = pendentes.Except(prontos).ToList();
        }

        var registros = 0;
        var ignorados = 0;
        void GravarLigados<T>(IEnumerable<T> itens, Func<T, BemId> bem, Action<T> salvar)
        {
            foreach (var item in itens)
            {
                if (!Conhecido(bem(item)))
                {
                    ignorados++;
                    continue;
                }

                salvar(item);
                registros++;
            }
        }

        GravarLigados(lote.Veiculos, v => v.BemId, _dados.Veiculos.Salvar);
        GravarLigados(lote.Imoveis, i => i.BemId, _dados.Imoveis.Salvar);
        GravarLigados(lote.Garantias, g => g.BemId, _dados.Garantias.Salvar);
        GravarLigados(lote.Manutencoes, m => m.BemId, _dados.Manutencoes.Salvar);
        GravarLigados(lote.Documentos, d => d.Documento.BemId, d =>
        {
            // Arquivo achado fora da pasta privada (ex.: exportação de outro ambiente): copia para dentro, senão o backup não o leva.
            if (d.Encontrado)
            {
                d.Documento.RelocalizarArquivo(CaminhoDocumentoLocal.De(_armazenamento.Armazenar(d.Documento.CaminhoLocal.Valor)));
            }

            _dados.Documentos.Salvar(d.Documento);
        });
        GravarLigados(lote.Alertas, a => a.BemId, _dados.Alertas.Salvar);
        GravarLigados(lote.Convenios, s => s.BemId, _dados.Convenios.Salvar);
        GravarLigados(lote.GastosSaude, g => g.BemId, _dados.GastosSaude.Salvar);
        GravarLigados(lote.Abastecimentos, a => a.BemId, _dados.Abastecimentos.Salvar);
        GravarLigados(lote.Despesas, d => d.BemId, _dados.Despesas.Salvar);

        foreach (var (bemId, responsavel) in lote.ResponsavelDe)
        {
            if (_dados.Bens.BuscarPorId(bemId) is { ResponsavelId: null } bem && Conhecido(responsavel))
            {
                bem.DefinirResponsavel(responsavel);
                _dados.Bens.Salvar(bem);
            }
        }

        return new ResultadoImportacao(lote.Bens.Count, registros, ignorados, lote.Documentos.Count(d => !d.Encontrado));
    }

    private static void Cada(JsonElement conteudo, string colecao, Action<JsonElement> ler)
    {
        if (!conteudo.TryGetProperty(colecao, out var lista) || lista.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var indice = 0;
        foreach (var item in lista.EnumerateArray())
        {
            indice++;
            try
            {
                ler(item);
            }
            catch (Exception excecao) when (excecao is ArgumentException or FormatException or InvalidOperationException or JsonException or KeyNotFoundException)
            {
                throw new ArgumentException($"registro {indice} de \"{colecao}\" inválido: {excecao.Message} Nada foi importado.", excecao);
            }
        }
    }

    private static T ExigirValor<T>(T? valor, string campo) where T : struct =>
        valor ?? throw new ArgumentException($"campo \"{campo}\" é obrigatório.");

    private static T Exigir<T>(T? valor, string campo) where T : class =>
        valor ?? throw new ArgumentException($"campo \"{campo}\" é obrigatório.");

    private static JsonElement? Campo(JsonElement e, string nome) =>
        e.TryGetProperty(nome, out var valor) && valor.ValueKind != JsonValueKind.Null ? valor : null;

    private static string? S(JsonElement e, string nome) => Campo(e, nome)?.GetString();

    private static Guid G(JsonElement e, string nome) => GOpcional(e, nome) ?? throw new ArgumentException($"campo \"{nome}\" é obrigatório.");

    private static Guid? GOpcional(JsonElement e, string nome) => Campo(e, nome)?.GetGuid();

    private static DateOnly? D(JsonElement e, string nome) =>
        Campo(e, nome)?.GetString() is { } texto ? DateOnly.Parse(texto, CultureInfo.InvariantCulture) : null;

    private static decimal? Dec(JsonElement e, string nome) => Campo(e, nome)?.GetDecimal();

    private static int? I(JsonElement e, string nome) => Campo(e, nome)?.GetInt32();

    private static TEnum E<TEnum>(JsonElement e, string nome) where TEnum : struct, Enum =>
        Enum.Parse<TEnum>(S(e, nome) ?? throw new ArgumentException($"campo \"{nome}\" é obrigatório."));

    private static Dinheiro? M(JsonElement e, string nome) =>
        Campo(e, nome) is { } valor ? Dinheiro.De(valor.GetProperty("valor").GetDecimal(), valor.GetProperty("moeda").GetString()!) : null;
}
