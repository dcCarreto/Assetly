package com.assetly.apresentacao.visao;

import com.assetly.aplicacao.dados.AgendarManutencaoComando;
import com.assetly.aplicacao.dados.AlterarStatusBemComando;
import com.assetly.aplicacao.dados.CadastrarBemComando;
import com.assetly.aplicacao.dados.ConcluirManutencaoComando;
import com.assetly.aplicacao.dados.CriarAlertaComando;
import com.assetly.aplicacao.dados.EditarBemComando;
import com.assetly.aplicacao.dados.EditarDocumentoComando;
import com.assetly.aplicacao.dados.EditarGarantiaComando;
import com.assetly.aplicacao.dados.EditarManutencaoComando;
import com.assetly.aplicacao.dados.RegistrarDocumentoComando;
import com.assetly.aplicacao.dados.RegistrarGarantiaComando;
import com.assetly.apresentacao.ContextoAplicacao;
import com.assetly.dominio.alerta.Alerta;
import com.assetly.dominio.alerta.SeveridadeAlerta;
import com.assetly.dominio.alerta.StatusAlerta;
import com.assetly.dominio.alerta.TipoAlerta;
import com.assetly.dominio.bem.Bem;
import com.assetly.dominio.bem.StatusBem;
import com.assetly.dominio.bem.TipoBem;
import com.assetly.dominio.compartilhado.Dinheiro;
import com.assetly.dominio.documento.Documento;
import com.assetly.dominio.documento.StatusDocumento;
import com.assetly.dominio.documento.TipoDocumento;
import com.assetly.dominio.garantia.Garantia;
import com.assetly.dominio.garantia.StatusGarantia;
import com.assetly.dominio.garantia.TipoGarantia;
import com.assetly.dominio.manutencao.Manutencao;
import com.assetly.dominio.manutencao.StatusManutencao;
import com.assetly.dominio.manutencao.TipoManutencao;
import javafx.beans.property.ReadOnlyStringWrapper;
import javafx.collections.FXCollections;
import javafx.geometry.Insets;
import javafx.geometry.Pos;
import javafx.scene.Parent;
import javafx.scene.control.Button;
import javafx.scene.control.ButtonBar;
import javafx.scene.control.ButtonType;
import javafx.scene.control.ComboBox;
import javafx.scene.control.DatePicker;
import javafx.scene.control.Dialog;
import javafx.scene.control.DialogPane;
import javafx.scene.control.Label;
import javafx.scene.control.SelectionMode;
import javafx.scene.control.TableCell;
import javafx.scene.control.TableColumn;
import javafx.scene.control.TableRow;
import javafx.scene.control.TableView;
import javafx.scene.control.TextArea;
import javafx.scene.control.TextField;
import javafx.scene.control.ToggleButton;
import javafx.scene.control.ToggleGroup;
import javafx.scene.image.ImageView;
import javafx.scene.layout.BorderPane;
import javafx.scene.layout.FlowPane;
import javafx.scene.layout.GridPane;
import javafx.scene.layout.HBox;
import javafx.scene.layout.Priority;
import javafx.scene.layout.Region;
import javafx.scene.layout.StackPane;
import javafx.scene.layout.TilePane;
import javafx.scene.layout.VBox;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.format.DateTimeFormatter;
import java.util.List;
import java.util.Objects;
import java.util.Optional;
import java.util.concurrent.atomic.AtomicReference;
import java.util.function.Function;
import java.util.function.Supplier;

public final class TelaPrincipal {

    private static final DateTimeFormatter DATA = DateTimeFormatter.ofPattern("dd/MM/yyyy");

    private final ContextoAplicacao contexto;
    private final BorderPane raiz = new BorderPane();
    private final StackPane areaConteudo = new StackPane();
    private final TableView<Bem> tabelaBens = new TableView<>();
    private final TableView<Garantia> tabelaGarantias = new TableView<>();
    private final TableView<Manutencao> tabelaManutencoes = new TableView<>();
    private final TableView<Documento> tabelaDocumentos = new TableView<>();
    private final TableView<Alerta> tabelaAlertas = new TableView<>();
    private final HBox painelOperacao = new HBox(12);
    private final TilePane painelResumo = new TilePane();
    private final VBox painelAlertas = new VBox(8);
    private final VBox painelAcoes = new VBox(8);

    public TelaPrincipal(ContextoAplicacao contexto) {
        this.contexto = Objects.requireNonNull(contexto, "contexto da aplicação é obrigatório");
        configurarRaiz();
        configurarTabelas();
        atualizarTudo();
    }

    public Parent raiz() {
        return raiz;
    }

    public void atualizarTudo() {
        var bens = contexto.listarBens().executar();
        tabelaBens.setItems(FXCollections.observableArrayList(bens));
        tabelaGarantias.setItems(FXCollections.observableArrayList(contexto.listarGarantias().executar()));
        tabelaManutencoes.setItems(FXCollections.observableArrayList(contexto.listarManutencoes().executar()));
        tabelaDocumentos.setItems(FXCollections.observableArrayList(contexto.listarDocumentos().executar()));
        tabelaAlertas.setItems(FXCollections.observableArrayList(contexto.listarAlertas().executar()));
        atualizarPainel();
    }

    private void configurarRaiz() {
        raiz.getStyleClass().add("raiz-aplicacao");
        raiz.setTop(cabecalho());

        var itens = List.of(
                new ItemNavegacao("Painel", "Resumo operacional", criarPainel()),
                new ItemNavegacao("Bens", "Inventário e status", criarAbaBens()),
                new ItemNavegacao("Garantias", "Cobertura e vencimentos", criarAbaGarantias()),
                new ItemNavegacao("Manutenções", "Agenda e custos", criarAbaManutencoes()),
                new ItemNavegacao("Documentos", "Arquivos vinculados", criarAbaDocumentos()),
                new ItemNavegacao("Alertas", "Pendências abertas", criarAbaAlertas())
        );

        areaConteudo.getStyleClass().add("area-conteudo");

        var estrutura = new BorderPane();
        estrutura.getStyleClass().add("estrutura-principal");
        estrutura.setLeft(navegacaoLateral(itens));
        estrutura.setCenter(areaConteudo);
        raiz.setCenter(estrutura);

        selecionarConteudo(itens.getFirst().conteudo());
    }

