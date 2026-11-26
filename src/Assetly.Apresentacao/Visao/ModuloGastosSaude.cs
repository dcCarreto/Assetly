using System.Windows;
using System.Windows.Controls;
using Assetly.Aplicacao.Dados;
using Assetly.Dominio.Saude;
using static Assetly.Apresentacao.Visao.AcoesApoio;
using static Assetly.Apresentacao.Visao.ContextoApoio;
using static Assetly.Apresentacao.Visao.DialogoApoio;
using static Assetly.Apresentacao.Visao.LayoutApoio;

namespace Assetly.Apresentacao.Visao;

internal sealed class ModuloGastosSaude : IModulo
{
    private readonly ContextoAplicacao _contexto;
    private readonly DataGrid _tabela = new();
    private readonly FiltroTabela<GastoSaudeLinha> _filtro;
    private readonly TextBlock _resumoEsquerda = new();
    private readonly TextBlock _resumoDireita = new();
    private int _contador;

    public string Titulo => "Gastos de saúde";
    public string Descricao => "Farmácia e despesas";
    public bool ComContador => true;
    public UIElement Conteudo { get; }
    public int Contador => _contador;

    internal ModuloGastosSaude(ContextoAplicacao contexto)
    {
        _contexto = contexto;
        ConfigurarTabela();

        var (criacao, busca) = CabecalhoBuscaCriacao("Buscar por titular, tipo, descrição ou prestador", "Novo gasto", () => AbrirDialogoGastoSaude(null));
        var direita = UiApoio.Linha(10, BotaoSecundario("Relatório para o IR…", () => RelatorioIrpfApoio.Gerar(_contexto)), criacao);

        _filtro = new FiltroTabela<GastoSaudeLinha>(_tabela, l => l.Id, l => $"{l.Bem} {l.Tipo} {l.Descricao} {l.Origem.Prestador} {l.Origem.DocumentoPrestador}",
        [
            new("Todos", _ => true),
            new("Este ano", l => l.Origem.Data.Year == Hoje().Year),
            new("Farmácia", l => l.Origem.Tipo == TipoGastoSaude.Farmacia),
            new("Consultas", l => l.Origem.Tipo == TipoGastoSaude.Consulta),
            new("Exames", l => l.Origem.Tipo == TipoGastoSaude.Exame),
            new("Procedimentos", l => l.Origem.Tipo == TipoGastoSaude.Procedimento)
        ]) { TextoSemRegistros = "Nenhum gasto de saúde. Registre consultas e exames com o CPF/CNPJ do prestador — eles entram no relatório para o Imposto de Renda." };
        _filtro.ConectarBusca(busca);

        var contextual = BarraContextual(_tabela,
        [
            ("Editar", () => AbrirDialogoGastoSaude(SelecionadoAtual().Origem))
        ],
        [
            ("Excluir", () => ExcluirGasto(SelecionadoAtual()))
        ]);

        Conteudo = TelaComTabela("Gastos de saúde", "Farmácia, consultas, exames e demais despesas.", direita,
            _filtro.Barra, _tabela, contextual, BarraResumoTabela(_resumoEsquerda, _resumoDireita));

        ConfigurarAtalhosTabela(Conteudo, _tabela, busca, () => AbrirDialogoGastoSaude(null),
            () => AbrirDialogoGastoSaude(SelecionadoAtual().Origem), () => ExcluirGasto(SelecionadoAtual()));
    }

    public bool Localizar(object chave) => _filtro.Localizar(chave);

    public void Atualizar()
    {
        var gastos = _contexto.ListarGastosSaude().Executar();
        _contador = gastos.Count;
        _filtro.AtualizarDados(ConstruirLinhas(gastos));
    }

    private GastoSaudeLinha SelecionadoAtual() => Selecionado<GastoSaudeLinha>(_tabela, "Selecione um gasto.");

