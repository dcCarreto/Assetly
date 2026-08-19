using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Manutencao;

namespace Assetly.Apresentacao.Visao;

internal sealed partial class TelaPrincipal
{
    private UIElement CriarAbaBens()
    {
        var barra = BarraAcoes(
            BotaoPrimario("Novo", () => AbrirDialogoBem(null)),
            BotaoSecundario("Editar", () => AbrirDialogoBem(Selecionado<BemLinha>(_tabelaBens, "Selecione um bem para editar.").Origem)),
            BotaoSecundario("Detalhes", MostrarDetalhesBem),
            BotaoSecundario("Ativar", () => AlterarStatusBem(StatusBem.Ativo)),
            BotaoSecundario("Manutenção", () => AlterarStatusBem(StatusBem.EmManutencao)),
            BotaoPerigo("Descartar", () => AlterarStatusBem(StatusBem.Descartado)),
            BotaoPerigo("Arquivar", () => AlterarStatusBem(StatusBem.Arquivado)));
        return TelaComTabela("Bens", "Inventário dos ativos, status de uso e valor de compra.", barra, _tabelaBens);
    }

    private UIElement CriarAbaGarantias()
    {
        var barra = BarraAcoes(
            BotaoPrimario("Nova", () => AbrirDialogoGarantia(null)),
            BotaoSecundario("Editar", () => AbrirDialogoGarantia(Selecionado<GarantiaLinha>(_tabelaGarantias, "Selecione uma garantia para editar.").Origem)));
        return TelaComTabela("Garantias", "Coberturas cadastradas, fornecedores e vencimentos.", barra, _tabelaGarantias);
    }

    private UIElement CriarAbaManutencoes()
    {
        var barra = BarraAcoes(
            BotaoPrimario("Nova", () => AbrirDialogoManutencao(null)),
            BotaoSecundario("Editar", () => AbrirDialogoManutencao(Selecionado<ManutencaoLinha>(_tabelaManutencoes, "Selecione uma manutenção para editar.").Origem)),
            BotaoSecundario("Concluir", ConcluirManutencao),
            BotaoPerigo("Cancelar", CancelarManutencao));
        return TelaComTabela("Manutenções", "Agenda preventiva, corretiva, custos e conclusões.", barra, _tabelaManutencoes);
    }

    private UIElement CriarAbaDocumentos()
    {
        var barra = BarraAcoes(
            BotaoPrimario("Novo", () => AbrirDialogoDocumento(null)),
            BotaoSecundario("Abrir", AbrirDocumentoSelecionado),
            BotaoSecundario("Editar", () => AbrirDialogoDocumento(Selecionado<DocumentoLinha>(_tabelaDocumentos, "Selecione um documento para editar.").Origem)),
            BotaoSecundario("Marcar ausente", MarcarDocumentoAusente),
            BotaoPerigo("Arquivar", ArquivarDocumento));
        return TelaComTabela("Documentos", "Notas fiscais, comprovantes e arquivos ligados aos bens.", barra, _tabelaDocumentos);
    }

    private UIElement CriarAbaAlertas()
    {
        var barra = BarraAcoes(
            BotaoPrimario("Novo", AbrirDialogoAlerta),
            BotaoSecundario("Ciente", TomarCienciaAlerta),
            BotaoSecundario("Resolver", ResolverAlerta));
        return TelaComTabela("Alertas", "Pendências abertas e eventos que exigem acompanhamento.", barra, _tabelaAlertas);
    }

    private UIElement TelaComTabela(string titulo, string descricao, FrameworkElement barra, DataGrid tabela)
    {
        var moldura = new Border { Style = (Style)Application.Current.Resources["CardStyle"], Padding = new Thickness(0), Effect = null, Child = tabela };

        var cabecalho = CabecalhoConteudo(titulo, descricao);
        cabecalho.Margin = new Thickness(0, 0, 0, 14);
        barra.Margin = new Thickness(0, 0, 0, 14);

        var raiz = new DockPanel { Margin = new Thickness(18) };
        DockPanel.SetDock(cabecalho, Dock.Top);
        DockPanel.SetDock(barra, Dock.Top);
        raiz.Children.Add(cabecalho);
        raiz.Children.Add(barra);
        raiz.Children.Add(moldura);
        return raiz;
    }