    private Parent cabecalho() {
        var marca = marcaVisual();

        var titulo = new Label("Assetly");
        titulo.getStyleClass().add("titulo-aplicacao");

        var subtitulo = new Label("Gestão local de bens, garantias, documentos e manutenções.");
        subtitulo.getStyleClass().add("subtitulo-aplicacao");

        var textos = new VBox(2, titulo, subtitulo);
        var identidade = new HBox(12, marca, textos);
        identidade.setAlignment(Pos.CENTER_LEFT);
        identidade.getStyleClass().add("marca-aplicacao");

        var seloLocal = new Label("Local");
        seloLocal.getStyleClass().add("selo-identidade");
        var seloEscuro = new Label("Escuro fixo");
        seloEscuro.getStyleClass().add("selo-identidade");
        var selos = new HBox(8, seloLocal, seloEscuro);
        selos.setAlignment(Pos.CENTER_LEFT);

        var atualizar = new Button("Atualizar");
        atualizar.getStyleClass().add("botao-secundario");
        atualizar.setOnAction(evento -> executarComTratamento(this::atualizarTudo));

        var cabecalho = new HBox(18, identidade, espaco(), selos, atualizar);
        cabecalho.setAlignment(Pos.CENTER_LEFT);
        cabecalho.setPadding(new Insets(8, 24, 8, 24));
        cabecalho.getStyleClass().add("cabecalho-aplicacao");
        return cabecalho;
    }

    private Parent marcaVisual() {
        var recurso = TelaPrincipal.class.getResource("/imagens/assetly-icon-128.png");
        if (recurso == null) {
            var fallback = new Label("A");
            fallback.getStyleClass().add("marca-simbolo");
            return fallback;
        }

        var imagem = new ImageView(recurso.toExternalForm());
        imagem.setFitWidth(78);
        imagem.setFitHeight(78);
        imagem.setPreserveRatio(true);
        imagem.setSmooth(true);

        var caixa = new StackPane(imagem);
        caixa.getStyleClass().add("marca-icone");
        return caixa;
    }

    private Parent navegacaoLateral(List<ItemNavegacao> itens) {
        var titulo = new Label("Módulos");
        titulo.getStyleClass().add("titulo-menu");
        var descricao = new Label("Controle por área");
        descricao.getStyleClass().add("subtitulo-menu");
        var cabecalho = new VBox(2, titulo, descricao);
        cabecalho.getStyleClass().add("cabecalho-menu");

        var grupo = new ToggleGroup();
        var botoes = itens.stream()
                .map(item -> botaoNavegacao(item, grupo))
                .toList();
        botoes.getFirst().setSelected(true);

        var rodape = new Label("Base local SQLite\nDados privados no dispositivo");
        rodape.setWrapText(true);
        rodape.getStyleClass().add("rodape-menu");

        var menu = new VBox(10);
        menu.getStyleClass().add("menu-lateral");
        menu.getChildren().add(cabecalho);
        menu.getChildren().addAll(botoes);
        menu.getChildren().add(espacoVertical());
        menu.getChildren().add(rodape);
        return menu;
    }

    private ToggleButton botaoNavegacao(ItemNavegacao item, ToggleGroup grupo) {
        var titulo = new Label(item.titulo());
        titulo.getStyleClass().add("titulo-navegacao");
        var descricao = new Label(item.descricao());
        descricao.getStyleClass().add("descricao-navegacao");
        var textos = new VBox(2, titulo, descricao);

        var botao = new ToggleButton();
        botao.setGraphic(textos);
        botao.setMaxWidth(Double.MAX_VALUE);
        botao.setToggleGroup(grupo);
        botao.getStyleClass().add("botao-navegacao");
        botao.setOnAction(evento -> {
            if (!botao.isSelected()) {
                botao.setSelected(true);
            }
            selecionarConteudo(item.conteudo());
        });
        return botao;
    }

    private void selecionarConteudo(Parent conteudo) {
        areaConteudo.getChildren().setAll(conteudo);
    }

    private Parent criarPainel() {
        painelResumo.setHgap(12);
        painelResumo.setVgap(12);
        painelResumo.setPrefColumns(4);
        painelResumo.getStyleClass().add("painel-resumo");

        var tituloPainel = new Label("Visão geral");
        tituloPainel.getStyleClass().add("titulo-secao");
        var subtituloPainel = new Label("Indicadores críticos para acompanhar a saúde dos ativos.");
        subtituloPainel.getStyleClass().add("texto-suave");
        var cabecalhoPainel = new VBox(4, tituloPainel, subtituloPainel);
        cabecalhoPainel.getStyleClass().add("cabecalho-secao");

        painelOperacao.getStyleClass().add("painel-operacao");

        var tituloAlertas = new Label("Alertas abertos");
        tituloAlertas.getStyleClass().add("titulo-secao");
        painelAlertas.getStyleClass().add("lista-alertas");

        var tituloAcoes = new Label("Prioridades");
        tituloAcoes.getStyleClass().add("titulo-secao");
        painelAcoes.getStyleClass().add("lista-prioridades");

        var secaoAlertas = new VBox(10, tituloAlertas, painelAlertas);
        secaoAlertas.getStyleClass().add("secao-dashboard");
        var secaoAcoes = new VBox(10, tituloAcoes, painelAcoes);
        secaoAcoes.getStyleClass().add("secao-dashboard");

        var painelInferior = new HBox(14, secaoAlertas, secaoAcoes);
        painelInferior.getStyleClass().add("painel-inferior");
        HBox.setHgrow(secaoAlertas, Priority.ALWAYS);
        HBox.setHgrow(secaoAcoes, Priority.ALWAYS);

        var conteudo = new VBox(18, cabecalhoPainel, painelOperacao, painelResumo, painelInferior);
        conteudo.setPadding(new Insets(18));
        conteudo.getStyleClass().add("painel-dashboard");
        return conteudo;
    }

    private Parent criarAbaBens() {
        var barra = barraAcoes(
                botaoPrimario("Novo", () -> abrirDialogoBem(null)),
                botaoSecundario("Editar",
                        () -> abrirDialogoBem(selecionado(tabelaBens, "Selecione um bem para editar."))),
                botaoSecundario("Detalhes", this::mostrarDetalhesBem),
                botaoSecundario("Ativar", () -> alterarStatusBem(StatusBem.ATIVO)),
                botaoSecundario("Manutenção", () -> alterarStatusBem(StatusBem.EM_MANUTENCAO)),
                botaoPerigo("Descartar", () -> alterarStatusBem(StatusBem.DESCARTADO)),
                botaoPerigo("Arquivar", () -> alterarStatusBem(StatusBem.ARQUIVADO)));
        return telaComTabela("Bens", "Inventário dos ativos, status de uso e valor de compra.", barra, tabelaBens);
    }

