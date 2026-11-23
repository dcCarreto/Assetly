using System.Windows;
using System.Windows.Controls;
using Assetly.Aplicacao.Dados;
using Assetly.Dominio.Garantia;
using static Assetly.Apresentacao.Visao.AcoesApoio;
using static Assetly.Apresentacao.Visao.ContextoApoio;
using static Assetly.Apresentacao.Visao.DialogoApoio;
using static Assetly.Apresentacao.Visao.LayoutApoio;

namespace Assetly.Apresentacao.Visao;

internal sealed class ModuloGarantias : IModulo
{
    private readonly ContextoAplicacao _contexto;
    private readonly DataGrid _tabela = new();
    private readonly FiltroTabela<GarantiaLinha> _filtro;
    private readonly TextBlock _resumoEsquerda = new();
    private readonly TextBlock _resumoDireita = new();
    private int _contador;

    public string Titulo => "Garantias";
    public string Descricao => "Cobertura e vencimentos";
    public bool ComContador => true;
    public UIElement Conteudo { get; }
    public int Contador => _contador;

    internal ModuloGarantias(ContextoAplicacao contexto)
    {
        _contexto = contexto;
        ConfigurarTabela();

        var (direita, busca) = CabecalhoBuscaCriacao("Buscar por bem, fornecedor ou suporte", "Nova garantia", () => AbrirDialogoGarantia(null));

        _filtro = new FiltroTabela<GarantiaLinha>(_tabela, l => l.Id, l => $"{l.Bem} {l.Fornecedor} {l.Tipo} {l.Suporte}",
        [
            new("Todas", _ => true),
            new("Vencidas", l => l.StatusClasse == "etiqueta-vencida"),
            new("Próximas", l => l.StatusClasse == "etiqueta-perto-do-vencimento"),
            new("Ativas", l => l.StatusClasse == "etiqueta-ativa")
        ]);
        busca.TextChanged += (_, _) => _filtro.DefinirBusca(busca.Text);

        var contextual = BarraContextual(_tabela,
        [
            ("Detalhes", () => MostrarInformacao("Garantia", Detalhes(SelecionadaAtual()))),
            ("Editar", () => AbrirDialogoGarantia(SelecionadaAtual().Origem))
        ],
        [
            ("Excluir", () => ExcluirGarantia(SelecionadaAtual()))
        ]);

        Conteudo = TelaComTabela("Garantias", "Coberturas cadastradas, fornecedores e vencimentos.", direita,
            _filtro.Barra, _tabela, contextual, BarraResumoTabela(_resumoEsquerda, _resumoDireita));

        ConfigurarAtalhosTabela(Conteudo, _tabela, busca, () => AbrirDialogoGarantia(null),
            () => AbrirDialogoGarantia(SelecionadaAtual().Origem), () => ExcluirGarantia(SelecionadaAtual()));
    }

    public void Atualizar()
    {
        var garantias = _contexto.ListarGarantias().Executar();
        _contador = garantias.Count;
        _filtro.AtualizarDados(ConstruirLinhas(garantias));
    }

    private GarantiaLinha SelecionadaAtual() => Selecionado<GarantiaLinha>(_tabela, "Selecione uma garantia.");

