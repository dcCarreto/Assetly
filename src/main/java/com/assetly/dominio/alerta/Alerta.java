package com.assetly.dominio.alerta;

import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.compartilhado.Validacao;

import java.time.LocalDate;
import java.util.Objects;
import java.util.Optional;

public final class Alerta {

    private final AlertaId id;
    private final BemId bemId;
    private final TipoAlerta tipo;
    private final SeveridadeAlerta severidade;
    private final String mensagem;
    private final LocalDate criadoEm;
    private final LocalDate prazoEm;
    private StatusAlerta status;

    private Alerta(
            AlertaId id,
            BemId bemId,
            TipoAlerta tipo,
            SeveridadeAlerta severidade,
            String mensagem,
            LocalDate criadoEm,
            LocalDate prazoEm
    ) {
        this.id = Objects.requireNonNull(id, "id do alerta é obrigatório");
        this.bemId = Objects.requireNonNull(bemId, "id do bem é obrigatório");
        this.tipo = Objects.requireNonNull(tipo, "tipo do alerta é obrigatório");
        this.severidade = Objects.requireNonNull(severidade, "severidade do alerta é obrigatória");
        this.mensagem = Validacao.exigirTexto(mensagem, "mensagem do alerta", 240);
        this.criadoEm = Objects.requireNonNull(criadoEm, "data de criação é obrigatória");
        this.prazoEm = prazoEm;
        this.status = StatusAlerta.ABERTO;
    }

    public static Alerta criar(
            BemId bemId,
            TipoAlerta tipo,
            SeveridadeAlerta severidade,
            String mensagem,
            LocalDate criadoEm,
            LocalDate prazoEm
    ) {
        return new Alerta(AlertaId.novo(), bemId, tipo, severidade, mensagem, criadoEm, prazoEm);
    }

    public static Alerta restaurar(
            AlertaId id,
            BemId bemId,
            TipoAlerta tipo,
            SeveridadeAlerta severidade,
            String mensagem,
            LocalDate criadoEm,
            LocalDate prazoEm,
            StatusAlerta status
    ) {
        var alerta = new Alerta(id, bemId, tipo, severidade, mensagem, criadoEm, prazoEm);
        alerta.status = Objects.requireNonNull(status, "status do alerta é obrigatório");
        return alerta;
    }

    public AlertaId id() {
        return id;
    }

    public BemId bemId() {
        return bemId;
    }

    public TipoAlerta tipo() {
        return tipo;
    }

    public SeveridadeAlerta severidade() {
        return severidade;
    }

    public String mensagem() {
        return mensagem;
    }

    public LocalDate criadoEm() {
        return criadoEm;
    }

    public Optional<LocalDate> prazoEm() {
        return Optional.ofNullable(prazoEm);
    }

    public StatusAlerta status() {
        return status;
    }

    public boolean estaAcionavel() {
        return status != StatusAlerta.RESOLVIDO;
    }

    public void tomarCiencia() {
        if (status == StatusAlerta.RESOLVIDO) {
            throw new IllegalStateException("alerta resolvido não pode ser marcado como ciente");
        }
        this.status = StatusAlerta.CIENTE;
    }

    public void resolver() {
        this.status = StatusAlerta.RESOLVIDO;
    }
}