    private Parent criarAbaGarantias() {
        var barra = barraAcoes(
                botaoPrimario("Nova", () -> abrirDialogoGarantia(null)),
                botaoSecundario("Editar", () -> abrirDialogoGarantia(
                        selecionado(tabelaGarantias, "Selecione uma garantia para editar."))));
        return telaComTabela("Garantias", "Coberturas cadastradas, fornecedores e vencimentos.", barra, tabelaGarantias);
    }

    private Parent criarAbaManutencoes() {
        var barra = barraAcoes(
                botaoPrimario("Nova", () -> abrirDialogoManutencao(null)),
                botaoSecundario("Editar",
                        () -> abrirDialogoManutencao(
                                selecionado(tabelaManutencoes, "Selecione uma manutenção para editar."))),
                botaoSecundario("Concluir", this::concluirManutencao),
                botaoPerigo("Cancelar", this::cancelarManutencao));
        return telaComTabela("Manutenções", "Agenda preventiva, corretiva, custos e conclusões.", barra, tabelaManutencoes);
    }

    private Parent criarAbaDocumentos() {
        var barra = barraAcoes(
                botaoPrimario("Novo", () -> abrirDialogoDocumento(null)),
                botaoSecundario("Editar",
                        () -> abrirDialogoDocumento(
                                selecionado(tabelaDocumentos, "Selecione um documento para editar."))),
                botaoSecundario("Marcar ausente", this::marcarDocumentoAusente),
                botaoPerigo("Arquivar", this::arquivarDocumento));
        return telaComTabela("Documentos", "Notas fiscais, comprovantes e arquivos ligados aos bens.", barra,
                tabelaDocumentos);
    }

    private Parent criarAbaAlertas() {
        var barra = barraAcoes(
                botaoPrimario("Novo", () -> abrirDialogoAlerta()),
                botaoSecundario("Ciente", this::tomarCienciaAlerta),
                botaoSecundario("Resolver", this::resolverAlerta));
        return telaComTabela("Alertas", "Pendências abertas e eventos que exigem acompanhamento.", barra, tabelaAlertas);
    }

    private Parent telaComTabela(String titulo, String descricao, Parent barra, TableView<?> tabela) {
        tabela.setColumnResizePolicy(TableView.CONSTRAINED_RESIZE_POLICY_FLEX_LAST_COLUMN);
        tabela.setFixedCellSize(42);
        tabela.setPlaceholder(new Label("Nenhum registro encontrado."));
        VBox.setVgrow(tabela, Priority.ALWAYS);
        var conteudo = new VBox(14, cabecalhoConteudo(titulo, descricao), barra, tabela);
        conteudo.setPadding(new Insets(18));
        conteudo.getStyleClass().add("conteudo-aba");
        return conteudo;
    }

    private Parent cabecalhoConteudo(String titulo, String descricao) {
        var tituloLabel = new Label(titulo);
        tituloLabel.getStyleClass().add("titulo-secao");
        var descricaoLabel = new Label(descricao);
        descricaoLabel.getStyleClass().add("texto-suave");
        var cabecalho = new VBox(4, tituloLabel, descricaoLabel);
        cabecalho.getStyleClass().add("cabecalho-conteudo");
        return cabecalho;
    }

    private void configurarTabelas() {
        configurarTabelaBens();
        configurarTabelaGarantias();
        configurarTabelaManutencoes();
        configurarTabelaDocumentos();
        configurarTabelaAlertas();
    }

    private void configurarTabelaBens() {
        tabelaBens.getSelectionModel().setSelectionMode(SelectionMode.SINGLE);
        tabelaBens.getColumns().setAll(List.of(
                coluna("Nome", bem -> bem.nome().valor()),
                coluna("Tipo", bem -> textoEnum(bem.tipo())),
                colunaEtiqueta("Status", bem -> textoEnum(bem.status()), bem -> classeEtiqueta(bem.status())),
                coluna("Compra", bem -> bem.compradoEm().map(this::data).orElse("")),
                coluna("Valor", bem -> bem.precoCompra().map(this::dinheiro).orElse("")),
                coluna("Observações", Bem::observacoes)));
        configurarLinhasTabela(tabelaBens, bem -> switch (bem.status()) {
            case ATIVO -> "linha-ok";
            case EM_MANUTENCAO -> "linha-aviso";
            case DESCARTADO -> "linha-perigo";
            case ARQUIVADO -> "linha-neutra";
        });
    }

    private void configurarTabelaGarantias() {
        tabelaGarantias.getSelectionModel().setSelectionMode(SelectionMode.SINGLE);
        tabelaGarantias.getColumns().setAll(List.of(
                coluna("Bem", garantia -> nomeBem(garantia.bemId())),
                coluna("Tipo", garantia -> textoEnum(garantia.tipo())),
                coluna("Fornecedor", Garantia::fornecedor),
                coluna("Início", garantia -> data(garantia.periodo().iniciaEm())),
                coluna("Fim", garantia -> data(garantia.periodo().terminaEm())),
                colunaEtiqueta(
                        "Status",
                        garantia -> textoEnum(garantia.statusEm(LocalDate.now())),
                        garantia -> classeEtiqueta(garantia.statusEm(LocalDate.now()))),
                coluna("Suporte", Garantia::contatoSuporte)));
        configurarLinhasTabela(tabelaGarantias, garantia -> switch (garantia.statusEm(LocalDate.now())) {
            case ATIVA -> "linha-ok";
            case PERTO_DO_VENCIMENTO, NAO_INICIADA -> "linha-aviso";
            case VENCIDA -> "linha-perigo";
        });
    }

    private void configurarTabelaManutencoes() {
        tabelaManutencoes.getSelectionModel().setSelectionMode(SelectionMode.SINGLE);
        tabelaManutencoes.getColumns().setAll(List.of(
                coluna("Bem", manutencao -> nomeBem(manutencao.bemId())),
                coluna("Tipo", manutencao -> textoEnum(manutencao.tipo())),
                coluna("Descrição", Manutencao::descricao),
                coluna("Agendada", manutencao -> data(manutencao.agendadaPara())),
                coluna("Concluída", manutencao -> manutencao.concluidaEm().map(this::data).orElse("")),
                coluna("Custo", manutencao -> manutencao.custo().map(this::dinheiro).orElse("")),
                colunaEtiqueta(
                        "Status",
                        manutencao -> textoEnum(manutencao.statusEm(LocalDate.now())),
                        manutencao -> classeEtiqueta(manutencao.statusEm(LocalDate.now())))));
        configurarLinhasTabela(tabelaManutencoes, manutencao -> switch (manutencao.statusEm(LocalDate.now())) {
            case CONCLUIDA -> "linha-ok";
            case AGENDADA -> "linha-informativa";
            case ATRASADA -> "linha-perigo";
            case CANCELADA -> "linha-neutra";
        });
    }