    private FrameworkElement CabecalhoConteudo(string titulo, string descricao)
    {
        var tituloLabel = UiApoio.Texto(titulo, 17, FontWeights.Black, (Brush)Application.Current.Resources["TextoTituloBrush"]);
        var descricaoLabel = UiApoio.Texto(descricao, 13, FontWeights.Normal, (Brush)Application.Current.Resources["TextoSuaveBrush"]);
        return UiApoio.Coluna(4, tituloLabel, descricaoLabel);
    }

    private void ConfigurarTabelas()
    {
        ConfigurarTabelaBens();
        ConfigurarTabelaGarantias();
        ConfigurarTabelaManutencoes();
        ConfigurarTabelaDocumentos();
        ConfigurarTabelaAlertas();
    }

    private void ConfigurarTabelaBens()
    {
        TabelaApoio.AplicarEstiloBase(_tabelaBens);
        _tabelaBens.Columns.Add(TabelaApoio.ColunaTexto("Nome", nameof(BemLinha.Nome)));
        _tabelaBens.Columns.Add(TabelaApoio.ColunaTexto("Tipo", nameof(BemLinha.Tipo)));
        _tabelaBens.Columns.Add(TabelaApoio.ColunaEtiqueta("Status", nameof(BemLinha.Status), nameof(BemLinha.StatusClasse)));
        _tabelaBens.Columns.Add(TabelaApoio.ColunaTexto("Compra", nameof(BemLinha.Compra)));
        _tabelaBens.Columns.Add(TabelaApoio.ColunaTexto("Valor", nameof(BemLinha.Valor)));
        _tabelaBens.Columns.Add(TabelaApoio.ColunaTexto("Observações", nameof(BemLinha.Observacoes)));
        TabelaApoio.ConfigurarLinhas<BemLinha>(_tabelaBens, l => l.LinhaClasse, l => MenuContextoBem(l.Origem));
    }

    private void ConfigurarTabelaGarantias()
    {
        TabelaApoio.AplicarEstiloBase(_tabelaGarantias);
        _tabelaGarantias.Columns.Add(TabelaApoio.ColunaTexto("Bem", nameof(GarantiaLinha.Bem)));
        _tabelaGarantias.Columns.Add(TabelaApoio.ColunaTexto("Tipo", nameof(GarantiaLinha.Tipo)));
        _tabelaGarantias.Columns.Add(TabelaApoio.ColunaTexto("Fornecedor", nameof(GarantiaLinha.Fornecedor)));
        _tabelaGarantias.Columns.Add(TabelaApoio.ColunaTexto("Início", nameof(GarantiaLinha.Inicio)));
        _tabelaGarantias.Columns.Add(TabelaApoio.ColunaTexto("Fim", nameof(GarantiaLinha.Fim)));
        _tabelaGarantias.Columns.Add(TabelaApoio.ColunaEtiqueta("Status", nameof(GarantiaLinha.Status), nameof(GarantiaLinha.StatusClasse)));
        _tabelaGarantias.Columns.Add(TabelaApoio.ColunaTexto("Suporte", nameof(GarantiaLinha.Suporte)));
        TabelaApoio.ConfigurarLinhas<GarantiaLinha>(_tabelaGarantias, l => l.LinhaClasse, l => MenuContextoGarantia(l.Origem));
    }

    private void ConfigurarTabelaManutencoes()
    {
        TabelaApoio.AplicarEstiloBase(_tabelaManutencoes);
        _tabelaManutencoes.Columns.Add(TabelaApoio.ColunaTexto("Bem", nameof(ManutencaoLinha.Bem)));
        _tabelaManutencoes.Columns.Add(TabelaApoio.ColunaTexto("Tipo", nameof(ManutencaoLinha.Tipo)));
        _tabelaManutencoes.Columns.Add(TabelaApoio.ColunaTexto("Descrição", nameof(ManutencaoLinha.Descricao)));
        _tabelaManutencoes.Columns.Add(TabelaApoio.ColunaTexto("Agendada", nameof(ManutencaoLinha.Agendada)));
        _tabelaManutencoes.Columns.Add(TabelaApoio.ColunaTexto("Concluída", nameof(ManutencaoLinha.Concluida)));
        _tabelaManutencoes.Columns.Add(TabelaApoio.ColunaTexto("Custo", nameof(ManutencaoLinha.Custo)));
        _tabelaManutencoes.Columns.Add(TabelaApoio.ColunaEtiqueta("Status", nameof(ManutencaoLinha.Status), nameof(ManutencaoLinha.StatusClasse)));
        TabelaApoio.ConfigurarLinhas<ManutencaoLinha>(_tabelaManutencoes, l => l.LinhaClasse, l => MenuContextoManutencao(l.Origem));
    }

