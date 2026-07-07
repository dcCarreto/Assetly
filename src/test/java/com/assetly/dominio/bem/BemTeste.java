package com.assetly.dominio.bem;

import com.assetly.dominio.compartilhado.Dinheiro;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.time.LocalDate;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class BemTeste {

    @Test
    void deveCriarBemAtivoComNomeNormalizado() {
        var bem = Bem.criar(new NomeBem("  Notebook  "), TipoBem.ELETRONICO);

        assertThat(bem.nome().valor()).isEqualTo("Notebook");
        assertThat(bem.tipo()).isEqualTo(TipoBem.ELETRONICO);
        assertThat(bem.status()).isEqualTo(StatusBem.ATIVO);
        assertThat(bem.compradoEm()).isEmpty();
    }

    @Test
    void deveRejeitarNomeEmBranco() {
        assertThatThrownBy(() -> new NomeBem("   "))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessageContaining("nome do bem");
    }

    @Test
    void deveRejeitarDataDeCompraFutura() {
        var futuro = LocalDate.now().plusDays(1);
        var preco = Dinheiro.de(new BigDecimal("100.00"), "USD");

        assertThatThrownBy(() -> Bem.criar(new NomeBem("Câmera"), TipoBem.ELETRONICO, futuro, preco))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessageContaining("data de compra");
    }

    @Test
    void deveImpedirReativarBemDescartado() {
        var bem = Bem.criar(new NomeBem("Bicicleta"), TipoBem.OUTRO);

        bem.descartar();

        assertThatThrownBy(bem::ativar)
                .isInstanceOf(IllegalStateException.class)
                .hasMessageContaining("arquivado ou descartado");
    }
}