    private void configurarTabelaDocumentos() {
        tabelaDocumentos.getSelectionModel().setSelectionMode(SelectionMode.SINGLE);
        tabelaDocumentos.getColumns().setAll(List.of(
                coluna("Bem", documento -> nomeBem(documento.bemId())),
                coluna("Tipo", documento -> textoEnum(documento.tipo())),
                coluna("Nome", documento -> documento.nome().valor()),
                coluna("Caminho", documento -> documento.caminhoLocal().valor().toString()),
                coluna("Registrado", documento -> data(documento.registradoEm())),
                colunaEtiqueta("Status", documento -> textoEnum(documento.status()),
                        documento -> classeEtiqueta(documento.status()))));
        configurarLinhasTabela(tabelaDocumentos, documento -> switch (documento.status()) {
            case DISPONIVEL -> "linha-ok";
            case AUSENTE -> "linha-aviso";
            case ARQUIVADO -> "linha-neutra";
        });
    }

    private void configurarTabelaAlertas() {
        tabelaAlertas.getSelectionModel().setSelectionMode(SelectionMode.SINGLE);
        tabelaAlertas.getColumns().setAll(List.of(
                coluna("Bem", alerta -> nomeBem(alerta.bemId())),
                coluna("Tipo", alerta -> textoEnum(alerta.tipo())),
                colunaEtiqueta("Severidade", alerta -> textoEnum(alerta.severidade()),
                        alerta -> classeEtiqueta(alerta.severidade())),
                coluna("Mensagem", Alerta::mensagem),
                coluna("Criado", alerta -> data(alerta.criadoEm())),
                coluna("Prazo", alerta -> alerta.prazoEm().map(this::data).orElse("")),
                colunaEtiqueta("Status", alerta -> textoEnum(alerta.status()),
                        alerta -> classeEtiqueta(alerta.status()))));
        configurarLinhasTabela(tabelaAlertas, alerta -> {
            if (alerta.status() == StatusAlerta.RESOLVIDO) {
                return "linha-ok";
            }
            return switch (alerta.severidade()) {
                case CRITICO -> "linha-perigo";
                case AVISO -> "linha-aviso";
                case INFORMATIVO -> "linha-informativa";
            };
        });
    }

    private <T> TableColumn<T, String> coluna(String titulo, Function<T, String> valor) {
        var coluna = new TableColumn<T, String>(titulo);
        coluna.setCellValueFactory(dados -> new ReadOnlyStringWrapper(valor.apply(dados.getValue())));
        return coluna;
    }

    private <T> TableColumn<T, String> colunaEtiqueta(String titulo, Function<T, String> valor,
            Function<T, String> classeCss) {
        var coluna = coluna(titulo, valor);
        coluna.setCellFactory(tabela -> new TableCell<>() {
            @Override
            protected void updateItem(String item, boolean vazio) {
                super.updateItem(item, vazio);
                setText(null);
                setGraphic(null);
                if (vazio || item == null || item.isBlank()) {
                    return;
                }

                var etiqueta = new Label(item);
                etiqueta.getStyleClass().add("etiqueta");
                var linha = getTableRow() == null ? null : getTableRow().getItem();
                if (linha == null && getTableView() != null && getIndex() >= 0
                        && getIndex() < getTableView().getItems().size()) {
                    linha = getTableView().getItems().get(getIndex());
                }
                if (linha != null) {
                    var classe = classeCss.apply(linha);
                    if (classe != null && !classe.isBlank()) {
                        etiqueta.getStyleClass().add(classe);
                    }
                }
                setGraphic(etiqueta);
            }
        });
        return coluna;
    }

    private <T> void configurarLinhasTabela(TableView<T> tabela, Function<T, String> classeCss) {
        tabela.setRowFactory(controle -> new TableRow<>() {
            @Override
            protected void updateItem(T item, boolean vazio) {
                super.updateItem(item, vazio);
                getStyleClass().removeAll("linha-ok", "linha-informativa", "linha-aviso", "linha-perigo",
                        "linha-neutra");
                if (!vazio && item != null) {
                    var classe = classeCss.apply(item);
                    if (classe != null && !classe.isBlank()) {
                        getStyleClass().add(classe);
                    }
                }
            }
        });
    }

