using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Manutencao;
using Assetly.Dominio.Saude;
using Assetly.Infraestrutura.Armazenamento;

namespace Assetly.Apresentacao.Visao;

internal sealed partial class TelaPrincipal
{
    private readonly TextBlock _resumoBensEsquerda = new();
    private readonly TextBlock _resumoBensDireita = new();
    private readonly TextBlock _resumoGarantiasEsquerda = new();
    private readonly TextBlock _resumoGarantiasDireita = new();
    private readonly TextBlock _resumoManutencoesEsquerda = new();
    private readonly TextBlock _resumoManutencoesDireita = new();
    private readonly TextBlock _resumoDocumentosEsquerda = new();
    private readonly TextBlock _resumoDocumentosDireita = new();
    private readonly TextBlock _resumoAlertasEsquerda = new();
    private readonly TextBlock _resumoAlertasDireita = new();
    private readonly TextBlock _resumoSaudeEsquerda = new();
    private readonly TextBlock _resumoSaudeDireita = new();
    private readonly TextBlock _resumoGastosSaudeEsquerda = new();
    private readonly TextBlock _resumoGastosSaudeDireita = new();

    private UIElement CriarAbaBens()
    {
        var (direita, busca) = CabecalhoBuscaCriacao("Buscar por nome, tipo ou observações", "Novo bem", () => AbrirDialogoBem(null));

        _filtroBens = new FiltroTabela<BemLinha>(_tabelaBens, l => l.Id, l => $"{l.Nome} {l.Tipo} {l.Observacoes}",
        [
            new("Todos", _ => true),
            new("Ativos", l => l.Origem.Status == StatusBem.Ativo),
            new("Em manutenção", l => l.Origem.Status == StatusBem.EmManutencao),
            new("Arquivados", l => l.Origem.Status == StatusBem.Arquivado),
            new("Descartados", l => l.Origem.Status == StatusBem.Descartado)
        ]);
        busca.TextChanged += (_, _) => _filtroBens.DefinirBusca(busca.Text);

        var contextual = BarraContextual(_tabelaBens,
        [
            ("Detalhes", MostrarDetalhesBem),
            ("Editar", () => AbrirDialogoBem(Selecionado<BemLinha>(_tabelaBens, "Selecione um bem para editar.").Origem)),
            ("Ativar", () => AlterarStatusBem(StatusBem.Ativo)),
            ("Manutenção", () => AlterarStatusBem(StatusBem.EmManutencao))
        ],
        [
            ("Descartar", () => AlterarStatusBem(StatusBem.Descartado)),
            ("Arquivar", () => AlterarStatusBem(StatusBem.Arquivado))
        ]);

        var raiz = TelaComTabela("Bens", "Inventário dos ativos, status de uso e valor de compra.", direita,
            _filtroBens.Barra, _tabelaBens, contextual, BarraResumoTabela(_resumoBensEsquerda, _resumoBensDireita));

        ConfigurarAtalhosTabela(raiz, _tabelaBens, busca, () => AbrirDialogoBem(null),
            () => AbrirDialogoBem(Selecionado<BemLinha>(_tabelaBens, "Selecione um bem para editar.").Origem),
            () => AlterarStatusBem(StatusBem.Descartado));
        return raiz;
    }

    private UIElement CriarAbaGarantias()
    {
        var (direita, busca) = CabecalhoBuscaCriacao("Buscar por bem ou fornecedor", "Nova garantia", () => AbrirDialogoGarantia(null));

        _filtroGarantias = new FiltroTabela<GarantiaLinha>(_tabelaGarantias, l => l.Id, l => $"{l.Bem} {l.Fornecedor}",
        [
            new("Todas", _ => true),
            new("Vencidas", l => l.StatusClasse == "etiqueta-vencida"),
            new("Próximas", l => l.StatusClasse == "etiqueta-perto-do-vencimento"),
            new("Ativas", l => l.StatusClasse == "etiqueta-ativa")
        ]);
        busca.TextChanged += (_, _) => _filtroGarantias.DefinirBusca(busca.Text);

        var contextual = BarraContextual(_tabelaGarantias,
        [
            ("Detalhes", () => MostrarInformacao("Garantia", DetalhesGarantia(Selecionado<GarantiaLinha>(_tabelaGarantias, "Selecione uma garantia.").Origem))),
            ("Editar", () => AbrirDialogoGarantia(Selecionado<GarantiaLinha>(_tabelaGarantias, "Selecione uma garantia para editar.").Origem))
        ], []);

        var raiz = TelaComTabela("Garantias", "Coberturas cadastradas, fornecedores e vencimentos.", direita,
            _filtroGarantias.Barra, _tabelaGarantias, contextual, BarraResumoTabela(_resumoGarantiasEsquerda, _resumoGarantiasDireita));

        ConfigurarAtalhosTabela(raiz, _tabelaGarantias, busca, () => AbrirDialogoGarantia(null),
            () => AbrirDialogoGarantia(Selecionado<GarantiaLinha>(_tabelaGarantias, "Selecione uma garantia para editar.").Origem), null);
        return raiz;
    }

