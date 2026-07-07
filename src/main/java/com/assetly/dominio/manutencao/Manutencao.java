package com.assetly.dominio.manutencao;

import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.compartilhado.Dinheiro;
import com.assetly.dominio.compartilhado.Validacao;

import java.time.LocalDate;
import java.util.Objects;
import java.util.Optional;

public final class Manutencao {

    private final ManutencaoId id;
    private final BemId bemId;
    private TipoManutencao tipo;
    private String descricao;
    private LocalDate agendadaPara;
    private LocalDate concluidaEm;
    private Dinheiro custo;
    private StatusManutencao status;

    private Manutencao(
            ManutencaoId id,
            BemId bemId,
            TipoManutencao tipo,
            String descricao,
            LocalDate agendadaPara
    ) {
        this.id = Objects.requireNonNull(id, "id da manutenção é obrigatório");
        this.bemId = Objects.requireNonNull(bemId, "id do bem é obrigatório");
        this.tipo = Objects.requireNonNull(tipo, "tipo da manutenção é obrigatório");
        this.descricao = Validacao.exigirTexto(descricao, "descrição da manutenção", 240);
        this.agendadaPara = Objects.requireNonNull(agendadaPara, "data agendada é obrigatória");
        this.status = StatusManutencao.AGENDADA;
    }

    public static Manutencao agendar(
            BemId bemId,
            TipoManutencao tipo,
            String descricao,
            LocalDate agendadaPara
    ) {
        return new Manutencao(ManutencaoId.novo(), bemId, tipo, descricao, agendadaPara);
    }

    public static Manutencao restaurar(
            ManutencaoId id,
            BemId bemId,
            TipoManutencao tipo,
            String descricao,
            LocalDate agendadaPara,
            LocalDate concluidaEm,
            Dinheiro custo,
            StatusManutencao status
    ) {
        var manutencao = new Manutencao(id, bemId, tipo, descricao, agendadaPara);
        manutencao.concluidaEm = concluidaEm;
        manutencao.custo = custo;
        manutencao.status = Objects.requireNonNull(status, "status da manutenção é obrigatório");
        return manutencao;
    }

    public ManutencaoId id() {
        return id;
    }

    public BemId bemId() {
        return bemId;
    }

    public TipoManutencao tipo() {
        return tipo;
    }

    public String descricao() {
        return descricao;
    }

    public LocalDate agendadaPara() {
        return agendadaPara;
    }

    public Optional<LocalDate> concluidaEm() {
        return Optional.ofNullable(concluidaEm);
    }

    public Optional<Dinheiro> custo() {
        return Optional.ofNullable(custo);
    }

    public StatusManutencao status() {
        return status;
    }

    public StatusManutencao statusEm(LocalDate data) {
        Objects.requireNonNull(data, "data é obrigatória");
        if (status == StatusManutencao.CONCLUIDA || status == StatusManutencao.CANCELADA) {
            return status;
        }
        if (agendadaPara.isBefore(data)) {
            return StatusManutencao.ATRASADA;
        }
        return StatusManutencao.AGENDADA;
    }

    public void reagendar(LocalDate agendadaPara) {
        exigirAberta();
        this.agendadaPara = Objects.requireNonNull(agendadaPara, "data agendada é obrigatória");
        this.status = StatusManutencao.AGENDADA;
    }

    public void atualizarDescricao(String descricao) {
        this.descricao = Validacao.exigirTexto(descricao, "descrição da manutenção", 240);
    }

    public void alterarTipo(TipoManutencao tipo) {
        this.tipo = Objects.requireNonNull(tipo, "tipo da manutenção é obrigatório");
    }

    public void concluir(LocalDate concluidaEm, Dinheiro custo) {
        exigirAberta();
        this.concluidaEm = Objects.requireNonNull(concluidaEm, "data de conclusão é obrigatória");
        this.custo = custo;
        this.status = StatusManutencao.CONCLUIDA;
    }

    public void cancelar() {
        exigirAberta();
        this.status = StatusManutencao.CANCELADA;
    }

    private void exigirAberta() {
        if (status == StatusManutencao.CONCLUIDA || status == StatusManutencao.CANCELADA) {
            throw new IllegalStateException("manutenção já está encerrada");
        }
    }
}
