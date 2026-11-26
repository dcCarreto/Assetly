using System.Windows;
using System.Windows.Controls;
using Assetly.Aplicacao.Dados;
using Assetly.Dominio.Saude;
using static Assetly.Apresentacao.Visao.AcoesApoio;
using static Assetly.Apresentacao.Visao.ContextoApoio;
using static Assetly.Apresentacao.Visao.DialogoApoio;
using static Assetly.Apresentacao.Visao.LayoutApoio;

namespace Assetly.Apresentacao.Visao;

internal sealed class ModuloSaude : IModulo
{
    private readonly ContextoAplicacao _contexto;
    private readonly DataGrid _tabela = new();
    private readonly FiltroTabela<SaudeLinha> _filtro;
    private readonly TextBlock _resumoEsquerda = new();
    private readonly TextBlock _resumoDireita = new();
    private int _contador;

    public string Titulo => "Convênios";
    public string Descricao => "Planos de saúde";
    public bool ComContador => true;
    public UIElement Conteudo { get; }
    public int Contador => _contador;

    internal ModuloSaude(ContextoAplicacao contexto)
    {
        _contexto = contexto;
        ConfigurarTabela();

        var (direita, busca) = CabecalhoBuscaCriacao("Buscar por titular, fornecedor ou contato", "Novo convênio", () => AbrirDialogoSaude(null));

        _filtro = new FiltroTabela<SaudeLinha>(_tabela, l => l.Id, l => $"{l.Bem} {l.Fornecedor} {l.Tipo} {l.Contato}",
        [
            new("Todos", _ => true),
            new("Vencidos", l => l.StatusClasse == "etiqueta-vencida"),
            new("Próximos", l => l.StatusClasse == "etiqueta-perto-do-vencimento"),
            new("Ativos", l => l.StatusClasse == "etiqueta-ativa"),
            new("Cancelados", l => l.StatusClasse == "etiqueta-cancelada")
        ]) { TextoSemRegistros = "Nenhum convênio. Cadastre planos de saúde e odontológicos para ser avisado do vencimento." };
        _filtro.ConectarBusca(busca);

        var contextual = BarraContextual(_tabela,
        [
            ("Detalhes", () => MostrarInformacao("Convênio", Detalhes(SelecionadoAtual()))),
            ("Editar", () => AbrirDialogoSaude(SelecionadoAtual().Origem))
        ],
        [
            ("Cancelar", () => CancelarSaude(SelecionadoAtual().Origem)),
            ("Excluir", () => ExcluirSaude(SelecionadoAtual()))
        ]);

        Conteudo = TelaComTabela("Convênios", "Planos de saúde, ortodontia e coberturas vigentes.", direita,
            _filtro.Barra, _tabela, contextual, BarraResumoTabela(_resumoEsquerda, _resumoDireita));

        ConfigurarAtalhosTabela(Conteudo, _tabela, busca, () => AbrirDialogoSaude(null),
            () => AbrirDialogoSaude(SelecionadoAtual().Origem), () => ExcluirSaude(SelecionadoAtual()));
    }

    public bool Localizar(object chave) => _filtro.Localizar(chave);

    public void Atualizar()
    {
        var registros = _contexto.ListarSaude().Executar();
        _contador = registros.Count;
        _filtro.AtualizarDados(ConstruirLinhas(registros));
    }

    private SaudeLinha SelecionadoAtual() => Selecionado<SaudeLinha>(_tabela, "Selecione um convênio.");