    private UIElement CriarAbaManutencoes()
    {
        var (direita, busca) = CabecalhoBuscaCriacao("Buscar por bem ou descrição", "Nova manutenção", () => AbrirDialogoManutencao(null));

        _filtroManutencoes = new FiltroTabela<ManutencaoLinha>(_tabelaManutencoes, l => l.Id, l => $"{l.Bem} {l.Descricao}",
        [
            new("Todas", _ => true),
            new("Atrasadas", l => l.StatusClasse == "etiqueta-atrasada"),
            new("Agendadas", l => l.StatusClasse == "etiqueta-agendada"),
            new("Concluídas", l => l.StatusClasse == "etiqueta-concluida"),
            new("Canceladas", l => l.StatusClasse == "etiqueta-cancelada")
        ]);
        busca.TextChanged += (_, _) => _filtroManutencoes.DefinirBusca(busca.Text);

        var contextual = BarraContextual(_tabelaManutencoes,
        [
            ("Detalhes", () => MostrarInformacao("Manutenção", DetalhesManutencao(Selecionado<ManutencaoLinha>(_tabelaManutencoes, "Selecione uma manutenção.").Origem))),
            ("Editar", () => AbrirDialogoManutencao(Selecionado<ManutencaoLinha>(_tabelaManutencoes, "Selecione uma manutenção para editar.").Origem)),
            ("Concluir", ConcluirManutencao)
        ],
        [
            ("Cancelar", CancelarManutencao)
        ]);

        var raiz = TelaComTabela("Manutenções", "Agenda preventiva, corretiva, custos e conclusões.", direita,
            _filtroManutencoes.Barra, _tabelaManutencoes, contextual, BarraResumoTabela(_resumoManutencoesEsquerda, _resumoManutencoesDireita));

        ConfigurarAtalhosTabela(raiz, _tabelaManutencoes, busca, () => AbrirDialogoManutencao(null),
            () => AbrirDialogoManutencao(Selecionado<ManutencaoLinha>(_tabelaManutencoes, "Selecione uma manutenção para editar.").Origem), CancelarManutencao);
        return raiz;
    }

    private UIElement CriarAbaDocumentos()
    {
        var (direita, busca) = CabecalhoBuscaCriacao("Buscar por bem ou nome do documento", "Novo documento", () => AbrirDialogoDocumento(null));

        _filtroDocumentos = new FiltroTabela<DocumentoLinha>(_tabelaDocumentos, l => l.Id, l => $"{l.Bem} {l.Nome}",
        [
            new("Todos", _ => true),
            new("Disponíveis", l => l.StatusClasse == "etiqueta-disponivel"),
            new("Ausentes", l => l.StatusClasse == "etiqueta-ausente"),
            new("Arquivados", l => l.StatusClasse == "etiqueta-arquivado")
        ]);
        busca.TextChanged += (_, _) => _filtroDocumentos.DefinirBusca(busca.Text);

        var contextual = BarraContextual(_tabelaDocumentos,
        [
            ("Abrir", AbrirDocumentoSelecionado),
            ("Editar", () => AbrirDialogoDocumento(Selecionado<DocumentoLinha>(_tabelaDocumentos, "Selecione um documento para editar.").Origem)),
            ("Marcar ausente", MarcarDocumentoAusente)
        ],
        [
            ("Arquivar", ArquivarDocumento)
        ]);

        var raiz = TelaComTabela("Documentos", "Notas fiscais, comprovantes e arquivos ligados aos bens.", direita,
            _filtroDocumentos.Barra, _tabelaDocumentos, contextual, BarraResumoTabela(_resumoDocumentosEsquerda, _resumoDocumentosDireita));

        ConfigurarAtalhosTabela(raiz, _tabelaDocumentos, busca, () => AbrirDialogoDocumento(null),
            () => AbrirDialogoDocumento(Selecionado<DocumentoLinha>(_tabelaDocumentos, "Selecione um documento para editar.").Origem), ArquivarDocumento);
        return raiz;
    }

    private UIElement CriarAbaAlertas()
    {
        var (direita, busca) = CabecalhoBuscaCriacao("Buscar por bem ou mensagem", "Novo alerta", AbrirDialogoAlerta);

        _filtroAlertas = new FiltroTabela<AlertaLinha>(_tabelaAlertas, l => l.Id, l => $"{l.Bem} {l.Mensagem}",
        [
            new("Abertos", l => l.StatusClasse == "etiqueta-aberto"),
            new("Ciente", l => l.StatusClasse == "etiqueta-ciente"),
            new("Resolvidos", l => l.StatusClasse == "etiqueta-resolvido"),
            new("Todos", _ => true)
        ]);
        busca.TextChanged += (_, _) => _filtroAlertas.DefinirBusca(busca.Text);

        var contextual = BarraContextual(_tabelaAlertas,
        [
            ("Detalhes", () => MostrarInformacao("Alerta", DetalhesAlerta(Selecionado<AlertaLinha>(_tabelaAlertas, "Selecione um alerta.").Origem))),
            ("Ciente", TomarCienciaAlerta),
            ("Resolver", ResolverAlerta)
        ], []);

        var raiz = TelaComTabela("Alertas", "Pendências abertas e eventos que exigem acompanhamento.", direita,
            _filtroAlertas.Barra, _tabelaAlertas, contextual, BarraResumoTabela(_resumoAlertasEsquerda, _resumoAlertasDireita));

        ConfigurarAtalhosTabela(raiz, _tabelaAlertas, busca, AbrirDialogoAlerta, null, null);
        return raiz;
    }

