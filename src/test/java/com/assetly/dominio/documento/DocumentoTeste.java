package com.assetly.dominio.documento;

import com.assetly.dominio.bem.BemId;
import org.junit.jupiter.api.Test;

import java.nio.file.Path;
import java.time.LocalDate;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class DocumentoTeste {

    @Test
    void deveRegistrarDocumentoENormalizarCaminho() {
        var documento = Documento.registrar(
                BemId.novo(),
                TipoDocumento.NOTA_FISCAL,
                new NomeDocumento("  Nota fiscal 2026  "),
                CaminhoDocumentoLocal.de("documentos/../documentos/nota-fiscal.pdf"),
                LocalDate.of(2026, 2, 1)
        );

        assertThat(documento.nome().valor()).isEqualTo("Nota fiscal 2026");
        assertThat(documento.caminhoLocal().valor()).isEqualTo(Path.of("documentos/nota-fiscal.pdf"));
        assertThat(documento.status()).isEqualTo(StatusDocumento.DISPONIVEL);
    }

    @Test
    void deveMarcarDocumentoComoAusente() {
        var documento = Documento.registrar(
                BemId.novo(),
                TipoDocumento.MANUAL,
                new NomeDocumento("Manual"),
                CaminhoDocumentoLocal.de("documentos/manual.pdf"),
                LocalDate.of(2026, 2, 1)
        );

        documento.marcarAusente();

        assertThat(documento.status()).isEqualTo(StatusDocumento.AUSENTE);
    }

    @Test
    void deveRejeitarNomeDeDocumentoEmBranco() {
        assertThatThrownBy(() -> new NomeDocumento(" "))
                .isInstanceOf(IllegalArgumentException.class)
                .hasMessageContaining("nome do documento");
    }
}