    private void atualizarPainel() {
        var bens = contexto.listarBens().executar();
        var garantias = contexto.listarGarantias().executar();
        var manutencoes = contexto.listarManutencoes().executar();
        var documentos = contexto.listarDocumentos().executar();
        var alertas = contexto.listarAlertas().executar();
        var hoje = LocalDate.now();
        var garantiasVencidas = contar(garantias, garantia -> garantia.statusEm(hoje) == StatusGarantia.VENCIDA);
        var garantiasProximas = contar(garantias,
                garantia -> garantia.statusEm(hoje) == StatusGarantia.PERTO_DO_VENCIMENTO);
        var manutencoesAtrasadas = contar(manutencoes,
                manutencao -> manutencao.statusEm(hoje) == StatusManutencao.ATRASADA);
        var documentosAusentes = contar(documentos, documento -> documento.status() == StatusDocumento.AUSENTE);
        var alertasAbertos = contar(alertas, alerta -> alerta.status() != StatusAlerta.RESOLVIDO);
        var alertasCriticos = contar(alertas,
                alerta -> alerta.status() != StatusAlerta.RESOLVIDO && alerta.severidade() == SeveridadeAlerta.CRITICO);
        var pontosAtencao = garantiasVencidas + garantiasProximas + manutencoesAtrasadas + documentosAusentes
                + alertasAbertos;

        painelOperacao.getChildren().setAll(
                resumoOperacao("Atenção necessária", String.valueOf(pontosAtencao),
                        pontosAtencao == 0 ? "sem pendências críticas" : "itens pedem revisão",
                        pontosAtencao == 0 ? "resumo-ok" : "resumo-perigo"),
                resumoOperacao("Alertas críticos", String.valueOf(alertasCriticos),
                        alertasCriticos == 1 ? "alerta crítico aberto" : "alertas críticos abertos",
                        alertasCriticos == 0 ? "resumo-ok" : "resumo-perigo"),
                resumoOperacao("Base local", String.valueOf(bens.size() + documentos.size()),
                        "registros sob controle", "resumo-informativo"));

        painelResumo.getChildren().setAll(
                indicador("Bens monitorados", String.valueOf(bens.size()), "indicador-neutro"),
                indicador("Garantias vencidas", String.valueOf(garantiasVencidas), "indicador-perigo"),
                indicador("Garantias próximas", String.valueOf(garantiasProximas), "indicador-aviso"),
                indicador("Manutenções atrasadas", String.valueOf(manutencoesAtrasadas), "indicador-perigo"),
                indicador("Documentos ausentes", String.valueOf(documentosAusentes), "indicador-aviso"),
                indicador("Alertas abertos", String.valueOf(alertasAbertos), "indicador-informativo"));

        painelAlertas.getChildren().clear();
        alertas.stream()
                .filter(alerta -> alerta.status() != StatusAlerta.RESOLVIDO)
                .limit(8)
                .map(this::alertaResumo)
                .forEach(painelAlertas.getChildren()::add);
        if (painelAlertas.getChildren().isEmpty()) {
            var vazio = new Label("Nenhum alerta aberto.");
            vazio.getStyleClass().add("texto-suave");
            painelAlertas.getChildren().add(vazio);
        }

        painelAcoes.getChildren().clear();
        garantias.stream()
                .filter(garantia -> garantia.statusEm(hoje) == StatusGarantia.VENCIDA
                        || garantia.statusEm(hoje) == StatusGarantia.PERTO_DO_VENCIMENTO)
                .limit(2)
                .map(garantia -> prioridadeResumo("Garantia", nomeBem(garantia.bemId()),
                        "fim em " + data(garantia.periodo().terminaEm()),
                        classeEtiqueta(garantia.statusEm(hoje))))
                .forEach(painelAcoes.getChildren()::add);
        manutencoes.stream()
                .filter(manutencao -> manutencao.statusEm(hoje) == StatusManutencao.ATRASADA)
                .limit(2)
                .map(manutencao -> prioridadeResumo("Manutenção", nomeBem(manutencao.bemId()),
                        manutencao.descricao() + " - agendada para " + data(manutencao.agendadaPara()),
                        classeEtiqueta(StatusManutencao.ATRASADA)))
                .forEach(painelAcoes.getChildren()::add);
        documentos.stream()
                .filter(documento -> documento.status() == StatusDocumento.AUSENTE)
                .limit(2)
                .map(documento -> prioridadeResumo("Documento", nomeBem(documento.bemId()),
                        documento.nome().valor(), classeEtiqueta(StatusDocumento.AUSENTE)))
                .forEach(painelAcoes.getChildren()::add);
        alertas.stream()
                .filter(alerta -> alerta.status() != StatusAlerta.RESOLVIDO)
                .filter(alerta -> alerta.severidade() == SeveridadeAlerta.CRITICO
                        || alerta.severidade() == SeveridadeAlerta.AVISO)
                .limit(2)
                .map(alerta -> prioridadeResumo("Alerta", nomeBem(alerta.bemId()),
                        alerta.mensagem(), classeEtiqueta(alerta.severidade())))
                .forEach(painelAcoes.getChildren()::add);
        if (painelAcoes.getChildren().isEmpty()) {
            var vazio = new Label("Nenhuma prioridade no momento.");
            vazio.getStyleClass().add("texto-suave");
            painelAcoes.getChildren().add(vazio);
        }
    }

    private Parent alertaResumo(Alerta alerta) {
        var severidade = new Label(textoEnum(alerta.severidade()));
        severidade.getStyleClass().addAll("etiqueta", classeEtiqueta(alerta.severidade()));

        var mensagem = new Label(nomeBem(alerta.bemId()) + " - " + alerta.mensagem());
        mensagem.getStyleClass().add("linha-alerta-texto");
        mensagem.setMaxWidth(Double.MAX_VALUE);
        mensagem.setWrapText(true);
        HBox.setHgrow(mensagem, Priority.ALWAYS);

        var linha = new HBox(10, severidade, mensagem);
        linha.setAlignment(Pos.CENTER_LEFT);
        linha.getStyleClass().add("linha-alerta");
        return linha;
    }

    private Parent resumoOperacao(String titulo, String valor, String detalhe, String classeCss) {
        var tituloLabel = new Label(titulo);
        tituloLabel.getStyleClass().add("resumo-operacao-titulo");
        var valorLabel = new Label(valor);
        valorLabel.getStyleClass().add("resumo-operacao-valor");
        var detalheLabel = new Label(detalhe);
        detalheLabel.getStyleClass().add("resumo-operacao-detalhe");

        var caixa = new VBox(3, tituloLabel, valorLabel, detalheLabel);
        caixa.getStyleClass().addAll("resumo-operacao", classeCss);
        HBox.setHgrow(caixa, Priority.ALWAYS);
        return caixa;
    }

    private Parent prioridadeResumo(String categoria, String titulo, String detalhe, String classeCss) {
        var categoriaLabel = new Label(categoria);
        categoriaLabel.getStyleClass().addAll("etiqueta", classeCss);

        var tituloLabel = new Label(titulo);
        tituloLabel.getStyleClass().add("prioridade-titulo");
        var detalheLabel = new Label(detalhe);
        detalheLabel.getStyleClass().add("prioridade-detalhe");
        detalheLabel.setWrapText(true);
        var textos = new VBox(2, tituloLabel, detalheLabel);
        textos.setMaxWidth(Double.MAX_VALUE);
        HBox.setHgrow(textos, Priority.ALWAYS);

        var linha = new HBox(10, categoriaLabel, textos);
        linha.setAlignment(Pos.CENTER_LEFT);
        linha.getStyleClass().add("linha-prioridade");
        return linha;
    }

    private Parent indicador(String titulo, String valor, String classeCss) {
        var tituloLabel = new Label(titulo);
        tituloLabel.getStyleClass().add("indicador-titulo");
        var valorLabel = new Label(valor);
        valorLabel.getStyleClass().add("indicador-valor");
        var caixa = new VBox(4, tituloLabel, valorLabel);
        caixa.getStyleClass().addAll("indicador", classeCss);
        return caixa;
    }

    private FlowPane barraAcoes(Button... botoes) {
        var barra = new FlowPane(8, 8);
        barra.getChildren().addAll(botoes);
        barra.setAlignment(Pos.CENTER_LEFT);
        barra.getStyleClass().add("barra-acoes");
        return barra;
    }

    private Button botaoPrimario(String texto, Runnable acao) {
        var botao = new Button(texto);
        botao.getStyleClass().add("botao-primario");
        botao.setOnAction(evento -> executarComTratamento(acao));
        return botao;
    }