    private UIElement CriarAbaSaude()
    {
        var (direita, busca) = CabecalhoBuscaCriacao("Buscar por fornecedor", "Novo convênio", () => AbrirDialogoSaude(null));

        _filtroSaude = new FiltroTabela<SaudeLinha>(_tabelaSaude, l => l.Id, l => $"{l.Bem} {l.Fornecedor}",
        [
            new("Todos", _ => true),
            new("Vencidos", l => l.StatusClasse == "etiqueta-vencida"),
            new("Próximos", l => l.StatusClasse == "etiqueta-perto-do-vencimento"),
            new("Ativos", l => l.StatusClasse == "etiqueta-ativa"),
            new("Cancelados", l => l.StatusClasse == "etiqueta-cancelada")
        ]);
        busca.TextChanged += (_, _) => _filtroSaude.DefinirBusca(busca.Text);

        var contextual = BarraContextual(_tabelaSaude,
        [
            ("Detalhes", () => MostrarInformacao("Convênio", DetalhesSaude(Selecionado<SaudeLinha>(_tabelaSaude, "Selecione um convênio.").Origem))),
            ("Editar", () => AbrirDialogoSaude(Selecionado<SaudeLinha>(_tabelaSaude, "Selecione um convênio para editar.").Origem))
        ],
        [
            ("Cancelar", CancelarSaude)
        ]);

        var raiz = TelaComTabela("Convênios", "Planos de saúde, ortodontia e coberturas vigentes.", direita,
            _filtroSaude.Barra, _tabelaSaude, contextual, BarraResumoTabela(_resumoSaudeEsquerda, _resumoSaudeDireita));

        ConfigurarAtalhosTabela(raiz, _tabelaSaude, busca, () => AbrirDialogoSaude(null),
            () => AbrirDialogoSaude(Selecionado<SaudeLinha>(_tabelaSaude, "Selecione um convênio para editar.").Origem), CancelarSaude);
        return raiz;
    }

    private UIElement CriarAbaGastosSaude()
    {
        var (direita, busca) = CabecalhoBuscaCriacao("Buscar por bem ou descrição", "Novo gasto", () => AbrirDialogoGastoSaude(null));

        _filtroGastosSaude = new FiltroTabela<GastoSaudeLinha>(_tabelaGastosSaude, l => l.Id, l => $"{l.Bem} {l.Descricao}",
        [
            new("Todos", _ => true),
            new("Farmácia", l => l.Origem.Tipo == TipoGastoSaude.Farmacia),
            new("Consultas", l => l.Origem.Tipo == TipoGastoSaude.Consulta),
            new("Exames", l => l.Origem.Tipo == TipoGastoSaude.Exame),
            new("Procedimentos", l => l.Origem.Tipo == TipoGastoSaude.Procedimento)
        ]);
        busca.TextChanged += (_, _) => _filtroGastosSaude.DefinirBusca(busca.Text);

        var contextual = BarraContextual(_tabelaGastosSaude,
        [
            ("Editar", () => AbrirDialogoGastoSaude(Selecionado<GastoSaudeLinha>(_tabelaGastosSaude, "Selecione um gasto para editar.").Origem))
        ], []);

        var raiz = TelaComTabela("Gastos de saúde", "Farmácia, consultas, exames e demais despesas.", direita,
            _filtroGastosSaude.Barra, _tabelaGastosSaude, contextual, BarraResumoTabela(_resumoGastosSaudeEsquerda, _resumoGastosSaudeDireita));

        ConfigurarAtalhosTabela(raiz, _tabelaGastosSaude, busca, () => AbrirDialogoGastoSaude(null),
            () => AbrirDialogoGastoSaude(Selecionado<GastoSaudeLinha>(_tabelaGastosSaude, "Selecione um gasto para editar.").Origem), null);
        return raiz;
    }

    /// <summary>Estrutura comum das 7 telas de tabela: título/ações no topo, barra de filtros, e o
    /// contêiner único da tabela (linhas, barra contextual de ações e resumo no rodapé).</summary>
    private static UIElement TelaComTabela(
        string titulo, string descricao, FrameworkElement direitaCabecalho,
        FrameworkElement barraFiltro, DataGrid tabela, Border barraContextual, Border barraResumo)
    {
        var cabecalho = CabecalhoTela(titulo, descricao, direitaCabecalho);
        cabecalho.Margin = new Thickness(0, 0, 0, 14);
        barraFiltro.Margin = new Thickness(0, 0, 0, 10);

        var corpoGrade = new Grid();
        corpoGrade.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        corpoGrade.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        corpoGrade.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(tabela, 0);
        Grid.SetRow(barraContextual, 1);
        Grid.SetRow(barraResumo, 2);
        corpoGrade.Children.Add(tabela);
        corpoGrade.Children.Add(barraContextual);
        corpoGrade.Children.Add(barraResumo);

        var moldura = new Border { Style = (Style)Application.Current.Resources["CardPanelStyle"], Child = corpoGrade };

        var raiz = new Grid { Margin = new Thickness(18, 16, 20, 18) };
        raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        raiz.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        raiz.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(cabecalho, 0);
        Grid.SetRow(barraFiltro, 1);
        Grid.SetRow(moldura, 2);
        raiz.Children.Add(cabecalho);
        raiz.Children.Add(barraFiltro);
        raiz.Children.Add(moldura);
        return raiz;
    }

    /// <summary>Busca (filtro em memória, <c>Ctrl+F</c> foca) e botão primário de criação, sempre à
    /// direita da primeira linha das telas de entidade.</summary>
    private (FrameworkElement Direita, TextBox Busca) CabecalhoBuscaCriacao(string placeholder, string textoNovo, Action acaoNovo)
    {
        var busca = new TextBox
        {
            Style = (Style)Application.Current.Resources["CampoTextoStyle"],
            Width = 252,
            ToolTip = placeholder
        };
        var criar = BotaoPrimarioCriacao(textoNovo, acaoNovo);
        return (UiApoio.Linha(10, busca, criar), busca);
    }

    private static Border BarraResumoTabela(TextBlock esquerda, TextBlock direita)
    {
        var recursos = Application.Current.Resources;
        esquerda.FontFamily = (FontFamily)recursos["FontMono"];
        esquerda.FontSize = 11;
        esquerda.Foreground = (Brush)recursos["TextLabel"];
        esquerda.VerticalAlignment = VerticalAlignment.Center;
        direita.FontSize = 11.5;
        direita.Foreground = (Brush)recursos["TextMuted"];
        direita.VerticalAlignment = VerticalAlignment.Center;

        var grade = new Grid();
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(esquerda, 0);
        Grid.SetColumn(direita, 1);
        grade.Children.Add(esquerda);
        grade.Children.Add(direita);

        return new Border
        {
            Background = (Brush)recursos["TableFooterBg"],
            BorderBrush = (Brush)recursos["CardBorder"],
            BorderThickness = new Thickness(0, 1, 0, 0),
            Height = 40,
            Padding = new Thickness(16, 0, 16, 0),
            Child = grade
        };
    }

