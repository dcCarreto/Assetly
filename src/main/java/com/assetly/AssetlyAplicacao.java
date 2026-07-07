package com.assetly;

import com.assetly.apresentacao.ContextoAplicacao;
import com.assetly.apresentacao.visao.TelaPrincipal;
import javafx.application.Application;
import javafx.scene.Scene;
import javafx.scene.image.Image;
import javafx.stage.Stage;

public class AssetlyAplicacao extends Application {

    @Override
    public void start(Stage palco) {
        var telaPrincipal = new TelaPrincipal(ContextoAplicacao.localPadrao());
        var cena = new Scene(telaPrincipal.raiz(), 1180, 760);
        var folhaEstilos = AssetlyAplicacao.class.getResource("/estilos/assetly.css");
        if (folhaEstilos != null) {
            cena.getStylesheets().add(folhaEstilos.toExternalForm());
        }

        palco.setTitle("Assetly");
        aplicarIcones(palco);
        palco.setMinWidth(980);
        palco.setMinHeight(640);
        palco.setScene(cena);
        palco.show();
    }

    private void aplicarIcones(Stage palco) {
        for (var caminho : new String[] {
                "/imagens/assetly-icon-16.png",
                "/imagens/assetly-icon-32.png",
                "/imagens/assetly-icon-64.png",
                "/imagens/assetly-icon-128.png",
                "/imagens/assetly-icon-256.png",
                "/imagens/assetly-icon.png"
        }) {
            var recurso = AssetlyAplicacao.class.getResource(caminho);
            if (recurso != null) {
                palco.getIcons().add(new Image(recurso.toExternalForm()));
            }
        }
    }

    public static void main(String[] args) {
        launch(args);
    }
}