    private void ConfigurarTabela()
    {
        TabelaApoio.AplicarEstiloBase(_tabela);
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Titular", nameof(SaudeLinha.Bem), Estrela(1.5), estiloBase: "CelulaDestaqueStyle"));
        _tabela.Columns.Add(TabelaApoio.ColunaEtiqueta("Status", nameof(SaudeLinha.Status), nameof(SaudeLinha.StatusClasse), new DataGridLength(170)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Tipo", nameof(SaudeLinha.Tipo), new DataGridLength(120)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Fornecedor", nameof(SaudeLinha.Fornecedor), Estrela(1.4)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Início", nameof(SaudeLinha.Inicio), new DataGridLength(104), nameof(SaudeLinha.InicioOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Fim", nameof(SaudeLinha.Fim), new DataGridLength(104), nameof(SaudeLinha.FimOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Contato", nameof(SaudeLinha.Contato), new DataGridLength(140), estiloBase: "CelulaSecundariaStyle"));
        TabelaApoio.ConfigurarLinhas<SaudeLinha>(_tabela, l => l.LinhaClasse, MenuContexto, AoDarDuploClique);
    }

    private IReadOnlyList<SaudeLinha> ConstruirLinhas(IReadOnlyList<Saude> registros)
    {
        var hoje = Hoje();
        var nomeBem = NomesBens(_contexto);
        var vencidos = registros.Count(s => s.StatusEm(hoje) == StatusSaude.Vencida);
        var proximos = registros.Count(s => s.StatusEm(hoje) == StatusSaude.PertoDoVencimento);
        var vigentes = registros.Count(s => s.StatusEm(hoje) is StatusSaude.Ativa or StatusSaude.PertoDoVencimento);
        _resumoEsquerda.Text = $"{registros.Count} convênios · {vencidos} vencido{(vencidos == 1 ? "" : "s")} · {proximos} vence{(proximos == 1 ? "" : "m")} em até 30 dias";
        _resumoDireita.Text = $"{vigentes} vigente{(vigentes == 1 ? "" : "s")} hoje";

        return registros.Select(s =>
        {
            var status = s.StatusEm(hoje);
            return new SaudeLinha(
                s.Id,
                nomeBem(s.BemId),
                Formatador.TextoEnum(s.Tipo),
                s.Fornecedor,
                Formatador.Data(s.Periodo.IniciaEm),
                Formatador.Data(s.Periodo.TerminaEm),
                Formatador.TextoEnum(status),
                Formatador.ClasseEtiqueta(status),
                ClasseLinha(status),
                Formatador.TextoOuTraco(s.Contato),
                s);
        }).ToList();
    }

    private static string ClasseLinha(StatusSaude status) => status switch
    {
        StatusSaude.Ativa => "linha-ok",
        StatusSaude.PertoDoVencimento or StatusSaude.NaoIniciada => "linha-aviso",
        StatusSaude.Vencida => "linha-perigo",
        _ => "linha-neutra"
    };

    private static bool Cancelado(Saude saude) => saude.DataCancelamento.HasValue;

    private void AoDarDuploClique(SaudeLinha linha)
    {
        if (Cancelado(linha.Origem))
        {
            MostrarInformacao("Convênio", Detalhes(linha));
        }
        else
        {
            AbrirDialogoSaude(linha.Origem);
        }
    }

    private static string Detalhes(SaudeLinha linha)
    {
        var saude = linha.Origem;
        var cancelamento = saude.DataCancelamento is null ? string.Empty : $"\nCancelado em: {Formatador.Data(saude.DataCancelamento)}";
        return $"""
            Titular: {linha.Bem}
            Status: {linha.Status}
            Tipo: {Formatador.TextoEnum(saude.Tipo)}
            Fornecedor: {Formatador.TextoOuTraco(saude.Fornecedor)}
            Início: {Formatador.Data(saude.Periodo.IniciaEm)}
            Fim: {Formatador.Data(saude.Periodo.TerminaEm)}
            Contato: {Formatador.TextoOuTraco(saude.Contato)}
            """ + cancelamento;
    }

    private ContextMenu MenuContexto(SaudeLinha linha)
    {
        var cancelado = Cancelado(linha.Origem);
        var menu = new ContextMenu();
        menu.Items.Add(ItemMenu("Editar", () => AbrirDialogoSaude(linha.Origem), !cancelado));
        menu.Items.Add(ItemMenu("Detalhes", () => MostrarInformacao("Convênio", Detalhes(linha))));
        menu.Items.Add(new Separator());
        menu.Items.Add(ItemMenu("Cancelar", () => CancelarSaude(linha.Origem), !cancelado));
        menu.Items.Add(ItemMenu("Excluir…", () => ExcluirSaude(linha)));
        return menu;
    }

    private static MenuItem ItemMenu(string texto, Action acao, bool habilitado = true)
    {
        var item = new MenuItem { Header = texto, IsEnabled = habilitado };
        item.Click += (_, _) => ExecutarComTratamento(acao);
        return item;
    }

    private void AbrirDialogoSaude(Saude? saude)
    {
        if (saude is not null && Cancelado(saude))
        {
            throw new InvalidOperationException("convênio cancelado não pode ser editado. Cadastre um novo convênio se a cobertura foi retomada.");
        }

        var bemSelecionado = saude is null ? null : _contexto.BuscarBem().Executar(saude.BemId);
        var bem = ComboBens(_contexto, bemSelecionado);
        bem.IsEnabled = saude is null;
        var tipo = Combo(saude?.Tipo ?? TipoSaude.Convenio);
        var fornecedor = CampoTexto("Operadora ou fornecedor", saude?.Fornecedor ?? string.Empty);
        var contato = CampoTexto("Telefones de atendimento e emergência", saude?.Contato ?? string.Empty);
        var iniciaEm = DataPicker(saude?.Periodo.IniciaEm ?? Hoje());
        var terminaEm = DataPicker(saude?.Periodo.TerminaEm ?? Hoje().AddYears(1));
        var grade = GradeFormulario();
        AdicionarCampo(grade, 0, 0, "Titular *", bem);
        AdicionarCampo(grade, 0, 1, "Tipo *", tipo);
        AdicionarCampo(grade, 1, 0, "Fornecedor", fornecedor);
        AdicionarCampo(grade, 1, 1, "Contato", contato);
        AdicionarCampo(grade, 2, 0, "Início da vigência *", iniciaEm);
        AdicionarCampo(grade, 2, 1, "Fim da vigência *", terminaEm);

        var salvou = MostrarFormulario(saude is null ? "Novo convênio" : "Editar convênio", grade, () =>
        {
            var inicio = DataObrigatoria(iniciaEm, "início");
            var fim = DataObrigatoria(terminaEm, "fim");
            if (saude is null)
            {
                _contexto.RegistrarSaude().Executar(new RegistrarSaudeComando(
                    ExigirBemSelecionado(bem).Id, ValorEnum<TipoSaude>(tipo), fornecedor.Text, inicio, fim, contato.Text));
            }
            else
            {
                _contexto.EditarSaude().Executar(new EditarSaudeComando(saude.Id, ValorEnum<TipoSaude>(tipo), fornecedor.Text, inicio, fim, contato.Text));
            }
        });

        if (salvou)
        {
            _contexto.NotificarAlteracao();
        }
    }

    private void CancelarSaude(Saude saude)
    {
        if (Cancelado(saude))
        {
            throw new InvalidOperationException("este convênio já está cancelado.");
        }

        if (!MostrarConfirmacao("Cancelar convênio", "O convênio será marcado como cancelado hoje. Cancelamento é definitivo (o registro fica no histórico). Deseja continuar?", "Cancelar convênio"))
        {
            return;
        }

        _contexto.CancelarSaude().Executar(saude.Id, Hoje());
        _contexto.NotificarAlteracao();
    }

    private void ExcluirSaude(SaudeLinha linha)
    {
        if (!MostrarConfirmacao("Excluir convênio",
                $"O convênio \"{Formatador.TextoOuTraco(linha.Fornecedor)}\" de \"{linha.Bem}\" será excluído permanentemente. Deseja continuar?",
                "Excluir"))
        {
            return;
        }

        _contexto.ExcluirSaude().Executar(linha.Id);
        _contexto.NotificarAlteracao();
    }
}