    private void ConfigurarTabelas()
    {
        ConfigurarTabelaBens();
        ConfigurarTabelaGarantias();
        ConfigurarTabelaManutencoes();
        ConfigurarTabelaDocumentos();
        ConfigurarTabelaAlertas();
        ConfigurarTabelaSaude();
        ConfigurarTabelaGastosSaude();
    }

    private static DataGridLength Estrela(double fator) => new(fator, DataGridLengthUnitType.Star);

    private void ConfigurarTabelaBens()
    {
        TabelaApoio.AplicarEstiloBase(_tabelaBens);
        _tabelaBens.Columns.Add(TabelaApoio.ColunaTexto("Nome", nameof(BemLinha.Nome), Estrela(1.9), estiloBase: "CelulaDestaqueStyle"));
        _tabelaBens.Columns.Add(TabelaApoio.ColunaEtiqueta("Status", nameof(BemLinha.Status), nameof(BemLinha.StatusClasse), new DataGridLength(134)));
        _tabelaBens.Columns.Add(TabelaApoio.ColunaTexto("Tipo", nameof(BemLinha.Tipo), new DataGridLength(150)));
        _tabelaBens.Columns.Add(TabelaApoio.ColunaTextoMono("Compra", nameof(BemLinha.Compra), new DataGridLength(104)));
        _tabelaBens.Columns.Add(TabelaApoio.ColunaTextoMono("Valor", nameof(BemLinha.Valor), new DataGridLength(132)));
        _tabelaBens.Columns.Add(TabelaApoio.ColunaTexto("Observações", nameof(BemLinha.Observacoes), Estrela(2.2), estiloBase: "CelulaSecundariaStyle"));
        TabelaApoio.ConfigurarLinhas<BemLinha>(_tabelaBens, l => l.LinhaClasse, l => MenuContextoBem(l.Origem), l => MostrarDetalhesBem(l.Origem));
    }

    private void ConfigurarTabelaGarantias()
    {
        TabelaApoio.AplicarEstiloBase(_tabelaGarantias);
        _tabelaGarantias.Columns.Add(TabelaApoio.ColunaTexto("Bem", nameof(GarantiaLinha.Bem), Estrela(1.7), estiloBase: "CelulaDestaqueStyle"));
        _tabelaGarantias.Columns.Add(TabelaApoio.ColunaEtiqueta("Status", nameof(GarantiaLinha.Status), nameof(GarantiaLinha.StatusClasse), new DataGridLength(170)));
        _tabelaGarantias.Columns.Add(TabelaApoio.ColunaTexto("Tipo", nameof(GarantiaLinha.Tipo), new DataGridLength(130)));
        _tabelaGarantias.Columns.Add(TabelaApoio.ColunaTexto("Fornecedor", nameof(GarantiaLinha.Fornecedor), Estrela(1.4)));
        _tabelaGarantias.Columns.Add(TabelaApoio.ColunaTextoMono("Início", nameof(GarantiaLinha.Inicio), new DataGridLength(104)));
        _tabelaGarantias.Columns.Add(TabelaApoio.ColunaTextoMono("Fim", nameof(GarantiaLinha.Fim), new DataGridLength(104)));
        _tabelaGarantias.Columns.Add(TabelaApoio.ColunaTexto("Suporte", nameof(GarantiaLinha.Suporte), new DataGridLength(120), estiloBase: "CelulaSecundariaStyle"));
        TabelaApoio.ConfigurarLinhas<GarantiaLinha>(_tabelaGarantias, l => l.LinhaClasse, l => MenuContextoGarantia(l.Origem), garantia => AbrirDialogoGarantia(garantia.Origem));
    }

    private void ConfigurarTabelaManutencoes()
    {
        TabelaApoio.AplicarEstiloBase(_tabelaManutencoes);
        _tabelaManutencoes.Columns.Add(TabelaApoio.ColunaTexto("Bem", nameof(ManutencaoLinha.Bem), Estrela(1.5), estiloBase: "CelulaDestaqueStyle"));
        _tabelaManutencoes.Columns.Add(TabelaApoio.ColunaEtiqueta("Status", nameof(ManutencaoLinha.Status), nameof(ManutencaoLinha.StatusClasse), new DataGridLength(130)));
        _tabelaManutencoes.Columns.Add(TabelaApoio.ColunaTexto("Tipo", nameof(ManutencaoLinha.Tipo), new DataGridLength(120)));
        _tabelaManutencoes.Columns.Add(TabelaApoio.ColunaTexto("Descrição", nameof(ManutencaoLinha.Descricao), Estrela(2.2), estiloBase: "CelulaSecundariaStyle"));
        _tabelaManutencoes.Columns.Add(TabelaApoio.ColunaTextoMono("Agendada", nameof(ManutencaoLinha.Agendada), new DataGridLength(104)));
        _tabelaManutencoes.Columns.Add(TabelaApoio.ColunaTextoMono("Concluída", nameof(ManutencaoLinha.Concluida), new DataGridLength(104)));
        _tabelaManutencoes.Columns.Add(TabelaApoio.ColunaTextoMono("Custo", nameof(ManutencaoLinha.Custo), new DataGridLength(116)));
        TabelaApoio.ConfigurarLinhas<ManutencaoLinha>(_tabelaManutencoes, l => l.LinhaClasse, l => MenuContextoManutencao(l.Origem), manutencao => AbrirDialogoManutencao(manutencao.Origem));
    }

