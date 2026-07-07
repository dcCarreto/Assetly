package com.assetly.infraestrutura.banco;

import com.assetly.infraestrutura.armazenamento.CaminhosArmazenamentoLocal;

public final class BancoDadosAssetly {

    private final FabricaConexaoSqlite fabricaConexao;
    private final MigradorBancoDados migradorBancoDados;

    public BancoDadosAssetly(FabricaConexaoSqlite fabricaConexao) {
        this.fabricaConexao = fabricaConexao;
        this.migradorBancoDados = new MigradorBancoDados(fabricaConexao);
    }

    public static BancoDadosAssetly localPadrao() {
        return new BancoDadosAssetly(new FabricaConexaoSqlite(CaminhosArmazenamentoLocal.arquivoBanco()));
    }

    public static BancoDadosAssetly localTeste() {
        return new BancoDadosAssetly(new FabricaConexaoSqlite(CaminhosArmazenamentoLocal.arquivoBancoTeste()));
    }

    public void inicializar() {
        migradorBancoDados.migrar();
    }

    public FabricaConexaoSqlite fabricaConexao() {
        return fabricaConexao;
    }
}