    private void ConfigurarTabela()
    {
        TabelaApoio.AplicarEstiloBase(_tabela);
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Bem", nameof(GarantiaLinha.Bem), Estrela(1.7), estiloBase: "CelulaDestaqueStyle"));
        _tabela.Columns.Add(TabelaApoio.ColunaEtiqueta("Status", nameof(GarantiaLinha.Status), nameof(GarantiaLinha.StatusClasse), new DataGridLength(170)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Tipo", nameof(GarantiaLinha.Tipo), new DataGridLength(130)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Fornecedor", nameof(GarantiaLinha.Fornecedor), Estrela(1.4)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Início", nameof(GarantiaLinha.Inicio), new DataGridLength(104), nameof(GarantiaLinha.InicioOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Fim", nameof(GarantiaLinha.Fim), new DataGridLength(104), nameof(GarantiaLinha.FimOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Suporte", nameof(GarantiaLinha.Suporte), new DataGridLength(140), estiloBase: "CelulaSecundariaStyle"));
        TabelaApoio.ConfigurarLinhas<GarantiaLinha>(_tabela, l => l.LinhaClasse, MenuContexto, garantia => AbrirDialogoGarantia(garantia.Origem));
    }

    private IReadOnlyList<GarantiaLinha> ConstruirLinhas(IReadOnlyList<Garantia> garantias)
    {
        var hoje = Hoje();
        var nomeBem = NomesBens(_contexto);
        var vencidas = garantias.Count(g => g.StatusEm(hoje) == StatusGarantia.Vencida);
        var proximas = garantias.Count(g => g.StatusEm(hoje) == StatusGarantia.PertoDoVencimento);
        var vigentes = garantias.Count(g => g.StatusEm(hoje) is StatusGarantia.Ativa or StatusGarantia.PertoDoVencimento);
        _resumoEsquerda.Text = $"{garantias.Count} garantias · {vencidas} vencida{(vencidas == 1 ? "" : "s")} · {proximas} vence{(proximas == 1 ? "" : "m")} em até 30 dias";
        _resumoDireita.Text = $"{vigentes} vigente{(vigentes == 1 ? "" : "s")} hoje";

        return garantias.Select(g =>
        {
            var status = g.StatusEm(hoje);
            return new GarantiaLinha(
                g.Id,
                nomeBem(g.BemId),
                Formatador.TextoEnum(g.Tipo),
                g.Fornecedor,
                Formatador.Data(g.Periodo.IniciaEm),
                Formatador.Data(g.Periodo.TerminaEm),
                Formatador.TextoEnum(status),
                Formatador.ClasseEtiqueta(status),
                ClasseLinha(status),
                Formatador.TextoOuTraco(g.ContatoSuporte),
                g);
        }).ToList();
    }

    private static string ClasseLinha(StatusGarantia status) => status switch
    {
        StatusGarantia.Ativa => "linha-ok",
        StatusGarantia.PertoDoVencimento or StatusGarantia.NaoIniciada => "linha-aviso",
        StatusGarantia.Vencida => "linha-perigo",
        _ => "linha-neutra"
    };

    private static string Detalhes(GarantiaLinha linha)
    {
        var garantia = linha.Origem;
        var dias = garantia.Periodo.TerminaEm.DayNumber - Hoje().DayNumber;
        var prazo = dias switch
        {
            < 0 => $"venceu há {-dias} dia(s)",
            0 => "vence hoje",
            _ => $"faltam {dias} dia(s)"
        };
        return $"""
            Bem: {linha.Bem}
            Status: {linha.Status} ({prazo})
            Tipo: {Formatador.TextoEnum(garantia.Tipo)}
            Fornecedor: {Formatador.TextoOuTraco(garantia.Fornecedor)}
            Início: {Formatador.Data(garantia.Periodo.IniciaEm)}
            Fim: {Formatador.Data(garantia.Periodo.TerminaEm)}
            Suporte: {Formatador.TextoOuTraco(garantia.ContatoSuporte)}
            """;
    }

    private ContextMenu MenuContexto(GarantiaLinha linha)
    {
        var menu = new ContextMenu();
        menu.Items.Add(ItemMenu("Editar", () => AbrirDialogoGarantia(linha.Origem)));
        menu.Items.Add(ItemMenu("Detalhes", () => MostrarInformacao("Garantia", Detalhes(linha))));
        menu.Items.Add(new Separator());
        menu.Items.Add(ItemMenu("Excluir…", () => ExcluirGarantia(linha)));
        return menu;
    }

    private static MenuItem ItemMenu(string texto, Action acao)
    {
        var item = new MenuItem { Header = texto };
        item.Click += (_, _) => ExecutarComTratamento(acao);
        return item;
    }

    private void AbrirDialogoGarantia(Garantia? garantia)
    {
        var bemSelecionado = garantia is null ? null : _contexto.BuscarBem().Executar(garantia.BemId);
        var bem = ComboBens(_contexto, bemSelecionado);
        bem.IsEnabled = garantia is null;
        var tipo = Combo(garantia?.Tipo ?? TipoGarantia.Fabricante);
        var fornecedor = CampoTexto("Fornecedor", garantia?.Fornecedor ?? string.Empty);
        var iniciaEm = DataPicker(garantia?.Periodo.IniciaEm ?? Hoje());
        var terminaEm = DataPicker(garantia?.Periodo.TerminaEm ?? Hoje().AddYears(1));
        var suporte = CampoTexto("Telefone, e-mail ou site do suporte", garantia?.ContatoSuporte ?? string.Empty);
        var grade = GradeFormulario();
        AdicionarCampo(grade, 0, 0, "Bem *", bem);
        AdicionarCampo(grade, 0, 1, "Tipo *", tipo);
        AdicionarCampo(grade, 1, 0, "Fornecedor", fornecedor);
        AdicionarCampo(grade, 1, 1, "Contato de suporte", suporte);
        AdicionarCampo(grade, 2, 0, "Início *", iniciaEm);
        AdicionarCampo(grade, 2, 1, "Fim *", terminaEm);

        var salvou = MostrarFormulario(garantia is null ? "Nova garantia" : "Editar garantia", grade, () =>
        {
            var inicio = DataObrigatoria(iniciaEm, "início");
            var fim = DataObrigatoria(terminaEm, "fim");
            if (garantia is null)
            {
                _contexto.RegistrarGarantia().Executar(new RegistrarGarantiaComando(
                    ExigirBemSelecionado(bem).Id, ValorEnum<TipoGarantia>(tipo), fornecedor.Text, inicio, fim, suporte.Text));
            }
            else
            {
                _contexto.EditarGarantia().Executar(new EditarGarantiaComando(garantia.Id, ValorEnum<TipoGarantia>(tipo), fornecedor.Text, inicio, fim, suporte.Text));
            }
        });

        if (salvou)
        {
            _contexto.NotificarAlteracao();
        }
    }

    private void ExcluirGarantia(GarantiaLinha linha)
    {
        if (!MostrarConfirmacao("Excluir garantia",
                $"A garantia de \"{linha.Bem}\" ({linha.Tipo}, até {linha.Fim}) será excluída permanentemente. Deseja continuar?",
                "Excluir"))
        {
            return;
        }

        _contexto.ExcluirGarantia().Executar(linha.Id);
        _contexto.NotificarAlteracao();
    }
}
