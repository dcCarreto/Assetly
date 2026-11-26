using System.Windows;
using System.Windows.Controls;
using Assetly.Aplicacao.Dados;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Manutencao;
using static Assetly.Apresentacao.Visao.AcoesApoio;
using static Assetly.Apresentacao.Visao.ContextoApoio;
using static Assetly.Apresentacao.Visao.DialogoApoio;
using static Assetly.Apresentacao.Visao.LayoutApoio;

namespace Assetly.Apresentacao.Visao;

internal sealed class ModuloManutencoes : IModulo
{
    private readonly ContextoAplicacao _contexto;
    private readonly DataGrid _tabela = new();
    private readonly FiltroTabela<ManutencaoLinha> _filtro;
    private readonly TextBlock _resumoEsquerda = new();
    private readonly TextBlock _resumoDireita = new();
    private int _contador;

    public string Titulo => "Manutenções";
    public string Descricao => "Agenda e custos";
    public bool ComContador => true;
    public UIElement Conteudo { get; }
    public int Contador => _contador;

    internal ModuloManutencoes(ContextoAplicacao contexto)
    {
        _contexto = contexto;
        ConfigurarTabela();

        var (direita, busca) = CabecalhoBuscaCriacao("Buscar por bem, tipo ou descrição", "Nova manutenção", () => AbrirDialogoManutencao(null));

        _filtro = new FiltroTabela<ManutencaoLinha>(_tabela, l => l.Id, l => $"{l.Bem} {l.Tipo} {l.Descricao}",
        [
            new("Todas", _ => true),
            new("Atrasadas", l => l.StatusClasse == "etiqueta-atrasada"),
            new("Agendadas", l => l.StatusClasse == "etiqueta-agendada"),
            new("Concluídas", l => l.StatusClasse == "etiqueta-concluida"),
            new("Canceladas", l => l.StatusClasse == "etiqueta-cancelada")
        ]);
        _filtro.ConectarBusca(busca);

        var contextual = BarraContextual(_tabela,
        [
            ("Detalhes", () => MostrarInformacao("Manutenção", Detalhes(SelecionadaAtual()))),
            ("Editar", () => AbrirDialogoManutencao(SelecionadaAtual().Origem)),
            ("Concluir", () => ConcluirManutencao(SelecionadaAtual().Origem)),
            ("Anexar comprovante", () => AnexarComprovante(SelecionadaAtual().Origem)),
            ("Ver comprovante", () => ComprovanteApoio.Abrir(_contexto, Vinculo(SelecionadaAtual().Origem)))
        ],
        [
            ("Cancelar", () => CancelarManutencao(SelecionadaAtual().Origem)),
            ("Excluir", () => ExcluirManutencao(SelecionadaAtual()))
        ]);

        Conteudo = TelaComTabela("Manutenções", "Agenda preventiva, corretiva, custos e conclusões.", direita,
            _filtro.Barra, _tabela, contextual, BarraResumoTabela(_resumoEsquerda, _resumoDireita));

        ConfigurarAtalhosTabela(Conteudo, _tabela, busca, () => AbrirDialogoManutencao(null),
            () => AbrirDialogoManutencao(SelecionadaAtual().Origem), () => ExcluirManutencao(SelecionadaAtual()));
    }

    public bool Localizar(object chave) => _filtro.Localizar(chave);

    public void Atualizar()
    {
        var manutencoes = _contexto.ListarManutencoes().Executar();
        _contador = manutencoes.Count;
        _filtro.AtualizarDados(ConstruirLinhas(manutencoes));
    }

    private ManutencaoLinha SelecionadaAtual() => Selecionado<ManutencaoLinha>(_tabela, "Selecione uma manutenção.");