    private Button botaoSecundario(String texto, Runnable acao) {
        var botao = new Button(texto);
        botao.getStyleClass().add("botao-secundario");
        botao.setOnAction(evento -> executarComTratamento(acao));
        return botao;
    }

    private Button botaoPerigo(String texto, Runnable acao) {
        var botao = new Button(texto);
        botao.getStyleClass().add("botao-perigo");
        botao.setOnAction(evento -> executarComTratamento(acao));
        return botao;
    }

    private void abrirDialogoBem(Bem bem) {
        var nome = campoTexto("Nome", bem == null ? "" : bem.nome().valor());
        var tipo = combo(TipoBem.values(), bem == null ? TipoBem.ELETRONICO : bem.tipo());
        var compradoEm = dataPicker(bem == null ? null : bem.compradoEm().orElse(null));
        var preco = campoTexto("Preço",
                bem == null ? "" : bem.precoCompra().map(valor -> valor.valor().toPlainString()).orElse(""));
        var moeda = campoTexto("Moeda",
                bem == null ? "BRL" : bem.precoCompra().map(valor -> valor.moeda().getCurrencyCode()).orElse("BRL"));
        var observacoes = areaTexto(bem == null ? "" : bem.observacoes());
        var grade = gradeFormulario();
        adicionarLinha(grade, 0, "Nome", nome);
        adicionarLinha(grade, 1, "Tipo", tipo);
        adicionarLinha(grade, 2, "Compra", compradoEm);
        adicionarLinha(grade, 3, "Preço", preco);
        adicionarLinha(grade, 4, "Moeda", moeda);
        adicionarLinha(grade, 5, "Observações", observacoes);

        mostrarFormulario(bem == null ? "Novo bem" : "Editar bem", grade, () -> {
            if (bem == null) {
                return contexto.cadastrarBem().executar(new CadastrarBemComando(
                        nome.getText(),
                        tipo.getValue(),
                        compradoEm.getValue(),
                        decimalOpcional(preco.getText()),
                        textoOpcional(moeda.getText()),
                        observacoes.getText()));
            }
            return contexto.editarBem().executar(new EditarBemComando(
                    bem.id(),
                    nome.getText(),
                    tipo.getValue(),
                    compradoEm.getValue(),
                    decimalOpcional(preco.getText()),
                    textoOpcional(moeda.getText()),
                    observacoes.getText()));
        }).ifPresent(resultado -> atualizarTudo());
    }

    private void abrirDialogoGarantia(Garantia garantia) {
        var bem = comboBens(garantia == null ? null : contexto.buscarBem().executar(garantia.bemId()));
        bem.setDisable(garantia != null);
        var tipo = combo(TipoGarantia.values(), garantia == null ? TipoGarantia.FABRICANTE : garantia.tipo());
        var fornecedor = campoTexto("Fornecedor", garantia == null ? "" : garantia.fornecedor());
        var iniciaEm = dataPicker(garantia == null ? LocalDate.now() : garantia.periodo().iniciaEm());
        var terminaEm = dataPicker(garantia == null ? LocalDate.now().plusYears(1) : garantia.periodo().terminaEm());
        var suporte = campoTexto("Contato de suporte", garantia == null ? "" : garantia.contatoSuporte());
        var grade = gradeFormulario();
        adicionarLinha(grade, 0, "Bem", bem);
        adicionarLinha(grade, 1, "Tipo", tipo);
        adicionarLinha(grade, 2, "Fornecedor", fornecedor);
        adicionarLinha(grade, 3, "Início", iniciaEm);
        adicionarLinha(grade, 4, "Fim", terminaEm);
        adicionarLinha(grade, 5, "Suporte", suporte);

        mostrarFormulario(garantia == null ? "Nova garantia" : "Editar garantia", grade, () -> {
            if (garantia == null) {
                return contexto.registrarGarantia().executar(new RegistrarGarantiaComando(
                        exigirBemSelecionado(bem).id(),
                        tipo.getValue(),
                        fornecedor.getText(),
                        dataObrigatoria(iniciaEm, "início"),
                        dataObrigatoria(terminaEm, "fim"),
                        suporte.getText()));
            }
            return contexto.editarGarantia().executar(new EditarGarantiaComando(
                    garantia.id(),
                    tipo.getValue(),
                    fornecedor.getText(),
                    dataObrigatoria(iniciaEm, "início"),
                    dataObrigatoria(terminaEm, "fim"),
                    suporte.getText()));
        }).ifPresent(resultado -> atualizarTudo());
    }

    private void abrirDialogoManutencao(Manutencao manutencao) {
        var bem = comboBens(manutencao == null ? null : contexto.buscarBem().executar(manutencao.bemId()));
        bem.setDisable(manutencao != null);
        var tipo = combo(TipoManutencao.values(), manutencao == null ? TipoManutencao.PREVENTIVA : manutencao.tipo());
        var descricao = campoTexto("Descrição", manutencao == null ? "" : manutencao.descricao());
        var data = dataPicker(manutencao == null ? LocalDate.now() : manutencao.agendadaPara());
        var grade = gradeFormulario();
        adicionarLinha(grade, 0, "Bem", bem);
        adicionarLinha(grade, 1, "Tipo", tipo);
        adicionarLinha(grade, 2, "Descrição", descricao);
        adicionarLinha(grade, 3, "Agendada para", data);

        mostrarFormulario(manutencao == null ? "Nova manutenção" : "Editar manutenção", grade, () -> {
            if (manutencao == null) {
                return contexto.agendarManutencao().executar(new AgendarManutencaoComando(
                        exigirBemSelecionado(bem).id(),
                        tipo.getValue(),
                        descricao.getText(),
                        dataObrigatoria(data, "data agendada")));
            }
            return contexto.editarManutencao().executar(new EditarManutencaoComando(
                    manutencao.id(),
                    tipo.getValue(),
                    descricao.getText(),
                    dataObrigatoria(data, "data agendada")));
        }).ifPresent(resultado -> atualizarTudo());
    }