    private void ConfigurarTabelaDocumentos()
    {
        TabelaApoio.AplicarEstiloBase(_tabelaDocumentos);
        _tabelaDocumentos.Columns.Add(TabelaApoio.ColunaTexto("Bem", nameof(DocumentoLinha.Bem), Estrela(1.3), estiloBase: "CelulaDestaqueStyle"));
        _tabelaDocumentos.Columns.Add(TabelaApoio.ColunaEtiqueta("Status", nameof(DocumentoLinha.Status), nameof(DocumentoLinha.StatusClasse), new DataGridLength(128)));
        _tabelaDocumentos.Columns.Add(TabelaApoio.ColunaTexto("Tipo", nameof(DocumentoLinha.Tipo), new DataGridLength(150)));
        _tabelaDocumentos.Columns.Add(TabelaApoio.ColunaTexto("Nome", nameof(DocumentoLinha.Nome), Estrela(1.6)));
        _tabelaDocumentos.Columns.Add(TabelaApoio.ColunaArquivo("Arquivo local", nameof(DocumentoLinha.ArquivoLocal), Estrela(1.3),
            contexto => AbrirDocumentoDaLinha((DocumentoLinha)contexto)));
        _tabelaDocumentos.Columns.Add(TabelaApoio.ColunaTextoMono("Registrado", nameof(DocumentoLinha.Registrado), new DataGridLength(110)));
        TabelaApoio.ConfigurarLinhas<DocumentoLinha>(_tabelaDocumentos, l => l.LinhaClasse, l => MenuContextoDocumento(l.Origem), l => AbrirDocumento(l.Origem));
    }

    private void ConfigurarTabelaAlertas()
    {
        TabelaApoio.AplicarEstiloBase(_tabelaAlertas);
        _tabelaAlertas.Columns.Add(TabelaApoio.ColunaTexto("Bem", nameof(AlertaLinha.Bem), Estrela(1.3), estiloBase: "CelulaDestaqueStyle"));
        _tabelaAlertas.Columns.Add(TabelaApoio.ColunaEtiqueta("Severidade", nameof(AlertaLinha.Severidade), nameof(AlertaLinha.SeveridadeClasse), new DataGridLength(128)));
        _tabelaAlertas.Columns.Add(TabelaApoio.ColunaTexto("Tipo", nameof(AlertaLinha.Tipo), new DataGridLength(180)));
        _tabelaAlertas.Columns.Add(TabelaApoio.ColunaTexto("Mensagem", nameof(AlertaLinha.Mensagem), Estrela(2.4), estiloBase: "CelulaSecundariaStyle"));
        _tabelaAlertas.Columns.Add(TabelaApoio.ColunaTextoMono("Criado", nameof(AlertaLinha.Criado), new DataGridLength(104)));
        _tabelaAlertas.Columns.Add(TabelaApoio.ColunaTextoMono("Prazo", nameof(AlertaLinha.Prazo), new DataGridLength(104)));
        TabelaApoio.ConfigurarLinhas<AlertaLinha>(_tabelaAlertas, l => l.LinhaClasse, l => MenuContextoAlerta(l.Origem));
    }

    private void ConfigurarTabelaSaude()
    {
        TabelaApoio.AplicarEstiloBase(_tabelaSaude);
        _tabelaSaude.Columns.Add(TabelaApoio.ColunaTexto("Bem", nameof(SaudeLinha.Bem), Estrela(1.7), estiloBase: "CelulaDestaqueStyle"));
        _tabelaSaude.Columns.Add(TabelaApoio.ColunaEtiqueta("Status", nameof(SaudeLinha.Status), nameof(SaudeLinha.StatusClasse), new DataGridLength(170)));
        _tabelaSaude.Columns.Add(TabelaApoio.ColunaTexto("Tipo", nameof(SaudeLinha.Tipo), new DataGridLength(130)));
        _tabelaSaude.Columns.Add(TabelaApoio.ColunaTexto("Fornecedor", nameof(SaudeLinha.Fornecedor), Estrela(1.4)));
        _tabelaSaude.Columns.Add(TabelaApoio.ColunaTextoMono("Início", nameof(SaudeLinha.Inicio), new DataGridLength(104)));
        _tabelaSaude.Columns.Add(TabelaApoio.ColunaTextoMono("Fim", nameof(SaudeLinha.Fim), new DataGridLength(104)));
        _tabelaSaude.Columns.Add(TabelaApoio.ColunaTexto("Contato", nameof(SaudeLinha.Contato), new DataGridLength(120), estiloBase: "CelulaSecundariaStyle"));
        TabelaApoio.ConfigurarLinhas<SaudeLinha>(_tabelaSaude, l => l.LinhaClasse, l => MenuContextoSaude(l.Origem), saude => AbrirDialogoSaude(saude.Origem));
    }

    private void ConfigurarTabelaGastosSaude()
    {
        TabelaApoio.AplicarEstiloBase(_tabelaGastosSaude);
        _tabelaGastosSaude.Columns.Add(TabelaApoio.ColunaTexto("Bem", nameof(GastoSaudeLinha.Bem), Estrela(1.3), estiloBase: "CelulaDestaqueStyle"));
        _tabelaGastosSaude.Columns.Add(TabelaApoio.ColunaTexto("Tipo", nameof(GastoSaudeLinha.Tipo), new DataGridLength(140)));
        _tabelaGastosSaude.Columns.Add(TabelaApoio.ColunaTexto("Descrição", nameof(GastoSaudeLinha.Descricao), Estrela(2.2), estiloBase: "CelulaSecundariaStyle"));
        _tabelaGastosSaude.Columns.Add(TabelaApoio.ColunaTextoMono("Data", nameof(GastoSaudeLinha.Data), new DataGridLength(104)));
        _tabelaGastosSaude.Columns.Add(TabelaApoio.ColunaTextoMono("Valor", nameof(GastoSaudeLinha.Valor), new DataGridLength(132)));
        TabelaApoio.ConfigurarLinhas<GastoSaudeLinha>(_tabelaGastosSaude, _ => "linha-neutra", l => MenuContextoGastoSaude(l.Origem), gasto => AbrirDialogoGastoSaude(gasto.Origem));
    }

