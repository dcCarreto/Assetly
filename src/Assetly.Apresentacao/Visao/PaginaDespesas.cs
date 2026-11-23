using System.Windows;
using System.Windows.Controls;
using Assetly.Aplicacao.CasoDeUso;
using Assetly.Aplicacao.Dados;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Despesa;
using static Assetly.Apresentacao.Visao.AcoesApoio;
using static Assetly.Apresentacao.Visao.ContextoApoio;
using static Assetly.Apresentacao.Visao.DialogoApoio;
using static Assetly.Apresentacao.Visao.LayoutApoio;

namespace Assetly.Apresentacao.Visao;

/// <summary>
/// Contas a pagar de veículos (IPVA, licenciamento, seguro, multas...) ou de imóveis (IPTU, condomínio,
/// água, energia...). A mesma página serve aos dois módulos: muda só quais bens aparecem (os que têm
/// ficha de veículo ou de imóvel) e quais tipos de despesa são oferecidos.
/// </summary>
internal sealed class PaginaDespesas : IPagina
{
    private readonly ContextoAplicacao _contexto;
    private readonly Func<IReadOnlyCollection<BemId>> _bensDoModulo;
    private readonly IReadOnlyList<TipoDespesa> _tipos;
    private readonly string _rotuloBem;
    private readonly string _termo;
    private readonly DataGrid _tabela = new();
    private readonly FiltroTabela<DespesaLinha> _filtro;
    private readonly TextBlock _resumoEsquerda = new();
    private readonly TextBlock _resumoDireita = new();

    public string Titulo { get; }
    public UIElement Conteudo { get; }

    internal PaginaDespesas(ContextoAplicacao contexto, string titulo, string descricao, string rotuloBem,
        IReadOnlyList<TipoDespesa> tipos, Func<IReadOnlyCollection<BemId>> bensDoModulo, string termo = "despesa")
    {
        _contexto = contexto;
        Titulo = titulo;
        _rotuloBem = rotuloBem;
        _termo = termo;
        _tipos = tipos;
        _bensDoModulo = bensDoModulo;
        ConfigurarTabela();

        var (direita, busca) = CabecalhoBuscaCriacao($"Buscar por {rotuloBem.ToLowerInvariant()}, tipo ou descrição", $"Nova {termo}", () => AbrirDialogo(null, null));

        _filtro = new FiltroTabela<DespesaLinha>(_tabela, l => l.Id, l => $"{l.Bem} {l.Tipo} {l.Descricao}",
        [
            new("Em aberto", l => l.Origem.PagaEm is null),
            new("Vencidas", l => l.StatusClasse == "etiqueta-vencida"),
            new("A vencer", l => l.StatusClasse == "etiqueta-a-vencer"),
            new("Pagas", l => l.Origem.PagaEm is not null),
            new("Este ano", l => l.Origem.Vencimento.Year == Hoje().Year),
            new("Todas", _ => true)
        ]);
        busca.TextChanged += (_, _) => _filtro.DefinirBusca(busca.Text);

        var contextual = BarraContextual(_tabela,
        [
            ("Pagar", () => Pagar(Selecionada().Origem)),
            ("Desfazer pagamento", () => DesfazerPagamento(Selecionada().Origem)),
            ("Editar", () => AbrirDialogo(Selecionada().Origem, null))
        ],
        [
            ("Excluir", () => Excluir(Selecionada()))
        ]);

        Conteudo = TelaComTabela(titulo, descricao, direita, _filtro.Barra, _tabela, contextual, BarraResumoTabela(_resumoEsquerda, _resumoDireita));
        ConfigurarAtalhosTabela(Conteudo, _tabela, busca, () => AbrirDialogo(null, null), () => AbrirDialogo(Selecionada().Origem, null), () => Excluir(Selecionada()));
    }

    public void Atualizar()
    {
        var bens = _bensDoModulo().ToHashSet();
        var despesas = _contexto.ListarDespesas().Executar().Where(d => bens.Contains(d.BemId)).ToList();
        _filtro.AtualizarDados(ConstruirLinhas(despesas));
    }

    private DespesaLinha Selecionada() => Selecionado<DespesaLinha>(_tabela, "Selecione uma despesa.");

