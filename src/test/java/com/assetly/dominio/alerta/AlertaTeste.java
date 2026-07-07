package com.assetly.dominio.alerta;

import com.assetly.dominio.bem.BemId;
import org.junit.jupiter.api.Test;

import java.time.LocalDate;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class AlertaTeste {

    @Test
    void deveTomarCienciaEResolverAlerta() {
        var alerta = Alerta.criar(
                BemId.novo(),
                TipoAlerta.GARANTIA_VENCENDO,
                SeveridadeAlerta.AVISO,
                "Garantia vence em breve",
                LocalDate.of(2026, 7, 1),
                LocalDate.of(2026, 7, 20)
        );

        alerta.tomarCiencia();
        assertThat(alerta.status()).isEqualTo(StatusAlerta.CIENTE);
        assertThat(alerta.estaAcionavel()).isTrue();

        alerta.resolver();
        assertThat(alerta.status()).isEqualTo(StatusAlerta.RESOLVIDO);
        assertThat(alerta.estaAcionavel()).isFalse();
    }

    @Test
    void deveRejeitarMensagemEmBranco() {
        assertThatThrownBy(() -> Alerta.criar(
                BemId.novo(),
                TipoAlerta.DOCUMENTO_AUSENTE,
                SeveridadeAlerta.INFORMATIVO,
                " ",
                LocalDate.of(2026, 7, 1),
                null
        ))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessageContaining("mensagem do alerta");
    }

    @Test
    void deveRejeitarTomarCienciaDeAlertaResolvido() {
        var alerta = Alerta.criar(
                BemId.novo(),
                TipoAlerta.MANUTENCAO_ATRASADA,
                SeveridadeAlerta.CRITICO,
                "Manutenção está atrasada",
                LocalDate.of(2026, 7, 1),
                LocalDate.of(2026, 6, 1)
        );

        alerta.resolver();

        assertThatThrownBy(alerta::tomarCiencia)
                .isInstanceOf(IllegalStateException.class)
                .hasMessageContaining("alerta resolvido");
    }
}