    private void AbrirDocumentoDaLinha(DocumentoLinha linha)
    {
        _tabelaDocumentos.SelectedItem = linha;
        ExecutarComTratamento(() => AbrirDocumento(linha.Origem));
    }

    private IReadOnlyList<BemLinha> ConstruirLinhasBens(IReadOnlyList<Bem> bens)
    {
        var ativos = bens.Count(b => b.Status == StatusBem.Ativo);
        var emManutencao = bens.Count(b => b.Status == StatusBem.EmManutencao);
        var valorTotal = bens.Sum(b => b.PrecoCompra?.Valor ?? 0m);
        _resumoBensEsquerda.Text = $"{bens.Count} bens · {ativos} ativos · {emManutencao} em manutenção";
        _resumoBensDireita.Text = $"Valor total {valorTotal.ToString("C2", CultureInfo.GetCultureInfo("pt-BR"))}";

        return bens.Select(bem => new BemLinha(
            bem.Id,
            bem.Nome.Valor,
            Formatador.TextoEnum(bem.Tipo),
            Formatador.TextoEnum(bem.Status),
            Formatador.ClasseEtiqueta(bem.Status),
            ClasseLinhaBem(bem.Status),
            Formatador.Data(bem.CompradoEm),
            Formatador.Dinheiro(bem.PrecoCompra),
            bem.Observacoes,
            bem)).ToList();
    }

    private IReadOnlyList<GarantiaLinha> ConstruirLinhasGarantias(IReadOnlyList<Garantia> garantias)
    {
        var hoje = Hoje();
        var vencidas = garantias.Count(g => g.StatusEm(hoje) == StatusGarantia.Vencida);
        var proximas = garantias.Count(g => g.StatusEm(hoje) == StatusGarantia.PertoDoVencimento);
        var coberturaMediaMeses = garantias.Count == 0 ? 0 :
            garantias.Average(g => (g.Periodo.TerminaEm.ToDateTime(TimeOnly.MinValue) - g.Periodo.IniciaEm.ToDateTime(TimeOnly.MinValue)).TotalDays / 30.44);
        _resumoGarantiasEsquerda.Text = $"{garantias.Count} garantias · {vencidas} vencida{(vencidas == 1 ? "" : "s")} · {proximas} vence{(proximas == 1 ? "" : "m")} em até 30 dias";
        _resumoGarantiasDireita.Text = $"Cobertura média {Math.Round(coberturaMediaMeses)} meses";

        return garantias.Select(g => new GarantiaLinha(
            g.Id,
            NomeBem(g.BemId),
            Formatador.TextoEnum(g.Tipo),
            g.Fornecedor,
            Formatador.Data(g.Periodo.IniciaEm),
            Formatador.Data(g.Periodo.TerminaEm),
            Formatador.TextoEnum(g.StatusEm(hoje)),
            Formatador.ClasseEtiqueta(g.StatusEm(hoje)),
            ClasseLinhaGarantia(g.StatusEm(hoje)),
            Formatador.TextoOuTraco(g.ContatoSuporte),
            g)).ToList();
    }

    private IReadOnlyList<ManutencaoLinha> ConstruirLinhasManutencoes(IReadOnlyList<Manutencao> manutencoes)
    {
        var hoje = Hoje();
        var atrasadas = manutencoes.Count(m => m.StatusEm(hoje) == StatusManutencao.Atrasada);
        var agendadas = manutencoes.Count(m => m.StatusEm(hoje) == StatusManutencao.Agendada);
        var custoAcumulado = manutencoes.Sum(m => m.Custo?.Valor ?? 0m);
        _resumoManutencoesEsquerda.Text = $"{manutencoes.Count} manutenções · {atrasadas} atrasada{(atrasadas == 1 ? "" : "s")} · {agendadas} agendada{(agendadas == 1 ? "" : "s")}";
        _resumoManutencoesDireita.Text = $"Custo acumulado {custoAcumulado.ToString("C2", CultureInfo.GetCultureInfo("pt-BR"))}";

        return manutencoes.Select(m => new ManutencaoLinha(
            m.Id,
            NomeBem(m.BemId),
            Formatador.TextoEnum(m.Tipo),
            m.Descricao,
            Formatador.Data(m.AgendadaPara),
            Formatador.Data(m.ConcluidaEm),
            Formatador.Dinheiro(m.Custo),
            Formatador.TextoEnum(m.StatusEm(hoje)),
            Formatador.ClasseEtiqueta(m.StatusEm(hoje)),
            ClasseLinhaManutencao(m.StatusEm(hoje)),
            m)).ToList();
    }

    private IReadOnlyList<DocumentoLinha> ConstruirLinhasDocumentos(IReadOnlyList<Documento> documentos)
    {
        var disponiveis = documentos.Count(d => d.Status == StatusDocumento.Disponivel);
        var ausentes = documentos.Count(d => d.Status == StatusDocumento.Ausente);
        var pasta = _contexto.AmbienteTeste ? CaminhosArmazenamentoLocal.DiretorioDocumentosTeste() : CaminhosArmazenamentoLocal.DiretorioDocumentos();
        _resumoDocumentosEsquerda.Text = $"{documentos.Count} documentos · {disponiveis} disponíve{(disponiveis == 1 ? "l" : "is")} · {ausentes} ausente{(ausentes == 1 ? "" : "s")}";
        _resumoDocumentosDireita.Text = $"Pasta {pasta}";

        return documentos.Select(d => new DocumentoLinha(
            d.Id,
            NomeBem(d.BemId),
            Formatador.TextoEnum(d.Tipo),
            d.Nome.Valor,
            d.CaminhoLocal.Valor,
            Formatador.Data(d.RegistradoEm),
            Formatador.TextoEnum(d.Status),
            Formatador.ClasseEtiqueta(d.Status),
            ClasseLinhaDocumento(d.Status),
            d)).ToList();
    }

