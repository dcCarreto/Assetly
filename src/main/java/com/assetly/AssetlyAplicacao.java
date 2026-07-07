package com.assetly;

import com.assetly.apresentacao.ContextoAplicacao;
import com.assetly.apresentacao.visao.TelaPrincipal;
import javafx.application.Application;
import javafx.scene.Scene;
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
        palco.setMinWidth(980);
        palco.setMinHeight(640);
        palco.setScene(cena);
        palco.show();
    }

    public static void main(String[] args) {
        launch(args);
    }
}