    private void ConfigurarTabelaDocumentos()
    {
        TabelaApoio.AplicarEstiloBase(_tabelaDocumentos);
        _tabelaDocumentos.Columns.Add(TabelaApoio.ColunaTexto("Bem", nameof(DocumentoLinha.Bem)));
        _tabelaDocumentos.Columns.Add(TabelaApoio.ColunaTexto("Tipo", nameof(DocumentoLinha.Tipo)));
        _tabelaDocumentos.Columns.Add(TabelaApoio.ColunaTexto("Nome", nameof(DocumentoLinha.Nome)));
        _tabelaDocumentos.Columns.Add(TabelaApoio.ColunaArquivo("Arquivo local", nameof(DocumentoLinha.ArquivoLocal),
            contexto => AbrirDocumentoDaLinha((DocumentoLinha)contexto)));
        _tabelaDocumentos.Columns.Add(TabelaApoio.ColunaTexto("Registrado", nameof(DocumentoLinha.Registrado)));
        _tabelaDocumentos.Columns.Add(TabelaApoio.ColunaEtiqueta("Status", nameof(DocumentoLinha.Status), nameof(DocumentoLinha.StatusClasse)));
        TabelaApoio.ConfigurarLinhas<DocumentoLinha>(_tabelaDocumentos, l => l.LinhaClasse, l => MenuContextoDocumento(l.Origem), l => AbrirDocumento(l.Origem));
    }

    private void ConfigurarTabelaAlertas()
    {
        TabelaApoio.AplicarEstiloBase(_tabelaAlertas);
        _tabelaAlertas.Columns.Add(TabelaApoio.ColunaTexto("Bem", nameof(AlertaLinha.Bem)));
        _tabelaAlertas.Columns.Add(TabelaApoio.ColunaTexto("Tipo", nameof(AlertaLinha.Tipo)));
        _tabelaAlertas.Columns.Add(TabelaApoio.ColunaEtiqueta("Severidade", nameof(AlertaLinha.Severidade), nameof(AlertaLinha.SeveridadeClasse)));
        _tabelaAlertas.Columns.Add(TabelaApoio.ColunaTexto("Mensagem", nameof(AlertaLinha.Mensagem)));
        _tabelaAlertas.Columns.Add(TabelaApoio.ColunaTexto("Criado", nameof(AlertaLinha.Criado)));
        _tabelaAlertas.Columns.Add(TabelaApoio.ColunaTexto("Prazo", nameof(AlertaLinha.Prazo)));
        _tabelaAlertas.Columns.Add(TabelaApoio.ColunaEtiqueta("Status", nameof(AlertaLinha.Status), nameof(AlertaLinha.StatusClasse)));
        TabelaApoio.ConfigurarLinhas<AlertaLinha>(_tabelaAlertas, l => l.LinhaClasse, l => MenuContextoAlerta(l.Origem));
    }

    private void AbrirDocumentoDaLinha(DocumentoLinha linha)
    {
        _tabelaDocumentos.SelectedItem = linha;
        ExecutarComTratamento(() => AbrirDocumento(linha.Origem));
    }

    private IReadOnlyList<BemLinha> ConstruirLinhasBens(IReadOnlyList<Bem> bens) =>
        bens.Select(bem => new BemLinha(
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

    private IReadOnlyList<GarantiaLinha> ConstruirLinhasGarantias(IReadOnlyList<Garantia> garantias)
    {
        var hoje = Hoje();
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
            g.ContatoSuporte,
            g)).ToList();
    }

    private IReadOnlyList<ManutencaoLinha> ConstruirLinhasManutencoes(IReadOnlyList<Manutencao> manutencoes)
    {
        var hoje = Hoje();
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

    private IReadOnlyList<DocumentoLinha> ConstruirLinhasDocumentos(IReadOnlyList<Documento> documentos) =>
        documentos.Select(d => new DocumentoLinha(
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

    private IReadOnlyList<AlertaLinha> ConstruirLinhasAlertas(IReadOnlyList<Alerta> alertas) =>
        alertas.Select(a => new AlertaLinha(
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
