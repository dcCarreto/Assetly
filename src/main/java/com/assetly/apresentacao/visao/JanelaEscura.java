package com.assetly.apresentacao.visao;

import javafx.geometry.Pos;
import javafx.scene.Cursor;
import javafx.scene.Node;
import javafx.scene.Parent;
import javafx.scene.control.Button;
import javafx.scene.control.Dialog;
import javafx.scene.control.Label;
import javafx.scene.image.ImageView;
import javafx.scene.layout.BorderPane;
import javafx.scene.layout.HBox;
import javafx.scene.layout.Priority;
import javafx.scene.layout.Region;
import javafx.scene.layout.StackPane;
import javafx.scene.input.MouseEvent;
import javafx.stage.Stage;
import javafx.stage.StageStyle;

public final class JanelaEscura {

    private static final int BORDA_REDIMENSIONAMENTO = 6;

    private JanelaEscura() {
    }

    public static Parent decorar(Stage palco, String titulo, Parent conteudo) {
        var estrutura = new BorderPane();
        estrutura.getStyleClass().add("janela-escura");
        estrutura.setTop(barraTitulo(palco, titulo, true));
        estrutura.setCenter(conteudo);
        configurarRedimensionamento(estrutura, palco);
        return estrutura;
    }

    public static void preparar(Dialog<?> dialogo, String titulo) {
        dialogo.initStyle(StageStyle.UNDECORATED);
        dialogo.getDialogPane().setHeader(barraTitulo(dialogo, titulo));
    }

    private static Parent barraTitulo(Stage palco, String titulo, boolean maximizar) {
        var icone = iconeJanela();
        var tituloLabel = new Label(titulo);
        tituloLabel.getStyleClass().add("titulo-janela-texto");
        var espaco = new Region();
        HBox.setHgrow(espaco, Priority.ALWAYS);

        var minimizar = botaoJanela("Minimizar", iconeMinimizar(), () -> palco.setIconified(true));
        var maximizarBotao = botaoJanela("Maximizar", iconeMaximizar(), () -> palco.setMaximized(!palco.isMaximized()));
        maximizarBotao.setVisible(maximizar);
        maximizarBotao.setManaged(maximizar);
        var fechar = botaoJanela("Fechar", iconeFechar(), palco::close);
        fechar.getStyleClass().add("botao-janela-fechar");

        var barra = new HBox(8, icone, tituloLabel, espaco, minimizar, maximizarBotao, fechar);
        barra.getStyleClass().add("barra-titulo-janela");
        barra.setAlignment(Pos.CENTER_LEFT);
        configurarMovimento(barra, palco);
        return barra;
    }

    private static Parent barraTitulo(Dialog<?> dialogo, String titulo) {
        var tituloLabel = new Label(titulo);
        tituloLabel.getStyleClass().add("titulo-janela-texto");
        var espaco = new Region();
        HBox.setHgrow(espaco, Priority.ALWAYS);
        var fechar = botaoJanela("Fechar", iconeFechar(), dialogo::close);
        fechar.getStyleClass().add("botao-janela-fechar");

        var barra = new HBox(8, tituloLabel, espaco, fechar);
        barra.getStyleClass().add("barra-titulo-janela");
        barra.setAlignment(Pos.CENTER_LEFT);
        configurarMovimentoDialogo(barra);
        return barra;
    }

    private static Node iconeJanela() {
        var recurso = JanelaEscura.class.getResource("/imagens/assetly-icon-16.png");
        if (recurso == null) {
            var fallback = new Label("A");
            fallback.getStyleClass().add("icone-janela-fallback");
            return fallback;
        }
        var imagem = new ImageView(recurso.toExternalForm());
        imagem.setFitWidth(16);
        imagem.setFitHeight(16);
        imagem.setPreserveRatio(true);
        imagem.setSmooth(true);
        return imagem;
    }

    private static Button botaoJanela(String descricao, Node icone, Runnable acao) {
        var botao = new Button();
        botao.getStyleClass().add("botao-janela");
        botao.setGraphic(icone);
        botao.setAccessibleText(descricao);
        botao.setFocusTraversable(false);
        botao.setOnAction(evento -> acao.run());
        return botao;
    }

    private static Node iconeMinimizar() {
        var linha = linhaIconeJanela();
        linha.getStyleClass().add("icone-janela-minimizar");
        linha.setMinSize(12, 2);
        linha.setPrefSize(12, 2);
        linha.setMaxSize(12, 2);
        return linha;
    }

    private static Node iconeMaximizar() {
        var caixa = new Region();
        caixa.getStyleClass().add("icone-janela-maximizar");
        caixa.setMinSize(10, 10);
        caixa.setPrefSize(10, 10);
        caixa.setMaxSize(10, 10);
        return caixa;
    }

    private static Node iconeFechar() {
        var icone = new StackPane();
        icone.getStyleClass().add("icone-janela-fechar");
        icone.setMinSize(12, 12);
        icone.setPrefSize(12, 12);
        icone.setMaxSize(12, 12);

        var linhaPrimaria = linhaIconeJanela();
        linhaPrimaria.getStyleClass().add("icone-janela-fechar-linha");
        linhaPrimaria.setRotate(45);

        var linhaSecundaria = linhaIconeJanela();
        linhaSecundaria.getStyleClass().add("icone-janela-fechar-linha");
        linhaSecundaria.setRotate(-45);

        icone.getChildren().addAll(linhaPrimaria, linhaSecundaria);
        return icone;
    }

