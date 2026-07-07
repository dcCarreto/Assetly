package com.assetly.dominio.manutencao;

import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.compartilhado.Dinheiro;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.time.LocalDate;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class ManutencaoTeste {

    @Test
    void deveMarcarManutencaoAgendadaComoAtrasadaAposDataPrevista() {
        var manutencao = Manutencao.agendar(
                BemId.novo(),
                TipoManutencao.PREVENTIVA,
                "Troca de óleo",
                LocalDate.of(2026, 3, 10)
        );

        assertThat(manutencao.statusEm(LocalDate.of(2026, 3, 11))).isEqualTo(StatusManutencao.ATRASADA);
    }

    @Test
    void deveManterManutencaoConcluidaEncerrada() {
        var manutencao = Manutencao.agendar(
                BemId.novo(),
                TipoManutencao.CORRETIVA,
                "Trocar bateria",
                LocalDate.of(2026, 3, 10)
        );

        manutencao.concluir(LocalDate.of(2026, 3, 9), Dinheiro.de(new BigDecimal("49.90"), "USD"));

        assertThat(manutencao.status()).isEqualTo(StatusManutencao.CONCLUIDA);
        assertThat(manutencao.statusEm(LocalDate.of(2026, 4, 1))).isEqualTo(StatusManutencao.CONCLUIDA);
        assertThat(manutencao.custo()).hasValueSatisfying(custo -> assertThat(custo.valor()).isEqualByComparingTo("49.90"));
    }

    @Test
    void deveRejeitarDescricaoEmBranco() {
        assertThatThrownBy(() -> Manutencao.agendar(
                BemId.novo(),
                TipoManutencao.OUTRA,
                " ",
                LocalDate.of(2026, 3, 10)
        ))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessageContaining("descrição da manutenção");
    }
}
