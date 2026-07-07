module com.assetly {
    requires java.sql;
    requires javafx.controls;
    requires javafx.fxml;
    requires org.xerial.sqlitejdbc;

    exports com.assetly;
    exports com.assetly.aplicacao.casodeuso;
    exports com.assetly.aplicacao.dados;
    exports com.assetly.aplicacao.excecao;
    exports com.assetly.aplicacao.repositorio;
    exports com.assetly.aplicacao.servico;
    exports com.assetly.dominio.alerta;
    exports com.assetly.dominio.bem;
    exports com.assetly.dominio.compartilhado;
    exports com.assetly.dominio.documento;
    exports com.assetly.dominio.garantia;
    exports com.assetly.dominio.manutencao;
}