    private void ConfigurarTabela()
    {
        TabelaApoio.AplicarEstiloBase(_tabela);
        _tabela.Columns.Add(TabelaApoio.ColunaTexto(_rotuloBem, nameof(DespesaLinha.Bem), Estrela(1.3), estiloBase: "CelulaDestaqueStyle"));
        _tabela.Columns.Add(TabelaApoio.ColunaEtiqueta("Status", nameof(DespesaLinha.Status), nameof(DespesaLinha.StatusClasse), new DataGridLength(112)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Tipo", nameof(DespesaLinha.Tipo), new DataGridLength(140)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Descrição", nameof(DespesaLinha.Descricao), Estrela(2), estiloBase: "CelulaSecundariaStyle"));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Vencimento", nameof(DespesaLinha.Vencimento), new DataGridLength(110), nameof(DespesaLinha.VencimentoOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Valor", nameof(DespesaLinha.Valor), new DataGridLength(124), nameof(DespesaLinha.ValorOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Paga em", nameof(DespesaLinha.PagaEm), new DataGridLength(104), nameof(DespesaLinha.PagaEmOrdenacao)));
        TabelaApoio.ConfigurarLinhas<DespesaLinha>(_tabela, l => l.LinhaClasse, MenuContexto, l => AbrirDialogo(l.Origem, null));
    }

    private IReadOnlyList<DespesaLinha> ConstruirLinhas(IReadOnlyList<Despesa> despesas)
    {
        var hoje = Hoje();
        var nomeBem = NomesBens(_contexto);
        var abertas = despesas.Where(d => d.PagaEm is null).ToList();
        var vencidas = abertas.Count(d => d.StatusEm(hoje) == StatusDespesa.Vencida);
        var pagasNoAno = despesas.Where(d => d.PagaEm?.Year == hoje.Year);
        _resumoEsquerda.Text = $"{abertas.Count} em aberto · {vencidas} vencida{(vencidas == 1 ? "" : "s")} · em aberto {Formatador.Total(abertas.Select(d => (Dominio.Compartilhado.Dinheiro?)d.Valor))}";
        _resumoDireita.Text = $"Pago em {hoje.Year}: {Formatador.Total(pagasNoAno.Select(d => (Dominio.Compartilhado.Dinheiro?)d.Valor))}";

        return despesas.Select(d =>
        {
            var status = d.StatusEm(hoje);
            return new DespesaLinha(d.Id, nomeBem(d.BemId), Formatador.TextoEnum(d.Tipo), d.Descricao, Formatador.Data(d.Vencimento),
                Formatador.Dinheiro(d.Valor), Formatador.TextoEnum(status), Formatador.ClasseEtiqueta(status), ClasseLinha(status),
                Formatador.Data(d.PagaEm), d);
        }).ToList();
    }

    private static string ClasseLinha(StatusDespesa status) => status switch
    {
        StatusDespesa.Vencida => "linha-perigo",
        StatusDespesa.AVencer => "linha-aviso",
        StatusDespesa.Paga => "linha-ok",
        _ => "linha-informativa"
    };

    private ContextMenu MenuContexto(DespesaLinha linha)
    {
        var paga = linha.Origem.PagaEm is not null;
        var menu = new ContextMenu();
        menu.Items.Add(ItemMenu("Marcar como paga", () => Pagar(linha.Origem), !paga));
        menu.Items.Add(ItemMenu("Desfazer pagamento", () => DesfazerPagamento(linha.Origem), paga));
        menu.Items.Add(ItemMenu("Editar", () => AbrirDialogo(linha.Origem, null)));
        menu.Items.Add(new Separator());
        menu.Items.Add(ItemMenu("Excluir…", () => Excluir(linha)));
        return menu;
    }

    private static MenuItem ItemMenu(string texto, Action acao, bool habilitado = true)
    {
        var item = new MenuItem { Header = texto, IsEnabled = habilitado };
        item.Click += (_, _) => ExecutarComTratamento(acao);
        return item;
    }

    /// <summary>Abre o formulário de despesa. <paramref name="bemPreSelecionado"/> vem das ações da ficha
    /// ("Nova despesa" a partir de um veículo/imóvel selecionado).</summary>
    internal void AbrirDialogo(Despesa? despesa, BemId? bemPreSelecionado)
    {
        var nomeBem = NomesBens(_contexto);
        var bens = _bensDoModulo().Select(id => new ItemValor<BemId>(id, nomeBem(id))).OrderBy(i => i.Texto, StringComparer.CurrentCulture).ToList();
        if (bens.Count == 0)
        {
            throw new InvalidOperationException($"cadastre primeiro um {_rotuloBem.ToLowerInvariant()} na aba ao lado para poder lançar despesas.");
        }

        var alvo = despesa?.BemId ?? bemPreSelecionado;
        var bem = ApoioFichas.ComboItens(bens, id => alvo is not null && id.Equals(alvo.Value));
        bem.IsEnabled = despesa is null;
        var tipo = ComboValores(_tipos, despesa?.Tipo ?? _tipos[0]);
        var descricao = CampoTexto("Descrição", despesa?.Descricao ?? string.Empty);
        var vencimento = DataPicker(despesa?.Vencimento ?? Hoje());
        var valor = CampoTexto("Valor (ex.: 1.234,56)", TextoValor(despesa?.Valor.Valor));
        var pagaEm = DataPicker(despesa?.PagaEm);
        var repeticoes = CampoTexto("Quantidade de meses/parcelas", "1");

        var grade = GradeFormulario();
        AdicionarCampo(grade, 0, 0, $"{_rotuloBem} *", bem);
        AdicionarCampo(grade, 0, 1, "Tipo *", tipo);
        AdicionarCampo(grade, 1, 0, "Descrição *", descricao, ocupaLinhaToda: true);
        AdicionarCampo(grade, 2, 0, "Vencimento *", vencimento);
        AdicionarCampo(grade, 2, 1, "Valor *", valor);
        AdicionarCampo(grade, 3, 0, "Paga em (vazio = em aberto)", pagaEm);
        if (despesa is null)
        {
            AdicionarCampo(grade, 3, 1, "Repetir por (meses)", repeticoes);
        }

        // Preenche a descrição com o tipo quando o usuário ainda não digitou nada.
        tipo.SelectionChanged += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(descricao.Text) || _tipos.Any(t => Formatador.TextoEnum(t) == descricao.Text))
            {
                descricao.Text = Formatador.TextoEnum(ValorEnum<TipoDespesa>(tipo));
            }
        };
        if (despesa is null)
        {
            descricao.Text = Formatador.TextoEnum(_tipos[0]);
        }