    private IReadOnlyList<AlertaLinha> ConstruirLinhasAlertas(IReadOnlyList<Alerta> alertas)
    {
        var abertos = alertas.Count(a => a.Status != StatusAlerta.Resolvido);
        var criticos = alertas.Count(a => a.Status != StatusAlerta.Resolvido && a.Severidade == SeveridadeAlerta.Critico);
        var avisos = alertas.Count(a => a.Status != StatusAlerta.Resolvido && a.Severidade == SeveridadeAlerta.Aviso);
        _resumoAlertasEsquerda.Text = $"{abertos} alertas abertos · {criticos} crítico{(criticos == 1 ? "" : "s")} · {avisos} aviso{(avisos == 1 ? "" : "s")}";
        _resumoAlertasDireita.Text = "Deduplicação ativa";

        return alertas.Select(a => new AlertaLinha(
            a.Id,
            NomeBem(a.BemId),
            Formatador.TextoEnum(a.Tipo),
            Formatador.TextoEnum(a.Severidade),
            Formatador.ClasseEtiqueta(a.Severidade),
            a.Mensagem,
            Formatador.Data(a.CriadoEm),
            Formatador.Data(a.PrazoEm),
            Formatador.TextoEnum(a.Status),
            Formatador.ClasseEtiqueta(a.Status),
            ClasseLinhaAlerta(a),
            a)).ToList();
    }

    private IReadOnlyList<SaudeLinha> ConstruirLinhasSaude(IReadOnlyList<Saude> registros)
    {
        var hoje = Hoje();
        var vencidos = registros.Count(s => s.StatusEm(hoje) == StatusSaude.Vencida);
        var proximos = registros.Count(s => s.StatusEm(hoje) == StatusSaude.PertoDoVencimento);
        var ativos = registros.Where(s => s.StatusEm(hoje) != StatusSaude.Cancelada).ToList();
        var coberturaMediaMeses = ativos.Count == 0 ? 0 :
            ativos.Average(s => (s.Periodo.TerminaEm.ToDateTime(TimeOnly.MinValue) - s.Periodo.IniciaEm.ToDateTime(TimeOnly.MinValue)).TotalDays / 30.44);
        _resumoSaudeEsquerda.Text = $"{registros.Count} convênios · {vencidos} vencido{(vencidos == 1 ? "" : "s")} · {proximos} vence{(proximos == 1 ? "" : "m")} em até 30 dias";
        _resumoSaudeDireita.Text = $"Cobertura média {Math.Round(coberturaMediaMeses)} meses";

        return registros.Select(s => new SaudeLinha(
            s.Id,
            NomeBem(s.BemId),
            Formatador.TextoEnum(s.Tipo),
            s.Fornecedor,
            Formatador.Data(s.Periodo.IniciaEm),
            Formatador.Data(s.Periodo.TerminaEm),
            Formatador.TextoEnum(s.StatusEm(hoje)),
            Formatador.ClasseEtiqueta(s.StatusEm(hoje)),
            ClasseLinhaSaude(s.StatusEm(hoje)),
            Formatador.TextoOuTraco(s.Contato),
            s)).ToList();
    }

    private IReadOnlyList<GastoSaudeLinha> ConstruirLinhasGastosSaude(IReadOnlyList<GastoSaude> gastos)
    {
        var totalFarmacia = gastos.Count(g => g.Tipo == TipoGastoSaude.Farmacia);
        var totalConsultas = gastos.Count(g => g.Tipo == TipoGastoSaude.Consulta);
        var totalGasto = gastos.Sum(g => g.Valor.Valor);
        _resumoGastosSaudeEsquerda.Text = $"{gastos.Count} gastos · {totalFarmacia} farmácia · {totalConsultas} consulta{(totalConsultas == 1 ? "" : "s")}";
        _resumoGastosSaudeDireita.Text = $"Total gasto {totalGasto.ToString("C2", CultureInfo.GetCultureInfo("pt-BR"))}";

        return gastos.Select(g => new GastoSaudeLinha(
            g.Id,
            NomeBem(g.BemId),
            Formatador.TextoEnum(g.Tipo),
            g.Descricao,
            Formatador.Data(g.Data),
            Formatador.Dinheiro(g.Valor),
            g)).ToList();
    }

    private static string ClasseLinhaBem(StatusBem status) => status switch
    {
        StatusBem.Ativo => "linha-ok",
        StatusBem.EmManutencao => "linha-aviso",
        StatusBem.Descartado => "linha-perigo",
        StatusBem.Arquivado => "linha-neutra",
        _ => "linha-neutra"
    };

    private static string ClasseLinhaGarantia(StatusGarantia status) => status switch
    {
        StatusGarantia.Ativa => "linha-ok",
        StatusGarantia.PertoDoVencimento or StatusGarantia.NaoIniciada => "linha-aviso",
        StatusGarantia.Vencida => "linha-perigo",
        _ => "linha-neutra"
    };

    private static string ClasseLinhaManutencao(StatusManutencao status) => status switch
    {
        StatusManutencao.Concluida => "linha-ok",
        StatusManutencao.Agendada => "linha-informativa",
        StatusManutencao.Atrasada => "linha-perigo",
        StatusManutencao.Cancelada => "linha-neutra",
        _ => "linha-neutra"
    };

