package com.assetly;

import com.assetly.apresentacao.ContextoAplicacao;
import com.assetly.apresentacao.visao.TelaPrincipal;
import javafx.application.Application;
import javafx.scene.Scene;
import javafx.scene.image.Image;
import javafx.stage.Stage;

import java.util.List;

public class AssetlyAplicacao extends Application {

    @Override
    public void start(Stage palco) {
        var contexto = contextoAplicacao(getParameters().getRaw());
        var telaPrincipal = new TelaPrincipal(contexto);
        var cena = new Scene(telaPrincipal.raiz(), 1180, 760);
        var folhaEstilos = AssetlyAplicacao.class.getResource("/estilos/assetly.css");
        if (folhaEstilos != null) {
            cena.getStylesheets().add(folhaEstilos.toExternalForm());
        }

        palco.setTitle(contexto.ambienteTeste() ? "Assetly - Teste" : "Assetly");
        aplicarIcones(palco);
        palco.setMinWidth(980);
        palco.setMinHeight(640);
        palco.setScene(cena);
        palco.show();
    }

    private ContextoAplicacao contextoAplicacao(List<String> argumentos) {
        if (usarAmbienteTeste(argumentos)) {
            return ContextoAplicacao.testeComDadosFicticios();
        }
        return ContextoAplicacao.localPadrao();
    }

    private boolean usarAmbienteTeste(List<String> argumentos) {
        var ambientePropriedade = System.getProperty("assetly.ambiente", "");
        var ambienteVariavel = System.getenv().getOrDefault("ASSETLY_AMBIENTE", "");
        return argumentos.contains("--teste")
                || ehAmbienteTeste(valorArgumentoAmbiente(argumentos))
                || ehAmbienteTeste(ambientePropriedade)
                || ehAmbienteTeste(ambienteVariavel);
    }

    private String valorArgumentoAmbiente(List<String> argumentos) {
        for (var indice = 0; indice < argumentos.size(); indice++) {
            var argumento = argumentos.get(indice);
            if (argumento.startsWith("--ambiente=")) {
                return argumento.substring("--ambiente=".length());
            }
            if ("--ambiente".equals(argumento) && indice + 1 < argumentos.size()) {
                return argumentos.get(indice + 1);
            }
        }
        return "";
    }

    private boolean ehAmbienteTeste(String valor) {
        return "teste".equalsIgnoreCase(valor.trim());
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