    private void abrirDialogoDocumento(Documento documento) {
        var bem = comboBens(documento == null ? null : contexto.buscarBem().executar(documento.bemId()));
        bem.setDisable(documento != null);
        var tipo = combo(TipoDocumento.values(), documento == null ? TipoDocumento.NOTA_FISCAL : documento.tipo());
        var nome = campoTexto("Nome", documento == null ? "" : documento.nome().valor());
        var caminho = campoTexto("Caminho local", documento == null ? "" : documento.caminhoLocal().valor().toString());
        var registradoEm = dataPicker(documento == null ? LocalDate.now() : documento.registradoEm());
        var grade = gradeFormulario();
        adicionarLinha(grade, 0, "Bem", bem);
        adicionarLinha(grade, 1, "Tipo", tipo);
        adicionarLinha(grade, 2, "Nome", nome);
        adicionarLinha(grade, 3, "Caminho", caminho);
        adicionarLinha(grade, 4, "Registrado em", registradoEm);

        mostrarFormulario(documento == null ? "Novo documento" : "Editar documento", grade, () -> {
            if (documento == null) {
                return contexto.registrarDocumento().executar(new RegistrarDocumentoComando(
                        exigirBemSelecionado(bem).id(),
                        tipo.getValue(),
                        nome.getText(),
                        caminho.getText(),
                        dataObrigatoria(registradoEm, "registro")));
            }
            return contexto.editarDocumento().executar(new EditarDocumentoComando(
                    documento.id(),
                    tipo.getValue(),
                    nome.getText(),
                    caminho.getText(),
                    dataObrigatoria(registradoEm, "registro")));
        }).ifPresent(resultado -> atualizarTudo());
    }

    private void abrirDialogoAlerta() {
        var bem = comboBens(null);
        var tipo = combo(TipoAlerta.values(), TipoAlerta.GARANTIA_VENCENDO);
        var severidade = combo(SeveridadeAlerta.values(), SeveridadeAlerta.AVISO);
        var mensagem = campoTexto("Mensagem", "");
        var criadoEm = dataPicker(LocalDate.now());
        var prazoEm = dataPicker(null);
        var grade = gradeFormulario();
        adicionarLinha(grade, 0, "Bem", bem);
        adicionarLinha(grade, 1, "Tipo", tipo);
        adicionarLinha(grade, 2, "Severidade", severidade);
        adicionarLinha(grade, 3, "Mensagem", mensagem);
        adicionarLinha(grade, 4, "Criado em", criadoEm);
        adicionarLinha(grade, 5, "Prazo", prazoEm);

        mostrarFormulario("Novo alerta", grade, () -> contexto.criarAlerta().executar(new CriarAlertaComando(
                exigirBemSelecionado(bem).id(),
                tipo.getValue(),
                severidade.getValue(),
                mensagem.getText(),
                dataObrigatoria(criadoEm, "criação"),
                prazoEm.getValue()))).ifPresent(resultado -> atualizarTudo());
    }

    private void concluirManutencao() {
        var manutencao = selecionado(tabelaManutencoes, "Selecione uma manutenção para concluir.");
        var concluidaEm = dataPicker(LocalDate.now());
        var custo = campoTexto("Custo", "");
        var moeda = campoTexto("Moeda", "BRL");
        var grade = gradeFormulario();
        adicionarLinha(grade, 0, "Concluída em", concluidaEm);
        adicionarLinha(grade, 1, "Custo", custo);
        adicionarLinha(grade, 2, "Moeda", moeda);

        mostrarFormulario("Concluir manutenção", grade,
                () -> contexto.concluirManutencao().executar(new ConcluirManutencaoComando(
                        manutencao.id(),
                        dataObrigatoria(concluidaEm, "conclusão"),
                        decimalOpcional(custo.getText()),
                        textoOpcional(moeda.getText()))))
                .ifPresent(resultado -> atualizarTudo());
    }

    private void cancelarManutencao() {
        var manutencao = selecionado(tabelaManutencoes, "Selecione uma manutenção para cancelar.");
        contexto.cancelarManutencao().executar(manutencao.id());
        atualizarTudo();
    }

    private void marcarDocumentoAusente() {
        var documento = selecionado(tabelaDocumentos, "Selecione um documento.");
        contexto.marcarDocumentoAusente().executar(documento.id());
        atualizarTudo();
    }

    private void arquivarDocumento() {
        var documento = selecionado(tabelaDocumentos, "Selecione um documento.");
        contexto.arquivarDocumento().executar(documento.id());
        atualizarTudo();
    }

    private void tomarCienciaAlerta() {
        var alerta = selecionado(tabelaAlertas, "Selecione um alerta.");
        contexto.tomarCienciaAlerta().executar(alerta.id());
        atualizarTudo();
    }

    private void resolverAlerta() {
        var alerta = selecionado(tabelaAlertas, "Selecione um alerta.");
        contexto.resolverAlerta().executar(alerta.id());
        atualizarTudo();
    }

    private void alterarStatusBem(StatusBem status) {
        var bem = selecionado(tabelaBens, "Selecione um bem.");
        contexto.alterarStatusBem().executar(new AlterarStatusBemComando(bem.id(), status));
        atualizarTudo();
    }

    private void mostrarDetalhesBem() {
        var bem = selecionado(tabelaBens, "Selecione um bem.");
        var garantias = contexto.repositorioGarantia().listarPorBem(bem.id()).size();
        var manutencoes = contexto.repositorioManutencao().listarPorBem(bem.id()).size();
        var documentos = contexto.repositorioDocumento().listarPorBem(bem.id()).size();
        var alertas = contexto.repositorioAlerta().listarPorBem(bem.id()).stream()
                .filter(alerta -> alerta.status() != StatusAlerta.RESOLVIDO)
                .count();
        var mensagem = """
                Nome: %s
                Tipo: %s
                Status: %s
                Compra: %s
                Valor: %s
                Observações: %s

                Garantias: %d
                Manutenções: %d
                Documentos: %d
                Alertas abertos: %d
                """.formatted(
                bem.nome().valor(),
                textoEnum(bem.tipo()),
                textoEnum(bem.status()),
                bem.compradoEm().map(this::data).orElse(""),
                bem.precoCompra().map(this::dinheiro).orElse(""),
                bem.observacoes(),
                garantias,
                manutencoes,
                documentos,
                alertas);
        mostrarInformacao("Detalhes do bem", mensagem);
    }