    private static Region linhaIconeJanela() {
        var linha = new Region();
        linha.getStyleClass().add("icone-janela-linha");
        linha.setMinSize(12, 2);
        linha.setPrefSize(12, 2);
        linha.setMaxSize(12, 2);
        return linha;
    }

    private static void configurarMovimento(HBox barra, Stage palco) {
        var posicao = new double[2];
        barra.setOnMousePressed(evento -> {
            posicao[0] = palco.getX() - evento.getScreenX();
            posicao[1] = palco.getY() - evento.getScreenY();
        });
        barra.setOnMouseDragged(evento -> {
            if (!palco.isMaximized()) {
                palco.setX(evento.getScreenX() + posicao[0]);
                palco.setY(evento.getScreenY() + posicao[1]);
            }
        });
        barra.setOnMouseClicked(evento -> {
            if (evento.getClickCount() == 2) {
                palco.setMaximized(!palco.isMaximized());
            }
        });
    }

    private static void configurarMovimentoDialogo(HBox barra) {
        var posicao = new double[2];
        barra.setOnMousePressed(evento -> {
            var janela = barra.getScene().getWindow();
            posicao[0] = janela.getX() - evento.getScreenX();
            posicao[1] = janela.getY() - evento.getScreenY();
        });
        barra.setOnMouseDragged(evento -> {
            var janela = barra.getScene().getWindow();
            janela.setX(evento.getScreenX() + posicao[0]);
            janela.setY(evento.getScreenY() + posicao[1]);
        });
    }

    private static void configurarRedimensionamento(Parent raiz, Stage palco) {
        var direcao = new int[2];
        var inicio = new double[6];
        raiz.addEventFilter(MouseEvent.MOUSE_MOVED, evento -> {
            if (palco.isMaximized()) {
                raiz.setCursor(Cursor.DEFAULT);
                return;
            }
            raiz.setCursor(cursorRedimensionamento(
                    evento.getX(),
                    evento.getY(),
                    palco.getWidth(),
                    palco.getHeight()));
        });
        raiz.addEventFilter(MouseEvent.MOUSE_PRESSED, evento -> {
            direcao[0] = direcaoHorizontal(evento.getX(), palco.getWidth());
            direcao[1] = direcaoVertical(evento.getY(), palco.getHeight());
            inicio[0] = palco.getX();
            inicio[1] = palco.getY();
            inicio[2] = palco.getWidth();
            inicio[3] = palco.getHeight();
            inicio[4] = evento.getScreenX();
            inicio[5] = evento.getScreenY();
        });
        raiz.addEventFilter(MouseEvent.MOUSE_DRAGGED, evento -> {
            if (palco.isMaximized() || (direcao[0] == 0 && direcao[1] == 0)) {
                return;
            }
            redimensionar(palco, direcao, inicio, evento.getScreenX(), evento.getScreenY());
            evento.consume();
        });
    }

    private static Cursor cursorRedimensionamento(double x, double y, double largura, double altura) {
        var horizontal = direcaoHorizontal(x, largura);
        var vertical = direcaoVertical(y, altura);
        if (horizontal < 0 && vertical < 0) {
            return Cursor.NW_RESIZE;
        }
        if (horizontal > 0 && vertical < 0) {
            return Cursor.NE_RESIZE;
        }
        if (horizontal < 0 && vertical > 0) {
            return Cursor.SW_RESIZE;
        }
        if (horizontal > 0 && vertical > 0) {
            return Cursor.SE_RESIZE;
        }
        if (horizontal != 0) {
            return Cursor.E_RESIZE;
        }
        if (vertical != 0) {
            return Cursor.N_RESIZE;
        }
        return Cursor.DEFAULT;
    }

    private static int direcaoHorizontal(double x, double largura) {
        if (x <= BORDA_REDIMENSIONAMENTO) {
            return -1;
        }
        if (x >= largura - BORDA_REDIMENSIONAMENTO) {
            return 1;
        }
        return 0;
    }

    private static int direcaoVertical(double y, double altura) {
        if (y <= BORDA_REDIMENSIONAMENTO) {
            return -1;
        }
        if (y >= altura - BORDA_REDIMENSIONAMENTO) {
            return 1;
        }
        return 0;
    }

    private static void redimensionar(Stage palco, int[] direcao, double[] inicio, double telaX, double telaY) {
        var deltaX = telaX - inicio[4];
        var deltaY = telaY - inicio[5];
        if (direcao[0] > 0) {
            palco.setWidth(Math.max(palco.getMinWidth(), inicio[2] + deltaX));
        } else if (direcao[0] < 0) {
            var largura = Math.max(palco.getMinWidth(), inicio[2] - deltaX);
            palco.setX(inicio[0] + inicio[2] - largura);
            palco.setWidth(largura);
        }
        if (direcao[1] > 0) {
            palco.setHeight(Math.max(palco.getMinHeight(), inicio[3] + deltaY));
        } else if (direcao[1] < 0) {
            var altura = Math.max(palco.getMinHeight(), inicio[3] - deltaY);
            palco.setY(inicio[1] + inicio[3] - altura);
            palco.setHeight(altura);
        }
    }
}
