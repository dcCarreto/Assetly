using System.Windows;
using System.Windows.Controls;
using Assetly.Aplicacao.Dados;
using Assetly.Dominio.Alerta;
using static Assetly.Apresentacao.Visao.AcoesApoio;
using static Assetly.Apresentacao.Visao.ContextoApoio;
using static Assetly.Apresentacao.Visao.DialogoApoio;
using static Assetly.Apresentacao.Visao.LayoutApoio;

namespace Assetly.Apresentacao.Visao;

/// <summary>Lista de alertas. A geração/resolução automática acontece uma vez por ciclo na
/// <see cref="TelaPrincipal"/>, antes de todos os módulos — este módulo só exibe e permite agir.</summary>
internal sealed class ModuloAlertas : IModulo
{
    private readonly ContextoAplicacao _contexto;
    private DataGridColumn _colunaStatus = null!;
    private readonly DataGrid _tabela = new();
    private readonly FiltroTabela<AlertaLinha> _filtro;
    private readonly TextBlock _resumoEsquerda = new();
    private readonly TextBlock _resumoDireita = new();
    private int _contador;

    public string Titulo => "Alertas";
    public string Descricao => "Pendências abertas";
    public bool ComContador => true;
    public UIElement Conteudo { get; }
    public int Contador => _contador;

    internal ModuloAlertas(ContextoAplicacao contexto)
    {
        _contexto = contexto;
        ConfigurarTabela();

        var (direita, busca) = CabecalhoBuscaCriacao("Buscar por bem, tipo ou mensagem", "Novo alerta", AbrirDialogoAlerta);

        _filtro = new FiltroTabela<AlertaLinha>(_tabela, l => l.Id, l => $"{l.Bem} {l.Tipo} {l.Mensagem}",
        [
            new("Pendentes", l => l.Origem.EstaAcionavel()),
            new("Abertos", l => l.Origem.Status == StatusAlerta.Aberto),
            new("Ciente", l => l.Origem.Status == StatusAlerta.Ciente),
            new("Resolvidos", l => l.Origem.Status == StatusAlerta.Resolvido),
            new("Todos", _ => true)
        ]) { TextoSemRegistros = "Nenhum alerta. Os automáticos aparecem sozinhos quando uma garantia, conta ou documento pede atenção." };
        _filtro.ConectarBusca(busca);
        // "Status" só aparece quando as linhas visíveis misturam status (em "Pendentes" eram todas "Aberto").
        _filtro.Aplicado += linhas => _colunaStatus.Visibility = linhas.Select(l => l.Origem.Status).Distinct().Count() > 1 ? Visibility.Visible : Visibility.Collapsed;

        var contextual = BarraContextual(_tabela,
        [
            ("Detalhes", () => MostrarInformacao("Alerta", Detalhes(SelecionadoAtual()))),
            ("Ciente", () => TomarCienciaAlerta(SelecionadoAtual().Origem)),
            ("Resolver", () => ResolverAlerta(SelecionadoAtual().Origem)),
            ("Reabrir", () => ReabrirAlerta(SelecionadoAtual().Origem))
        ],
        [
            ("Excluir", () => ExcluirAlerta(SelecionadoAtual()))
        ]);

        Conteudo = TelaComTabela("Alertas", "Pendências abertas e eventos que exigem acompanhamento.", direita,
            _filtro.Barra, _tabela, contextual, BarraResumoTabela(_resumoEsquerda, _resumoDireita));

        ConfigurarAtalhosTabela(Conteudo, _tabela, busca, AbrirDialogoAlerta, null, () => ExcluirAlerta(SelecionadoAtual()));
    }

    public bool Localizar(object chave) => _filtro.Localizar(chave);

    public void Atualizar()
    {
        var alertas = _contexto.ListarAlertas().Executar();
        _contador = alertas.Count(a => a.EstaAcionavel());
        _filtro.AtualizarDados(ConstruirLinhas(alertas));
    }

    private AlertaLinha SelecionadoAtual() => Selecionado<AlertaLinha>(_tabela, "Selecione um alerta.");