    private static string ClasseLinhaDocumento(StatusDocumento status) => status switch
    {
        StatusDocumento.Disponivel => "linha-ok",
        StatusDocumento.Ausente => "linha-aviso",
        StatusDocumento.Arquivado => "linha-neutra",
        _ => "linha-neutra"
    };

    private static string ClasseLinhaSaude(StatusSaude status) => status switch
    {
        StatusSaude.Ativa => "linha-ok",
        StatusSaude.PertoDoVencimento or StatusSaude.NaoIniciada => "linha-aviso",
        StatusSaude.Vencida => "linha-perigo",
        _ => "linha-neutra"
    };

    private static string ClasseLinhaAlerta(Alerta alerta)
    {
        if (alerta.Status == StatusAlerta.Resolvido)
        {
            return "linha-ok";
        }

        return alerta.Severidade switch
        {
            SeveridadeAlerta.Critico => "linha-perigo",
            SeveridadeAlerta.Aviso => "linha-aviso",
            SeveridadeAlerta.Informativo => "linha-informativa",
            _ => "linha-neutra"
        };
    }

    private static string DetalhesGarantia(Garantia garantia) => $"""
        Tipo: {Formatador.TextoEnum(garantia.Tipo)}
        Fornecedor: {garantia.Fornecedor}
        Início: {Formatador.Data(garantia.Periodo.IniciaEm)}
        Fim: {Formatador.Data(garantia.Periodo.TerminaEm)}
        Suporte: {Formatador.TextoOuTraco(garantia.ContatoSuporte)}
        """;

    private static string DetalhesManutencao(Manutencao manutencao) => $"""
        Tipo: {Formatador.TextoEnum(manutencao.Tipo)}
        Descrição: {manutencao.Descricao}
        Agendada para: {Formatador.Data(manutencao.AgendadaPara)}
        Concluída em: {Formatador.Data(manutencao.ConcluidaEm)}
        Custo: {Formatador.Dinheiro(manutencao.Custo)}
        """;

    private static string DetalhesAlerta(Alerta alerta) => $"""
        Tipo: {Formatador.TextoEnum(alerta.Tipo)}
        Severidade: {Formatador.TextoEnum(alerta.Severidade)}
        Mensagem: {alerta.Mensagem}
        Criado em: {Formatador.Data(alerta.CriadoEm)}
        Prazo: {Formatador.Data(alerta.PrazoEm)}
        """;

    private static string DetalhesSaude(Saude saude) => $"""
        Tipo: {Formatador.TextoEnum(saude.Tipo)}
        Fornecedor: {saude.Fornecedor}
        Início: {Formatador.Data(saude.Periodo.IniciaEm)}
        Fim: {Formatador.Data(saude.Periodo.TerminaEm)}
        Contato: {Formatador.TextoOuTraco(saude.Contato)}
        """;

    private ContextMenu MenuContextoBem(Bem bem) => MenuContexto(
        ItemMenu("Editar", () => AbrirDialogoBem(bem)),
        ItemMenu("Detalhes", () => MostrarDetalhesBem(bem)),
        SeparadorMenu(),
        ItemMenu("Ativar", () => AlterarStatusBem(bem, StatusBem.Ativo)),
        ItemMenu("Marcar em manutenção", () => AlterarStatusBem(bem, StatusBem.EmManutencao)),
        SeparadorMenu(),
        ItemMenu("Descartar", () => AlterarStatusBem(bem, StatusBem.Descartado)),
        ItemMenu("Arquivar", () => AlterarStatusBem(bem, StatusBem.Arquivado)));

    private ContextMenu MenuContextoGarantia(Garantia garantia) =>
        MenuContexto(ItemMenu("Editar", () => AbrirDialogoGarantia(garantia)));

    private ContextMenu MenuContextoManutencao(Manutencao manutencao) => MenuContexto(
        ItemMenu("Editar", () => AbrirDialogoManutencao(manutencao)),
        ItemMenu("Concluir", () => ConcluirManutencao(manutencao)),
        ItemMenu("Cancelar", () => CancelarManutencao(manutencao)));

    private ContextMenu MenuContextoDocumento(Documento documento) => MenuContexto(
        ItemMenu("Abrir documento", () => AbrirDocumento(documento), documento.Status == StatusDocumento.Ausente),
        ItemMenu("Editar", () => AbrirDialogoDocumento(documento)),
        SeparadorMenu(),
        ItemMenu("Marcar ausente", () => MarcarDocumentoAusente(documento)),
        ItemMenu("Arquivar", () => ArquivarDocumento(documento)));

    private ContextMenu MenuContextoAlerta(Alerta alerta) => MenuContexto(
        ItemMenu("Marcar como ciente", () => TomarCienciaAlerta(alerta)),
        ItemMenu("Resolver", () => ResolverAlerta(alerta)));

    private ContextMenu MenuContextoSaude(Saude saude) => MenuContexto(
        ItemMenu("Editar", () => AbrirDialogoSaude(saude)),
        ItemMenu("Detalhes", () => MostrarInformacao("Convênio", DetalhesSaude(saude))),
        SeparadorMenu(),
        ItemMenu("Cancelar", () => CancelarSaude(saude)));

    private ContextMenu MenuContextoGastoSaude(GastoSaude gasto) =>
        MenuContexto(ItemMenu("Editar", () => AbrirDialogoGastoSaude(gasto)));

    private static ContextMenu MenuContexto(params object[] itens)
    {
        var menu = new ContextMenu();
        foreach (var item in itens)
        {
            menu.Items.Add(item);
        }

        return menu;
    }

    private MenuItem ItemMenu(string texto, Action acao, bool desabilitado = false)
    {
        var item = new MenuItem { Header = texto, IsEnabled = !desabilitado };
        item.Click += (_, _) => ExecutarComTratamento(acao);
        return item;
    }

    private static Separator SeparadorMenu() => new();
}
