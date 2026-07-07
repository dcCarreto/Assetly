package com.assetly.infraestrutura.repositorio;

import com.assetly.dominio.alerta.Alerta;
import com.assetly.dominio.alerta.SeveridadeAlerta;
import com.assetly.dominio.alerta.StatusAlerta;
import com.assetly.dominio.alerta.TipoAlerta;
import com.assetly.dominio.bem.Bem;
import com.assetly.dominio.bem.NomeBem;
import com.assetly.dominio.bem.StatusBem;
import com.assetly.dominio.bem.TipoBem;
import com.assetly.dominio.compartilhado.Dinheiro;
import com.assetly.dominio.documento.CaminhoDocumentoLocal;
import com.assetly.dominio.documento.Documento;
import com.assetly.dominio.documento.NomeDocumento;
import com.assetly.dominio.documento.StatusDocumento;
import com.assetly.dominio.documento.TipoDocumento;
import com.assetly.dominio.garantia.Garantia;
import com.assetly.dominio.garantia.StatusGarantia;
import com.assetly.dominio.garantia.TipoGarantia;
import com.assetly.dominio.manutencao.Manutencao;
import com.assetly.dominio.manutencao.StatusManutencao;
import com.assetly.dominio.manutencao.TipoManutencao;
import com.assetly.infraestrutura.banco.BancoDadosAssetly;
import com.assetly.infraestrutura.banco.FabricaConexaoSqlite;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.nio.file.Files;
import java.nio.file.Path;
import java.time.LocalDate;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;

class RepositoriosSqliteTeste {

    @Test
    void deveMigrarBancoDeFormaIdempotente() throws Exception {
        var banco = bancoTemporario();

        banco.inicializar();
        banco.inicializar();

        assertThat(Files.exists(banco.fabricaConexao().arquivoBanco())).isTrue();
        try (var conexao = banco.fabricaConexao().abrir();
             var comando = conexao.createStatement();
             var resultado = comando.executeQuery("SELECT COUNT(*) AS total FROM schema_migrations")) {
            assertThat(resultado.next()).isTrue();
            assertThat(resultado.getInt("total")).isEqualTo(1);
        }
    }

    @Test
    void deveSalvarERecuperarBens() {
        var banco = bancoTemporario();
        banco.inicializar();
        var repositorio = new RepositorioBemSqlite(banco.fabricaConexao());
        var bem = Bem.criar(
                new NomeBem("Notebook"),
                TipoBem.ELETRONICO,
                LocalDate.of(2026, 1, 10),
                Dinheiro.de(new BigDecimal("2500.00"), "BRL")
        );

        bem.atualizarObservacoes("Uso de trabalho");
        bem.marcarEmManutencao();
        repositorio.salvar(bem);

        var recuperado = repositorio.buscarPorId(bem.id()).orElseThrow();

        assertThat(recuperado.nome().valor()).isEqualTo("Notebook");
        assertThat(recuperado.status()).isEqualTo(StatusBem.EM_MANUTENCAO);
        assertThat(recuperado.precoCompra()).hasValueSatisfying(preco -> assertThat(preco.valor()).isEqualByComparingTo("2500.00"));
        assertThat(repositorio.listar()).extracting(item -> item.id()).containsExactly(bem.id());
    }

    @Test
    void deveSalvarEListarDadosRelacionadosAoBem() {
        var banco = bancoTemporario();
        banco.inicializar();
        var repositorioBem = new RepositorioBemSqlite(banco.fabricaConexao());
        var repositorioGarantia = new RepositorioGarantiaSqlite(banco.fabricaConexao());
        var repositorioManutencao = new RepositorioManutencaoSqlite(banco.fabricaConexao());
        var repositorioDocumento = new RepositorioDocumentoSqlite(banco.fabricaConexao());
        var repositorioAlerta = new RepositorioAlertaSqlite(banco.fabricaConexao());
        var bem = Bem.criar(new NomeBem("Impressora"), TipoBem.ELETRONICO);

        repositorioBem.salvar(bem);

        var garantia = Garantia.criar(
                bem.id(),
                TipoGarantia.FABRICANTE,
                "Fabricante",
                LocalDate.of(2026, 1, 1),
                LocalDate.of(2026, 12, 31)
        );
        garantia.atualizarContatoSuporte("0800");
        repositorioGarantia.salvar(garantia);

        var manutencao = Manutencao.agendar(
                bem.id(),
                TipoManutencao.PREVENTIVA,
                "Limpeza",
                LocalDate.of(2026, 8, 1)
        );
        manutencao.concluir(LocalDate.of(2026, 8, 1), Dinheiro.de(new BigDecimal("120.00"), "BRL"));
        repositorioManutencao.salvar(manutencao);

        var documento = Documento.registrar(
                bem.id(),
                TipoDocumento.NOTA_FISCAL,
                new NomeDocumento("Nota fiscal"),
                CaminhoDocumentoLocal.de("documentos/nota.pdf"),
                LocalDate.of(2026, 1, 2)
        );
        documento.marcarAusente();
        repositorioDocumento.salvar(documento);

        var alerta = Alerta.criar(
                bem.id(),
                TipoAlerta.GARANTIA_VENCENDO,
                SeveridadeAlerta.AVISO,
                "Garantia vence em breve",
                LocalDate.of(2026, 12, 1),
                LocalDate.of(2026, 12, 31)
        );
        alerta.tomarCiencia();
        repositorioAlerta.salvar(alerta);

        assertThat(repositorioGarantia.listarPorBem(bem.id()))
                .singleElement()
                .satisfies(item -> assertThat(item.statusEm(LocalDate.of(2026, 12, 15))).isEqualTo(StatusGarantia.PERTO_DO_VENCIMENTO));
        assertThat(repositorioManutencao.listarPorBem(bem.id()))
                .singleElement()
                .satisfies(item -> assertThat(item.status()).isEqualTo(StatusManutencao.CONCLUIDA));
        assertThat(repositorioDocumento.listarPorBem(bem.id()))
                .singleElement()
                .satisfies(item -> assertThat(item.status()).isEqualTo(StatusDocumento.AUSENTE));
        assertThat(repositorioAlerta.listarPorBem(bem.id()))
                .singleElement()
                .satisfies(item -> assertThat(item.status()).isEqualTo(StatusAlerta.CIENTE));
    }

    private BancoDadosAssetly bancoTemporario() {
        var arquivo = Path.of(
                System.getProperty("java.io.tmpdir"),
                "assetly-" + UUID.randomUUID(),
                "assetly.sqlite3"
        );
        return new BancoDadosAssetly(new FabricaConexaoSqlite(arquivo));
    }
}
