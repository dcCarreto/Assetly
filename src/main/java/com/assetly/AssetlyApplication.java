package com.assetly;

import javafx.application.Application;
import javafx.geometry.Insets;
import javafx.geometry.Pos;
import javafx.scene.Scene;
import javafx.scene.control.Label;
import javafx.scene.layout.BorderPane;
import javafx.scene.layout.VBox;
import javafx.stage.Stage;

public class AssetlyApplication extends Application {

    @Override
    public void start(Stage stage) {
        var title = new Label("Assetly");
        title.getStyleClass().add("app-title");

        var subtitle = new Label("Personal assets, warranties and maintenance in one local-first app.");
        subtitle.getStyleClass().add("app-subtitle");

        var content = new VBox(12, title, subtitle);
        content.setAlignment(Pos.CENTER_LEFT);
        content.setPadding(new Insets(32));

        var root = new BorderPane(content);
        root.getStyleClass().add("app-root");

        var scene = new Scene(root, 960, 640);
        var stylesheet = AssetlyApplication.class.getResource("/css/assetly.css");
        if (stylesheet != null) {
            scene.getStylesheets().add(stylesheet.toExternalForm());
        }

        stage.setTitle("Assetly");
        stage.setMinWidth(720);
        stage.setMinHeight(480);
        stage.setScene(scene);
        stage.show();
    }

    public static void main(String[] args) {
        launch(args);
    }
}
