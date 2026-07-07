package com.assetly.dominio.bem;

import com.assetly.dominio.compartilhado.Dinheiro;
import com.assetly.dominio.compartilhado.Validacao;

import java.time.LocalDate;
import java.util.Objects;
import java.util.Optional;

public final class Bem {

    private final BemId id;
    private NomeBem nome;
    private TipoBem tipo;
    private StatusBem status;
    private LocalDate compradoEm;
    private Dinheiro precoCompra;
    private String observacoes;

    private Bem(
            BemId id,
            NomeBem nome,
            TipoBem tipo,
            StatusBem status,
            LocalDate compradoEm,
            Dinheiro precoCompra,
            String observacoes
    ) {
        this.id = Objects.requireNonNull(id, "id do bem é obrigatório");
        this.nome = Objects.requireNonNull(nome, "nome do bem é obrigatório");
        this.tipo = Objects.requireNonNull(tipo, "tipo do bem é obrigatório");
        this.status = Objects.requireNonNull(status, "status do bem é obrigatório");
        this.compradoEm = compradoEm;
        this.precoCompra = precoCompra;
        this.observacoes = Validacao.textoOpcional(observacoes, "observações do bem", 500);
    }

    public static Bem criar(NomeBem nome, TipoBem tipo) {
        return new Bem(BemId.novo(), nome, tipo, StatusBem.ATIVO, null, null, "");
    }

    public static Bem criar(NomeBem nome, TipoBem tipo, LocalDate compradoEm, Dinheiro precoCompra) {
        var dataCompra = Validacao.exigirDataNaoFutura(compradoEm, "data de compra", LocalDate.now());
        return new Bem(BemId.novo(), nome, tipo, StatusBem.ATIVO, dataCompra, precoCompra, "");
    }

    public static Bem restaurar(
            BemId id,
            NomeBem nome,
            TipoBem tipo,
            StatusBem status,
            LocalDate compradoEm,
            Dinheiro precoCompra,
            String observacoes
    ) {
        return new Bem(id, nome, tipo, status, compradoEm, precoCompra, observacoes);
    }

    public BemId id() {
        return id;
    }

    public NomeBem nome() {
        return nome;
    }

    public TipoBem tipo() {
        return tipo;
    }

    public StatusBem status() {
        return status;
    }

    public Optional<LocalDate> compradoEm() {
        return Optional.ofNullable(compradoEm);
    }

    public Optional<Dinheiro> precoCompra() {
        return Optional.ofNullable(precoCompra);
    }

    public String observacoes() {
        return observacoes;
    }

    public void renomear(NomeBem novoNome) {
        this.nome = Objects.requireNonNull(novoNome, "nome do bem é obrigatório");
    }

    public void alterarTipo(TipoBem novoTipo) {
        this.tipo = Objects.requireNonNull(novoTipo, "tipo do bem é obrigatório");
    }

    public void atualizarCompra(LocalDate compradoEm, Dinheiro precoCompra) {
        this.compradoEm = Validacao.exigirDataNaoFutura(compradoEm, "data de compra", LocalDate.now());
        this.precoCompra = precoCompra;
    }

    public void atualizarObservacoes(String observacoes) {
        this.observacoes = Validacao.textoOpcional(observacoes, "observações do bem", 500);
    }

    public void marcarEmManutencao() {
        exigirAbertoParaMudanca();
        this.status = StatusBem.EM_MANUTENCAO;
    }

    public void ativar() {
        exigirAbertoParaMudanca();
        this.status = StatusBem.ATIVO;
    }

    public void descartar() {
        this.status = StatusBem.DESCARTADO;
    }

    public void arquivar() {
        this.status = StatusBem.ARQUIVADO;
    }

    private void exigirAbertoParaMudanca() {
        if (status == StatusBem.ARQUIVADO || status == StatusBem.DESCARTADO) {
            throw new IllegalStateException("bem arquivado ou descartado não pode ser alterado");
        }
    }
}
