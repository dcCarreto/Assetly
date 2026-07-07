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
import javafx.scene.control.Tab;
import javafx.scene.control.TabPane;
import javafx.scene.control.TableColumn;
import javafx.scene.control.TableRow;
import javafx.scene.control.TableView;
import javafx.scene.control.TextArea;
import javafx.scene.control.TextField;
import javafx.scene.layout.BorderPane;
import javafx.scene.layout.GridPane;
import javafx.scene.layout.HBox;
import javafx.scene.layout.Priority;
import javafx.scene.layout.Region;
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
    private final TableView<Bem> tabelaBens = new TableView<>();
    private final TableView<Garantia> tabelaGarantias = new TableView<>();
    private final TableView<Manutencao> tabelaManutencoes = new TableView<>();
    private final TableView<Documento> tabelaDocumentos = new TableView<>();
    private final TableView<Alerta> tabelaAlertas = new TableView<>();
    private final TilePane painelResumo = new TilePane();
    private final VBox painelAlertas = new VBox(8);

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

        var abas = new TabPane();
        abas.setTabClosingPolicy(TabPane.TabClosingPolicy.UNAVAILABLE);
        abas.getTabs().add(new Tab("Painel", criarPainel()));
        abas.getTabs().add(new Tab("Bens", criarAbaBens()));
        abas.getTabs().add(new Tab("Garantias", criarAbaGarantias()));
        abas.getTabs().add(new Tab("Manutenções", criarAbaManutencoes()));
        abas.getTabs().add(new Tab("Documentos", criarAbaDocumentos()));
        abas.getTabs().add(new Tab("Alertas", criarAbaAlertas()));
        raiz.setCenter(abas);
    }

    private Parent cabecalho() {
        var titulo = new Label("Assetly");
        titulo.getStyleClass().add("titulo-aplicacao");

        var subtitulo = new Label("Bens, garantias, documentos e manutenções em um aplicativo local.");
        subtitulo.getStyleClass().add("subtitulo-aplicacao");

        var atualizar = new Button("Atualizar");
        atualizar.getStyleClass().add("botao-secundario");
        atualizar.setOnAction(evento -> executarComTratamento(this::atualizarTudo));

        var textos = new VBox(2, titulo, subtitulo);
        var cabecalho = new HBox(16, textos, espaco(), atualizar);
        cabecalho.setAlignment(Pos.CENTER_LEFT);
        cabecalho.setPadding(new Insets(18, 22, 14, 22));
        cabecalho.getStyleClass().add("cabecalho-aplicacao");
        return cabecalho;
    }

    private Parent criarPainel() {
        painelResumo.setHgap(12);
        painelResumo.setVgap(12);
        painelResumo.setPrefColumns(4);

        var tituloAlertas = new Label("Alertas abertos");
        tituloAlertas.getStyleClass().add("titulo-secao");
        painelAlertas.getStyleClass().add("lista-alertas");

        var conteudo = new VBox(18, painelResumo, tituloAlertas, painelAlertas);
        conteudo.setPadding(new Insets(18));
        return conteudo;
    }

    private Parent criarAbaBens() {
        var barra = barraAcoes(
                botaoPrimario("Novo", () -> abrirDialogoBem(null)),
                botaoSecundario("Editar", () -> abrirDialogoBem(selecionado(tabelaBens, "Selecione um bem para editar."))),
                botaoSecundario("Detalhes", this::mostrarDetalhesBem),
                botaoSecundario("Ativar", () -> alterarStatusBem(StatusBem.ATIVO)),
                botaoSecundario("Manutenção", () -> alterarStatusBem(StatusBem.EM_MANUTENCAO)),
                botaoPerigo("Descartar", () -> alterarStatusBem(StatusBem.DESCARTADO)),
                botaoPerigo("Arquivar", () -> alterarStatusBem(StatusBem.ARQUIVADO))
        );
        return telaComTabela(barra, tabelaBens);
    }

    private Parent criarAbaGarantias() {
        var barra = barraAcoes(
                botaoPrimario("Nova", () -> abrirDialogoGarantia(null)),
                botaoSecundario("Editar", () -> abrirDialogoGarantia(selecionado(tabelaGarantias, "Selecione uma garantia para editar.")))
        );
        return telaComTabela(barra, tabelaGarantias);
    }

    private Parent criarAbaManutencoes() {
        var barra = barraAcoes(
                botaoPrimario("Nova", () -> abrirDialogoManutencao(null)),
                botaoSecundario("Editar", () -> abrirDialogoManutencao(selecionado(tabelaManutencoes, "Selecione uma manutenção para editar."))),
                botaoSecundario("Concluir", this::concluirManutencao),
                botaoPerigo("Cancelar", this::cancelarManutencao)
        );
        return telaComTabela(barra, tabelaManutencoes);
    }

    private Parent criarAbaDocumentos() {
        var barra = barraAcoes(
                botaoPrimario("Novo", () -> abrirDialogoDocumento(null)),
                botaoSecundario("Editar", () -> abrirDialogoDocumento(selecionado(tabelaDocumentos, "Selecione um documento para editar."))),
                botaoSecundario("Marcar ausente", this::marcarDocumentoAusente),
                botaoPerigo("Arquivar", this::arquivarDocumento)
        );
        return telaComTabela(barra, tabelaDocumentos);
    }

    private Parent criarAbaAlertas() {
        var barra = barraAcoes(
                botaoPrimario("Novo", () -> abrirDialogoAlerta()),
                botaoSecundario("Ciente", this::tomarCienciaAlerta),
                botaoSecundario("Resolver", this::resolverAlerta)
        );
        return telaComTabela(barra, tabelaAlertas);
    }

    private Parent telaComTabela(Parent barra, TableView<?> tabela) {
        tabela.setColumnResizePolicy(TableView.CONSTRAINED_RESIZE_POLICY_FLEX_LAST_COLUMN);
        VBox.setVgrow(tabela, Priority.ALWAYS);
        var conteudo = new VBox(12, barra, tabela);
        conteudo.setPadding(new Insets(18));
        return conteudo;
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
                coluna("Status", bem -> textoEnum(bem.status())),
                coluna("Compra", bem -> bem.compradoEm().map(this::data).orElse("")),
                coluna("Valor", bem -> bem.precoCompra().map(this::dinheiro).orElse("")),
                coluna("Observações", Bem::observacoes)
        ));
    }

    private void configurarTabelaGarantias() {
        tabelaGarantias.getSelectionModel().setSelectionMode(SelectionMode.SINGLE);
        tabelaGarantias.getColumns().setAll(List.of(
                coluna("Bem", garantia -> nomeBem(garantia.bemId())),
                coluna("Tipo", garantia -> textoEnum(garantia.tipo())),
                coluna("Fornecedor", Garantia::fornecedor),
                coluna("Início", garantia -> data(garantia.periodo().iniciaEm())),
                coluna("Fim", garantia -> data(garantia.periodo().terminaEm())),
                coluna("Status", garantia -> textoEnum(garantia.statusEm(LocalDate.now()))),
                coluna("Suporte", Garantia::contatoSuporte)
        ));
        tabelaGarantias.setRowFactory(tabela -> new TableRow<>() {
            @Override
            protected void updateItem(Garantia garantia, boolean vazio) {
                super.updateItem(garantia, vazio);
                getStyleClass().removeAll("garantia-ativa", "garantia-perto", "garantia-vencida");
                if (!vazio && garantia != null) {
                    var status = garantia.statusEm(LocalDate.now());
                    if (status == StatusGarantia.VENCIDA) {
                        getStyleClass().add("garantia-vencida");
                    } else if (status == StatusGarantia.PERTO_DO_VENCIMENTO) {
                        getStyleClass().add("garantia-perto");
                    } else if (status == StatusGarantia.ATIVA) {
                        getStyleClass().add("garantia-ativa");
                    }
                }
            }
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
                coluna("Status", manutencao -> textoEnum(manutencao.statusEm(LocalDate.now())))
        ));
    }

    private void configurarTabelaDocumentos() {
        tabelaDocumentos.getSelectionModel().setSelectionMode(SelectionMode.SINGLE);
        tabelaDocumentos.getColumns().setAll(List.of(
                coluna("Bem", documento -> nomeBem(documento.bemId())),
                coluna("Tipo", documento -> textoEnum(documento.tipo())),
                coluna("Nome", documento -> documento.nome().valor()),
                coluna("Caminho", documento -> documento.caminhoLocal().valor().toString()),
                coluna("Registrado", documento -> data(documento.registradoEm())),
                coluna("Status", documento -> textoEnum(documento.status()))
        ));
    }

    private void configurarTabelaAlertas() {
        tabelaAlertas.getSelectionModel().setSelectionMode(SelectionMode.SINGLE);
        tabelaAlertas.getColumns().setAll(List.of(
                coluna("Bem", alerta -> nomeBem(alerta.bemId())),
                coluna("Tipo", alerta -> textoEnum(alerta.tipo())),
                coluna("Severidade", alerta -> textoEnum(alerta.severidade())),
                coluna("Mensagem", Alerta::mensagem),
                coluna("Criado", alerta -> data(alerta.criadoEm())),
                coluna("Prazo", alerta -> alerta.prazoEm().map(this::data).orElse("")),
                coluna("Status", alerta -> textoEnum(alerta.status()))
        ));
    }

    private <T> TableColumn<T, String> coluna(String titulo, Function<T, String> valor) {
        var coluna = new TableColumn<T, String>(titulo);
        coluna.setCellValueFactory(dados -> new ReadOnlyStringWrapper(valor.apply(dados.getValue())));
        return coluna;
    }

    private void atualizarPainel() {
        var bens = contexto.listarBens().executar();
        var garantias = contexto.listarGarantias().executar();
        var manutencoes = contexto.listarManutencoes().executar();
        var documentos = contexto.listarDocumentos().executar();
        var alertas = contexto.listarAlertas().executar();
        var hoje = LocalDate.now();

        painelResumo.getChildren().setAll(
                indicador("Bens", String.valueOf(bens.size())),
                indicador("Garantias vencidas", String.valueOf(contar(garantias, garantia -> garantia.statusEm(hoje) == StatusGarantia.VENCIDA))),
                indicador("Garantias próximas", String.valueOf(contar(garantias, garantia -> garantia.statusEm(hoje) == StatusGarantia.PERTO_DO_VENCIMENTO))),
                indicador("Manutenções atrasadas", String.valueOf(contar(manutencoes, manutencao -> manutencao.statusEm(hoje) == StatusManutencao.ATRASADA))),
                indicador("Documentos ausentes", String.valueOf(contar(documentos, documento -> documento.status() == StatusDocumento.AUSENTE))),
                indicador("Alertas abertos", String.valueOf(contar(alertas, alerta -> alerta.status() != StatusAlerta.RESOLVIDO)))
        );

        painelAlertas.getChildren().clear();
        alertas.stream()
                .filter(alerta -> alerta.status() != StatusAlerta.RESOLVIDO)
                .limit(8)
                .map(alerta -> new Label(nomeBem(alerta.bemId()) + " - " + alerta.mensagem()))
                .forEach(rotulo -> {
                    rotulo.getStyleClass().add("linha-alerta");
                    painelAlertas.getChildren().add(rotulo);
                });
        if (painelAlertas.getChildren().isEmpty()) {
            var vazio = new Label("Nenhum alerta aberto.");
            vazio.getStyleClass().add("texto-suave");
            painelAlertas.getChildren().add(vazio);
        }
    }

    private Parent indicador(String titulo, String valor) {
        var tituloLabel = new Label(titulo);
        tituloLabel.getStyleClass().add("indicador-titulo");
        var valorLabel = new Label(valor);
        valorLabel.getStyleClass().add("indicador-valor");
        var caixa = new VBox(4, tituloLabel, valorLabel);
        caixa.getStyleClass().add("indicador");
        return caixa;
    }

    private HBox barraAcoes(Button... botoes) {
        var barra = new HBox(8, botoes);
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
        var preco = campoTexto("Preço", bem == null ? "" : bem.precoCompra().map(valor -> valor.valor().toPlainString()).orElse(""));
        var moeda = campoTexto("Moeda", bem == null ? "BRL" : bem.precoCompra().map(valor -> valor.moeda().getCurrencyCode()).orElse("BRL"));
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
                        observacoes.getText()
                ));
            }
            return contexto.editarBem().executar(new EditarBemComando(
                    bem.id(),
                    nome.getText(),
                    tipo.getValue(),
                    compradoEm.getValue(),
                    decimalOpcional(preco.getText()),
                    textoOpcional(moeda.getText()),
                    observacoes.getText()
            ));
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
                        suporte.getText()
                ));
            }
            return contexto.editarGarantia().executar(new EditarGarantiaComando(
                    garantia.id(),
                    tipo.getValue(),
                    fornecedor.getText(),
                    dataObrigatoria(iniciaEm, "início"),
                    dataObrigatoria(terminaEm, "fim"),
                    suporte.getText()
            ));
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
                        dataObrigatoria(data, "data agendada")
                ));
            }
            return contexto.editarManutencao().executar(new EditarManutencaoComando(
                    manutencao.id(),
                    tipo.getValue(),
                    descricao.getText(),
                    dataObrigatoria(data, "data agendada")
            ));
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
                        dataObrigatoria(registradoEm, "registro")
                ));
            }
            return contexto.editarDocumento().executar(new EditarDocumentoComando(
                    documento.id(),
                    tipo.getValue(),
                    nome.getText(),
                    caminho.getText(),
                    dataObrigatoria(registradoEm, "registro")
            ));
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
                prazoEm.getValue()
        ))).ifPresent(resultado -> atualizarTudo());
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

        mostrarFormulario("Concluir manutenção", grade, () -> contexto.concluirManutencao().executar(new ConcluirManutencaoComando(
                manutencao.id(),
                dataObrigatoria(concluidaEm, "conclusão"),
                decimalOpcional(custo.getText()),
                textoOpcional(moeda.getText())
        ))).ifPresent(resultado -> atualizarTudo());
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
                alertas
        );
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
        alerta.showAndWait();
    }

    private void mostrarInformacao(String titulo, String mensagem) {
        var alerta = new javafx.scene.control.Alert(javafx.scene.control.Alert.AlertType.INFORMATION);
        alerta.setTitle(titulo);
        alerta.setHeaderText(null);
        alerta.setContentText(mensagem);
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

    private <T> long contar(List<T> itens, java.util.function.Predicate<T> predicado) {
        return itens.stream().filter(predicado).count();
    }

    private record ItemBem(Bem bem) {

        @Override
        public String toString() {
            return bem.nome().valor();
        }
    }
}