    private void ConfigurarTabela()
    {
        TabelaApoio.AplicarEstiloBase(_tabela);
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Bem", nameof(AlertaLinha.Bem), Estrela(1.3), estiloBase: "CelulaDestaqueStyle"));
        _tabela.Columns.Add(TabelaApoio.ColunaEtiqueta("Severidade", nameof(AlertaLinha.Severidade), nameof(AlertaLinha.SeveridadeClasse), new DataGridLength(118), nameof(AlertaLinha.SeveridadeOrdenacao)));
        _colunaStatus = TabelaApoio.ColunaEtiqueta("Status", nameof(AlertaLinha.Status), nameof(AlertaLinha.StatusClasse), new DataGridLength(108));
        _tabela.Columns.Add(_colunaStatus);
        // Sem coluna "Tipo": a mensagem já começa por ele ("Garantia vencida em…"); o tipo continua na busca e nos detalhes.
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Mensagem", nameof(AlertaLinha.Mensagem), Estrela(2.4), estiloBase: "CelulaSecundariaStyle"));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Criado", nameof(AlertaLinha.Criado), new DataGridLength(104), nameof(AlertaLinha.CriadoOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Prazo", nameof(AlertaLinha.Prazo), new DataGridLength(104), nameof(AlertaLinha.PrazoOrdenacao)));
        TabelaApoio.ConfigurarLinhas<AlertaLinha>(_tabela, l => l.LinhaClasse, MenuContexto, l => MostrarInformacao("Alerta", Detalhes(l)));
    }

    private IReadOnlyList<AlertaLinha> ConstruirLinhas(IReadOnlyList<Alerta> alertas)
    {
        var nomeBem = NomesBens(_contexto);
        var pendentes = alertas.Where(a => a.EstaAcionavel()).ToList();
        var criticos = pendentes.Count(a => a.Severidade == SeveridadeAlerta.Critico);
        var avisos = pendentes.Count(a => a.Severidade == SeveridadeAlerta.Aviso);
        _resumoEsquerda.Text = $"{pendentes.Count} alerta{(pendentes.Count == 1 ? "" : "s")} pendente{(pendentes.Count == 1 ? "" : "s")} · {criticos} crítico{(criticos == 1 ? "" : "s")} · {avisos} aviso{(avisos == 1 ? "" : "s")}";
        _resumoDireita.Text = "Alertas automáticos se resolvem sozinhos quando a pendência é corrigida";

        return alertas
            .OrderBy(a => a.EstaAcionavel() ? 0 : 1)
            .ThenBy(a => a.Severidade switch { SeveridadeAlerta.Critico => 0, SeveridadeAlerta.Aviso => 1, _ => 2 })
            .ThenBy(a => a.PrazoEm ?? DateOnly.MaxValue)
            .Select(a => new AlertaLinha(
                a.Id,
                nomeBem(a.BemId),
                Formatador.TextoEnum(a.Tipo),
                Formatador.TextoEnum(a.Severidade),
                Formatador.ClasseEtiqueta(a.Severidade),
                a.Mensagem,
                Formatador.Data(a.CriadoEm),
                Formatador.Data(a.PrazoEm),
                Formatador.TextoEnum(a.Status),
                Formatador.ClasseEtiqueta(a.Status),
                ClasseLinha(a),
                a))
            .ToList();
    }

    private static string ClasseLinha(Alerta alerta)
    {
        if (alerta.Status == StatusAlerta.Resolvido)
        {
            return "linha-neutra";
        }

        // Só o crítico pinta a linha: com todas tingidas (vermelho e laranja), nada se destacava. O selo de severidade
        // continua mostrando aviso e informativo.
        return alerta.Severidade == SeveridadeAlerta.Critico ? "linha-perigo" : "linha-neutra";
    }

    private static string Detalhes(AlertaLinha linha)
    {
        var alerta = linha.Origem;
        return $"""
            Bem: {linha.Bem}
            Tipo: {Formatador.TextoEnum(alerta.Tipo)}
            Severidade: {Formatador.TextoEnum(alerta.Severidade)}
            Status: {Formatador.TextoEnum(alerta.Status)}
            Origem: {(alerta.Automatico ? "gerado automaticamente" : "criado manualmente")}
            Mensagem: {alerta.Mensagem}
            Criado em: {Formatador.Data(alerta.CriadoEm)}
            Prazo: {Formatador.Data(alerta.PrazoEm)}
            """;
    }

    private ContextMenu MenuContexto(AlertaLinha linha)
    {
        var alerta = linha.Origem;
        var menu = new ContextMenu();
        menu.Items.Add(ItemMenu("Detalhes", () => MostrarInformacao("Alerta", Detalhes(linha))));
        menu.Items.Add(new Separator());
        menu.Items.Add(ItemMenu("Marcar como ciente", () => TomarCienciaAlerta(alerta), alerta.Status == StatusAlerta.Aberto));
        menu.Items.Add(ItemMenu("Resolver", () => ResolverAlerta(alerta), alerta.EstaAcionavel()));
        menu.Items.Add(ItemMenu("Reabrir", () => ReabrirAlerta(alerta), alerta.Status != StatusAlerta.Aberto));
        menu.Items.Add(new Separator());
        menu.Items.Add(ItemMenu("Excluir…", () => ExcluirAlerta(linha)));
        return menu;
    }

    private static MenuItem ItemMenu(string texto, Action acao, bool habilitado = true)
    {
        var item = new MenuItem { Header = texto, IsEnabled = habilitado };
        item.Click += (_, _) => ExecutarComTratamento(acao);
        return item;
    }

    private void AbrirDialogoAlerta()
    {
        var bem = ComboBens(_contexto, null);
        var tipo = Combo(TipoAlerta.ManutencaoProxima);
        var severidade = Combo(SeveridadeAlerta.Aviso);
        var mensagem = CampoTexto("Mensagem", string.Empty);
        var criadoEm = DataPicker(Hoje());
        var prazoEm = DataPicker(null);
        var grade = GradeFormulario();
        AdicionarCampo(grade, 0, 0, "Bem *", bem);
        AdicionarCampo(grade, 0, 1, "Tipo *", tipo);
        AdicionarCampo(grade, 1, 0, "Severidade *", severidade);
        AdicionarCampo(grade, 1, 1, "Criado em *", criadoEm);
        AdicionarCampo(grade, 2, 0, "Prazo", prazoEm);
        AdicionarCampo(grade, 3, 0, "Mensagem *", mensagem, ocupaLinhaToda: true);

        var salvou = MostrarFormulario("Novo alerta", grade, () => _contexto.CriarAlerta().Executar(new CriarAlertaComando(
            ExigirBemSelecionado(bem).Id, ValorEnum<TipoAlerta>(tipo), ValorEnum<SeveridadeAlerta>(severidade), mensagem.Text,
            DataObrigatoria(criadoEm, "criação"), ValorData(prazoEm))));

        if (salvou)
        {
            _contexto.NotificarAlteracao();
        }
    }

    private void TomarCienciaAlerta(Alerta alerta)
    {
        _contexto.TomarCienciaAlerta().Executar(alerta.Id);
        _contexto.NotificarAlteracao();
    }

    private void ResolverAlerta(Alerta alerta)
    {
        _contexto.ResolverAlerta().Executar(alerta.Id);
        _contexto.NotificarAlteracao();
    }

    private void ReabrirAlerta(Alerta alerta)
    {
        _contexto.ReabrirAlerta().Executar(alerta.Id);
        _contexto.NotificarAlteracao();
    }

    private void ExcluirAlerta(AlertaLinha linha)
    {
        var aviso = linha.Origem.Automatico && linha.Origem.EstaAcionavel()
            ? "\n\nEste alerta é automático: se a pendência continuar existindo, ele será criado de novo. Para dispensá-lo, use Resolver."
            : string.Empty;
        if (!MostrarConfirmacao("Excluir alerta", $"O alerta \"{linha.Mensagem}\" será excluído permanentemente.{aviso}", "Excluir"))
        {
            return;
        }

        _contexto.ExcluirAlerta().Executar(linha.Id);
        _contexto.NotificarAlteracao();
    }
}