    private void ConfigurarTabela()
    {
        TabelaApoio.AplicarEstiloBase(_tabela);
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Bem", nameof(ManutencaoLinha.Bem), Estrela(1.5), estiloBase: "CelulaDestaqueStyle"));
        _tabela.Columns.Add(TabelaApoio.ColunaEtiqueta("Status", nameof(ManutencaoLinha.Status), nameof(ManutencaoLinha.StatusClasse), new DataGridLength(130)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Tipo", nameof(ManutencaoLinha.Tipo), new DataGridLength(120)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Descrição", nameof(ManutencaoLinha.Descricao), Estrela(2.2), estiloBase: "CelulaSecundariaStyle"));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Agendada", nameof(ManutencaoLinha.Agendada), new DataGridLength(104), nameof(ManutencaoLinha.AgendadaOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Concluída", nameof(ManutencaoLinha.Concluida), new DataGridLength(104), nameof(ManutencaoLinha.ConcluidaOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Custo", nameof(ManutencaoLinha.Custo), new DataGridLength(116), nameof(ManutencaoLinha.CustoOrdenacao)));
        TabelaApoio.ConfigurarLinhas<ManutencaoLinha>(_tabela, l => l.LinhaClasse, MenuContexto, manutencao => AbrirDialogoManutencao(manutencao.Origem));
    }

    private IReadOnlyList<ManutencaoLinha> ConstruirLinhas(IReadOnlyList<Manutencao> manutencoes)
    {
        var hoje = Hoje();
        var nomeBem = NomesBens(_contexto);
        var atrasadas = manutencoes.Count(m => m.StatusEm(hoje) == StatusManutencao.Atrasada);
        var agendadas = manutencoes.Count(m => m.StatusEm(hoje) == StatusManutencao.Agendada);
        _resumoEsquerda.Text = $"{manutencoes.Count} manutenções · {atrasadas} atrasada{(atrasadas == 1 ? "" : "s")} · {agendadas} agendada{(agendadas == 1 ? "" : "s")}";
        _resumoDireita.Text = $"Custo acumulado {Formatador.Total(manutencoes.Select(m => m.Custo))}";

        var comComprovante = ComprovanteApoio.ComComprovante(_contexto, TipoVinculoDocumento.Manutencao);
        return manutencoes.Select(m =>
        {
            var status = m.StatusEm(hoje);
            return new ManutencaoLinha(
                m.Id,
                nomeBem(m.BemId),
                Formatador.TextoEnum(m.Tipo),
                comComprovante.Contains(m.Id.Valor) ? m.Descricao + ComprovanteApoio.Marca : m.Descricao,
                Formatador.Data(m.AgendadaPara),
                Formatador.Data(m.ConcluidaEm),
                Formatador.Dinheiro(m.Custo),
                Formatador.TextoEnum(status),
                Formatador.ClasseEtiqueta(status),
                ClasseLinha(status),
                m);
        }).ToList();
    }

    private static string ClasseLinha(StatusManutencao status) => status switch
    {
        StatusManutencao.Concluida => "linha-ok",
        StatusManutencao.Agendada => "linha-informativa",
        StatusManutencao.Atrasada => "linha-perigo",
        StatusManutencao.Cancelada => "linha-neutra",
        _ => "linha-neutra"
    };

    private static bool EstaAberta(Manutencao manutencao) =>
        manutencao.Status is not (StatusManutencao.Concluida or StatusManutencao.Cancelada);

    private static string Detalhes(ManutencaoLinha linha)
    {
        var manutencao = linha.Origem;
        return $"""
            Bem: {linha.Bem}
            Status: {linha.Status}
            Tipo: {Formatador.TextoEnum(manutencao.Tipo)}
            Descrição: {manutencao.Descricao}
            Agendada para: {Formatador.Data(manutencao.AgendadaPara)}
            Concluída em: {Formatador.Data(manutencao.ConcluidaEm)}
            Custo: {Formatador.Dinheiro(manutencao.Custo)}
            """;
    }

    private ContextMenu MenuContexto(ManutencaoLinha linha)
    {
        var aberta = EstaAberta(linha.Origem);
        var menu = new ContextMenu();
        menu.Items.Add(ItemMenu("Editar", () => AbrirDialogoManutencao(linha.Origem)));
        menu.Items.Add(ItemMenu("Detalhes", () => MostrarInformacao("Manutenção", Detalhes(linha))));
        menu.Items.Add(ItemMenu("Concluir", () => ConcluirManutencao(linha.Origem), aberta));
        menu.Items.Add(ItemMenu("Anexar comprovante…", () => AnexarComprovante(linha.Origem)));
        menu.Items.Add(ItemMenu("Ver comprovante", () => ComprovanteApoio.Abrir(_contexto, Vinculo(linha.Origem))));
        menu.Items.Add(new Separator());
        menu.Items.Add(ItemMenu("Cancelar", () => CancelarManutencao(linha.Origem), aberta));
        menu.Items.Add(ItemMenu("Excluir…", () => ExcluirManutencao(linha)));
        return menu;
    }

    private static MenuItem ItemMenu(string texto, Action acao, bool habilitado = true)
    {
        var item = new MenuItem { Header = texto, IsEnabled = habilitado };
        item.Click += (_, _) => ExecutarComTratamento(acao);
        return item;
    }

    private void AbrirDialogoManutencao(Manutencao? manutencao)
    {
        var bemSelecionado = manutencao is null ? null : _contexto.BuscarBem().Executar(manutencao.BemId);
        var bem = ComboBens(_contexto, bemSelecionado);
        bem.IsEnabled = manutencao is null;
        var tipo = Combo(manutencao?.Tipo ?? TipoManutencao.Preventiva);
        var descricao = CampoTexto("Descrição", manutencao?.Descricao ?? string.Empty);
        var data = DataPicker(manutencao?.AgendadaPara ?? Hoje());
        // Manutenção encerrada (concluída/cancelada): descrição e tipo podem ser corrigidos, a data não.
        data.IsEnabled = manutencao is null || EstaAberta(manutencao);
        var grade = GradeFormulario();
        AdicionarCampo(grade, 0, 0, "Bem *", bem);
        AdicionarCampo(grade, 0, 1, "Tipo *", tipo);
        AdicionarCampo(grade, 1, 0, "Descrição *", descricao, ocupaLinhaToda: true);
        AdicionarCampo(grade, 2, 0, "Agendada para *", data);

        var salvou = MostrarFormulario(manutencao is null ? "Nova manutenção" : "Editar manutenção", grade, () =>
        {
            var agendadaPara = DataObrigatoria(data, "agendamento");
            if (manutencao is null)
            {
                _contexto.AgendarManutencao().Executar(new AgendarManutencaoComando(ExigirBemSelecionado(bem).Id, ValorEnum<TipoManutencao>(tipo), descricao.Text, agendadaPara));
            }
            else
            {
                _contexto.EditarManutencao().Executar(new EditarManutencaoComando(manutencao.Id, ValorEnum<TipoManutencao>(tipo), descricao.Text, agendadaPara));
            }
        });

        if (salvou)
        {
            _contexto.NotificarAlteracao();
        }
    }

    private void ConcluirManutencao(Manutencao manutencao)
    {
        if (!EstaAberta(manutencao))
        {
            throw new InvalidOperationException("esta manutenção já está encerrada (concluída ou cancelada).");
        }

        var concluidaEm = DataPicker(Hoje());
        var custo = CampoTexto("Custo (ex.: 150,00)", string.Empty);
        var moeda = ComboMoeda("BRL");
        var grade = GradeFormulario();
        AdicionarCampo(grade, 0, 0, "Concluída em *", concluidaEm);
        AdicionarCampo(grade, 0, 1, "Custo", custo);
        AdicionarCampo(grade, 1, 0, "Moeda", moeda);

        if (MostrarFormulario("Concluir manutenção", grade, () => _contexto.ConcluirManutencao().Executar(new ConcluirManutencaoComando(
                manutencao.Id, DataObrigatoria(concluidaEm, "conclusão"), DecimalOpcional(custo.Text), MoedaEscolhida(moeda)))))
        {
            _contexto.NotificarAlteracao();
        }
    }

    private void CancelarManutencao(Manutencao manutencao)
    {
        if (!EstaAberta(manutencao))
        {
            throw new InvalidOperationException("esta manutenção já está encerrada (concluída ou cancelada).");
        }

        if (!MostrarConfirmacao("Cancelar manutenção", "A manutenção selecionada será marcada como cancelada (o registro é mantido no histórico). Deseja continuar?", "Cancelar manutenção"))
        {
            return;
        }

        _contexto.CancelarManutencao().Executar(manutencao.Id);
        _contexto.NotificarAlteracao();
    }

    private static VinculoDocumento Vinculo(Manutencao manutencao) => new(TipoVinculoDocumento.Manutencao, manutencao.Id.Valor);

    private void AnexarComprovante(Manutencao manutencao) =>
        ComprovanteApoio.Anexar(_contexto, manutencao.BemId, Vinculo(manutencao), manutencao.Descricao);

    private void ExcluirManutencao(ManutencaoLinha linha)
    {
        if (!MostrarConfirmacao("Excluir manutenção",
                $"A manutenção \"{linha.Descricao}\" de \"{linha.Bem}\" será excluída permanentemente. Para manter o histórico, prefira Cancelar. Deseja continuar?",
                "Excluir"))
        {
            return;
        }

        _contexto.ExcluirManutencao().Executar(linha.Id);
        _contexto.NotificarAlteracao();
    }
}