    private <T> Optional<T> mostrarFormulario(String titulo, Parent conteudo, Supplier<T> fornecedorResultado) {
        var salvar = new ButtonType("Salvar", ButtonBar.ButtonData.OK_DONE);
        var dialogo = new Dialog<T>();
        dialogo.setTitle(titulo);
        dialogo.getDialogPane().getButtonTypes().addAll(salvar, ButtonType.CANCEL);
        dialogo.getDialogPane().setContent(conteudo);
        aplicarEstilo(dialogo.getDialogPane());

        var resultado = new AtomicReference<T>();
        var botaoSalvar = (Button) dialogo.getDialogPane().lookupButton(salvar);
        botaoSalvar.getStyleClass().add("botao-primario");
        var botaoCancelar = (Button) dialogo.getDialogPane().lookupButton(ButtonType.CANCEL);
        botaoCancelar.getStyleClass().add("botao-secundario");
        botaoSalvar.addEventFilter(javafx.event.ActionEvent.ACTION, evento -> {
            try {
                resultado.set(fornecedorResultado.get());
            } catch (RuntimeException excecao) {
                evento.consume();
                mostrarErro(excecao);
            }
        });
        dialogo.setResultConverter(botao -> botao == salvar ? resultado.get() : null);
        return dialogo.showAndWait();
    }

    private void aplicarEstilo(DialogPane painel) {
        var css = TelaPrincipal.class.getResource("/estilos/assetly.css");
        if (css != null) {
            painel.getStylesheets().add(css.toExternalForm());
        }
        painel.getStyleClass().add("dialogo-formulario");
    }

    private ComboBox<ItemBem> comboBens(Bem selecionado) {
        var combo = new ComboBox<ItemBem>();
        var itens = contexto.listarBens().executar().stream().map(ItemBem::new).toList();
        combo.setItems(FXCollections.observableArrayList(itens));
        combo.setMaxWidth(Double.MAX_VALUE);
        if (selecionado != null) {
            itens.stream()
                    .filter(item -> item.bem().id().equals(selecionado.id()))
                    .findFirst()
                    .ifPresent(combo::setValue);
        } else if (!itens.isEmpty()) {
            combo.setValue(itens.getFirst());
        }
        return combo;
    }

    private Bem exigirBemSelecionado(ComboBox<ItemBem> combo) {
        var item = combo.getValue();
        if (item == null) {
            throw new IllegalArgumentException("selecione um bem");
        }
        return item.bem();
    }

    private <T> ComboBox<T> combo(T[] valores, T selecionado) {
        var combo = new ComboBox<T>(FXCollections.observableArrayList(valores));
        combo.setValue(selecionado);
        combo.setMaxWidth(Double.MAX_VALUE);
        return combo;
    }

    private TextField campoTexto(String prompt, String valor) {
        var campo = new TextField(valor == null ? "" : valor);
        campo.setPromptText(prompt);
        campo.setMaxWidth(Double.MAX_VALUE);
        return campo;
    }

    private TextArea areaTexto(String valor) {
        var area = new TextArea(valor == null ? "" : valor);
        area.setPrefRowCount(3);
        area.setWrapText(true);
        return area;
    }

    private DatePicker dataPicker(LocalDate valor) {
        var campo = new DatePicker(valor);
        campo.setMaxWidth(Double.MAX_VALUE);
        return campo;
    }

    private GridPane gradeFormulario() {
        var grade = new GridPane();
        grade.setHgap(10);
        grade.setVgap(10);
        grade.setPadding(new Insets(8));
        return grade;
    }

    private void adicionarLinha(GridPane grade, int linha, String rotulo, Parent campo) {
        var label = new Label(rotulo);
        label.getStyleClass().add("rotulo-formulario");
        grade.add(label, 0, linha);
        grade.add(campo, 1, linha);
        GridPane.setHgrow(campo, Priority.ALWAYS);
    }

    private <T> T selecionado(TableView<T> tabela, String mensagem) {
        var item = tabela.getSelectionModel().getSelectedItem();
        if (item == null) {
            throw new IllegalStateException(mensagem);
        }
        return item;
    }

    private void executarComTratamento(Runnable acao) {
        try {
            acao.run();
        } catch (RuntimeException excecao) {
            mostrarErro(excecao);
        }
    }

    private void mostrarErro(RuntimeException excecao) {
        var alerta = new javafx.scene.control.Alert(javafx.scene.control.Alert.AlertType.ERROR);
        alerta.setTitle("Erro");
        alerta.setHeaderText("Não foi possível concluir a ação");
        alerta.setContentText(excecao.getMessage());
        aplicarEstilo(alerta.getDialogPane());
        alerta.getDialogPane().getStyleClass().add("dialogo-erro");
        alerta.showAndWait();
    }

    private void mostrarInformacao(String titulo, String mensagem) {
        var alerta = new javafx.scene.control.Alert(javafx.scene.control.Alert.AlertType.INFORMATION);
        alerta.setTitle(titulo);
        alerta.setHeaderText(null);
        alerta.setContentText(mensagem);
        aplicarEstilo(alerta.getDialogPane());
        alerta.getDialogPane().getStyleClass().add("dialogo-informacao");
        alerta.showAndWait();
    }

    private String nomeBem(com.assetly.dominio.bem.BemId bemId) {
        return contexto.repositorioBem().buscarPorId(bemId)
                .map(bem -> bem.nome().valor())
                .orElse("Bem não encontrado");
    }

    private String data(LocalDate data) {
        return data == null ? "" : DATA.format(data);
    }

    private String dinheiro(Dinheiro dinheiro) {
        return dinheiro.moeda().getCurrencyCode() + " " + dinheiro.valor().toPlainString();
    }

    private String textoEnum(Enum<?> valor) {
        return valor.name().toLowerCase().replace('_', ' ');
    }

    private String classeEtiqueta(Enum<?> valor) {
        return "etiqueta-" + valor.name().toLowerCase().replace('_', '-');
    }

    private String textoOpcional(String valor) {
        return valor == null || valor.isBlank() ? null : valor.trim();
    }

    private BigDecimal decimalOpcional(String valor) {
        if (valor == null || valor.isBlank()) {
            return null;
        }
        return new BigDecimal(valor.trim().replace(',', '.'));
    }

    private LocalDate dataObrigatoria(DatePicker campo, String nome) {
        if (campo.getValue() == null) {
            throw new IllegalArgumentException("data de " + nome + " é obrigatória");
        }
        return campo.getValue();
    }

    private Region espaco() {
        var espaco = new Region();
        HBox.setHgrow(espaco, Priority.ALWAYS);
        return espaco;
    }

    private Region espacoVertical() {
        var espaco = new Region();
        VBox.setVgrow(espaco, Priority.ALWAYS);
        return espaco;
    }

    private <T> long contar(List<T> itens, java.util.function.Predicate<T> predicado) {
        return itens.stream().filter(predicado).count();
    }

    private record ItemBem(Bem bem) {

        @Override
        public String toString() {
            return bem.nome().valor();
        }
    }

    private record ItemNavegacao(String titulo, String descricao, Parent conteudo) {
    }
}