        var salvou = MostrarFormulario(despesa is null ? $"Nova {_termo}" : $"Editar {_termo}", grade, () =>
        {
            var dataVencimento = DataObrigatoria(vencimento, "vencimento");
            var valorDecimal = DecimalOpcional(valor.Text) ?? throw new ArgumentException("informe o valor da despesa.");
            var dataPagamento = ValorData(pagaEm);
            if (despesa is null)
            {
                var quantidade = InteiroOpcional(repeticoes.Text, "repetir por") ?? 1;
                if (quantidade > 1 && dataPagamento is not null)
                {
                    throw new ArgumentException("para várias parcelas, deixe \"Paga em\" vazio e marque cada parcela como paga quando pagar.");
                }

                var criadas = _contexto.RegistrarDespesa().Executar(new RegistrarDespesaComando(
                    ApoioFichas.ValorItem<BemId>(bem, "selecione o bem"), ValorEnum<TipoDespesa>(tipo), descricao.Text, dataVencimento,
                    valorDecimal, ApoioFichas.MoedaPadrao, quantidade));
                if (dataPagamento is not null)
                {
                    _contexto.PagarDespesa().Executar(criadas[0].Id, dataPagamento.Value);
                }
            }
            else
            {
                _contexto.EditarDespesa().Executar(new EditarDespesaComando(despesa.Id, ValorEnum<TipoDespesa>(tipo), descricao.Text,
                    dataVencimento, valorDecimal, despesa.Valor.CodigoMoeda));
                if (despesa.PagaEm != dataPagamento)
                {
                    if (despesa.PagaEm is not null)
                    {
                        _contexto.DesfazerPagamentoDespesa().Executar(despesa.Id);
                    }

                    if (dataPagamento is not null)
                    {
                        _contexto.PagarDespesa().Executar(despesa.Id, dataPagamento.Value);
                    }
                }
            }
        });

        if (salvou)
        {
            _contexto.NotificarAlteracao();
        }
    }

    private void Pagar(Despesa despesa)
    {
        if (despesa.PagaEm is not null)
        {
            throw new InvalidOperationException("esta despesa já está paga.");
        }

        var data = DataPicker(Hoje());
        var grade = GradeFormulario();
        AdicionarCampo(grade, 0, 0, "Data do pagamento *", data);
        if (MostrarFormulario($"Pagar: {despesa.Descricao}", grade,
                () => _contexto.PagarDespesa().Executar(despesa.Id, DataObrigatoria(data, "pagamento")), 480))
        {
            _contexto.NotificarAlteracao();
        }
    }

    private void DesfazerPagamento(Despesa despesa)
    {
        _contexto.DesfazerPagamentoDespesa().Executar(despesa.Id);
        _contexto.NotificarAlteracao();
    }

    private void Excluir(DespesaLinha linha)
    {
        if (!MostrarConfirmacao($"Excluir {_termo}", $"A {_termo} \"{linha.Descricao}\" ({linha.Valor}, vencimento {linha.Vencimento}) será excluída permanentemente. Deseja continuar?", "Excluir"))
        {
            return;
        }

        _contexto.ExcluirDespesa().Executar(linha.Id);
        _contexto.NotificarAlteracao();
    }
}