    private void ConfigurarTabela()
    {
        TabelaApoio.AplicarEstiloBase(_tabela);
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Titular", nameof(GastoSaudeLinha.Bem), Estrela(1.3), estiloBase: "CelulaDestaqueStyle"));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Tipo", nameof(GastoSaudeLinha.Tipo), new DataGridLength(140)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Descrição", nameof(GastoSaudeLinha.Descricao), Estrela(2.2), estiloBase: "CelulaSecundariaStyle"));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Data", nameof(GastoSaudeLinha.Data), new DataGridLength(104), nameof(GastoSaudeLinha.DataOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Valor", nameof(GastoSaudeLinha.Valor), new DataGridLength(132), nameof(GastoSaudeLinha.ValorOrdenacao)));
        TabelaApoio.ConfigurarLinhas<GastoSaudeLinha>(_tabela, _ => "linha-neutra", MenuContexto, gasto => AbrirDialogoGastoSaude(gasto.Origem));
    }

    private IReadOnlyList<GastoSaudeLinha> ConstruirLinhas(IReadOnlyList<GastoSaude> gastos)
    {
        var nomeBem = NomesBens(_contexto);
        var anoAtual = Hoje().Year;
        var doAno = gastos.Where(g => g.Data.Year == anoAtual).ToList();
        _resumoEsquerda.Text = $"{gastos.Count} gastos · {doAno.Count} em {anoAtual} ({Formatador.Total(doAno.Select(g => (Dominio.Compartilhado.Dinheiro?)g.Valor))})";
        _resumoDireita.Text = $"Total geral {Formatador.Total(gastos.Select(g => (Dominio.Compartilhado.Dinheiro?)g.Valor))}";

        return gastos.Select(g => new GastoSaudeLinha(
            g.Id,
            nomeBem(g.BemId),
            Formatador.TextoEnum(g.Tipo),
            g.Descricao,
            Formatador.Data(g.Data),
            Formatador.Dinheiro(g.Valor),
            g)).ToList();
    }

    private ContextMenu MenuContexto(GastoSaudeLinha linha)
    {
        var menu = new ContextMenu();
        var editar = new MenuItem { Header = "Editar" };
        editar.Click += (_, _) => ExecutarComTratamento(() => AbrirDialogoGastoSaude(linha.Origem));
        var excluir = new MenuItem { Header = "Excluir…" };
        excluir.Click += (_, _) => ExecutarComTratamento(() => ExcluirGasto(linha));
        menu.Items.Add(editar);
        menu.Items.Add(new Separator());
        menu.Items.Add(excluir);
        return menu;
    }

    private void AbrirDialogoGastoSaude(GastoSaude? gasto)
    {
        var bemSelecionado = gasto is null ? null : _contexto.BuscarBem().Executar(gasto.BemId);
        var bem = ComboBens(_contexto, bemSelecionado);
        bem.IsEnabled = gasto is null;
        var tipo = Combo(gasto?.Tipo ?? TipoGastoSaude.Farmacia);
        var descricao = CampoTexto("Descrição", gasto?.Descricao ?? string.Empty);
        var data = DataPicker(gasto?.Data ?? Hoje());
        var valor = CampoTexto("Valor (ex.: 89,90)", TextoValor(gasto?.Valor.Valor));
        var moeda = ComboMoeda(gasto?.Valor.CodigoMoeda);
        var prestador = CampoTexto("Médico, clínica, laboratório...", gasto?.Prestador ?? string.Empty);
        var documentoPrestador = CampoTexto("CPF ou CNPJ de quem recebeu (está no recibo ou na nota)",
            gasto is null || gasto.DocumentoPrestador.Length == 0 ? string.Empty : Dominio.Compartilhado.CpfCnpj.Formatar(gasto.DocumentoPrestador));
        var grade = GradeFormulario();
        AdicionarCampo(grade, 0, 0, "Titular *", bem);
        AdicionarCampo(grade, 0, 1, "Tipo *", tipo);
        AdicionarCampo(grade, 1, 0, "Descrição *", descricao, ocupaLinhaToda: true);
        AdicionarCampo(grade, 2, 0, "Data *", data);
        AdicionarCampo(grade, 2, 1, "Valor *", valor);
        AdicionarCampo(grade, 3, 0, "Moeda *", moeda);
        AdicionarSecao(grade, 4, "Para o Imposto de Renda (consultas, exames e procedimentos)");
        AdicionarCampo(grade, 5, 0, "Prestador", prestador);
        AdicionarCampo(grade, 5, 1, "CPF/CNPJ do prestador", documentoPrestador);

        var salvou = MostrarFormulario(gasto is null ? "Novo gasto" : "Editar gasto", grade, () =>
        {
            var dataValor = DataObrigatoria(data, "do gasto");
            var valorDecimal = DecimalOpcional(valor.Text) ?? throw new ArgumentException("valor do gasto é obrigatório");
            var codigoMoeda = MoedaEscolhida(moeda);
            if (gasto is null)
            {
                _contexto.RegistrarGastoSaude().Executar(new RegistrarGastoSaudeComando(
                    ExigirBemSelecionado(bem).Id, ValorEnum<TipoGastoSaude>(tipo), descricao.Text, dataValor, valorDecimal, codigoMoeda,
                    prestador.Text, documentoPrestador.Text));
            }
            else
            {
                _contexto.EditarGastoSaude().Executar(new EditarGastoSaudeComando(
                    gasto.Id, ValorEnum<TipoGastoSaude>(tipo), descricao.Text, dataValor, valorDecimal, codigoMoeda,
                    prestador.Text, documentoPrestador.Text));
            }
        });

        if (salvou)
        {
            _contexto.NotificarAlteracao();
        }
    }

    private void ExcluirGasto(GastoSaudeLinha linha)
    {
        if (!MostrarConfirmacao("Excluir gasto",
                $"O gasto \"{linha.Descricao}\" ({linha.Valor}, {linha.Data}) será excluído permanentemente. Deseja continuar?",
                "Excluir"))
        {
            return;
        }

        _contexto.ExcluirGastoSaude().Executar(linha.Id);
        _contexto.NotificarAlteracao();
    }
}
