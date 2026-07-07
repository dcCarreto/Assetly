package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.dados.AgendarManutencaoComando;
import com.assetly.aplicacao.dados.AlterarStatusBemComando;
import com.assetly.aplicacao.dados.CadastrarBemComando;
import com.assetly.aplicacao.dados.ConcluirManutencaoComando;
import com.assetly.aplicacao.dados.CriarAlertaComando;
import com.assetly.aplicacao.dados.EditarBemComando;
import com.assetly.aplicacao.dados.RegistrarDocumentoComando;
import com.assetly.aplicacao.dados.RegistrarGarantiaComando;
import com.assetly.aplicacao.excecao.RecursoNaoEncontradoExcecao;
import com.assetly.aplicacao.repositorio.RepositorioAlerta;
import com.assetly.aplicacao.repositorio.RepositorioBem;
import com.assetly.aplicacao.repositorio.RepositorioDocumento;
import com.assetly.aplicacao.repositorio.RepositorioGarantia;
import com.assetly.aplicacao.repositorio.RepositorioManutencao;
import com.assetly.dominio.alerta.Alerta;
import com.assetly.dominio.alerta.AlertaId;
import com.assetly.dominio.alerta.SeveridadeAlerta;
import com.assetly.dominio.alerta.StatusAlerta;
import com.assetly.dominio.alerta.TipoAlerta;
import com.assetly.dominio.bem.Bem;
import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.bem.StatusBem;
import com.assetly.dominio.bem.TipoBem;
import com.assetly.dominio.documento.Documento;
import com.assetly.dominio.documento.DocumentoId;
import com.assetly.dominio.documento.StatusDocumento;
import com.assetly.dominio.documento.TipoDocumento;
import com.assetly.dominio.garantia.Garantia;
import com.assetly.dominio.garantia.GarantiaId;
import com.assetly.dominio.garantia.TipoGarantia;
import com.assetly.dominio.manutencao.Manutencao;
import com.assetly.dominio.manutencao.ManutencaoId;
import com.assetly.dominio.manutencao.StatusManutencao;
import com.assetly.dominio.manutencao.TipoManutencao;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

class CasosUsoAplicacaoTeste {

    @Test
    void deveCadastrarEditarListarEAlterarStatusDoBem() {
        var repositorioBem = new RepositorioBemMemoria();
        var cadastro = new CadastrarBem(repositorioBem);
        var edicao = new EditarBem(repositorioBem);
        var alteracaoStatus = new AlterarStatusBem(repositorioBem);
        var listagem = new ListarBens(repositorioBem);

        var bem = cadastro.executar(new CadastrarBemComando(
                "Notebook",
                TipoBem.ELETRONICO,
                LocalDate.of(2026, 1, 20),
                new BigDecimal("3200.00"),
                "BRL",
                "Uso diário"
        ));

        var editado = edicao.executar(new EditarBemComando(
                bem.id(),
                "Notebook trabalho",
                TipoBem.ELETRONICO,
                LocalDate.of(2026, 1, 20),
                new BigDecimal("3100.00"),
                "BRL",
                "Atualizado"
        ));

        alteracaoStatus.executar(new AlterarStatusBemComando(bem.id(), StatusBem.EM_MANUTENCAO));

        assertThat(editado.nome().valor()).isEqualTo("Notebook trabalho");
        assertThat(editado.precoCompra()).hasValueSatisfying(preco -> assertThat(preco.valor()).isEqualByComparingTo("3100.00"));
        assertThat(editado.observacoes()).isEqualTo("Atualizado");
        assertThat(listagem.executar()).containsExactly(editado);
        assertThat(editado.status()).isEqualTo(StatusBem.EM_MANUTENCAO);
    }

    @Test
    void deveRegistrarItensRelacionadosAoBemEListarPorBem() {
        var repositorioBem = new RepositorioBemMemoria();
        var repositorioGarantia = new RepositorioGarantiaMemoria();
        var repositorioManutencao = new RepositorioManutencaoMemoria();
        var repositorioDocumento = new RepositorioDocumentoMemoria();
        var repositorioAlerta = new RepositorioAlertaMemoria();
        var bem = new CadastrarBem(repositorioBem).executar(new CadastrarBemComando(
                "Impressora",
                TipoBem.ELETRONICO,
                null,
                null,
                null,
                null
        ));

        var garantia = new RegistrarGarantia(repositorioBem, repositorioGarantia).executar(new RegistrarGarantiaComando(
                bem.id(),
                TipoGarantia.FABRICANTE,
                "Fabricante",
                LocalDate.of(2026, 1, 1),
                LocalDate.of(2026, 12, 31),
                "0800"
        ));
        var manutencao = new AgendarManutencao(repositorioBem, repositorioManutencao).executar(new AgendarManutencaoComando(
                bem.id(),
                TipoManutencao.PREVENTIVA,
                "Limpeza",
                LocalDate.of(2026, 8, 1)
        ));
        var documento = new RegistrarDocumento(repositorioBem, repositorioDocumento).executar(new RegistrarDocumentoComando(
                bem.id(),
                TipoDocumento.NOTA_FISCAL,
                "Nota fiscal",
                "documentos/nota.pdf",
                LocalDate.of(2026, 1, 2)
        ));
        var alerta = new CriarAlerta(repositorioBem, repositorioAlerta).executar(new CriarAlertaComando(
                bem.id(),
                TipoAlerta.GARANTIA_VENCENDO,
                SeveridadeAlerta.AVISO,
                "Garantia vence em breve",
                LocalDate.of(2026, 12, 1),
                LocalDate.of(2026, 12, 31)
        ));

        assertThat(new ListarGarantiasDoBem(repositorioBem, repositorioGarantia).executar(bem.id())).containsExactly(garantia);
        assertThat(new ListarManutencoesDoBem(repositorioBem, repositorioManutencao).executar(bem.id())).containsExactly(manutencao);
        assertThat(new ListarDocumentosDoBem(repositorioBem, repositorioDocumento).executar(bem.id())).containsExactly(documento);
        assertThat(new ListarAlertasDoBem(repositorioBem, repositorioAlerta).executar(bem.id())).containsExactly(alerta);
    }

    @Test
    void deveExecutarAcoesDeManutencaoDocumentoEAlerta() {
        var repositorioBem = new RepositorioBemMemoria();
        var repositorioManutencao = new RepositorioManutencaoMemoria();
        var repositorioDocumento = new RepositorioDocumentoMemoria();
        var repositorioAlerta = new RepositorioAlertaMemoria();
        var bem = new CadastrarBem(repositorioBem).executar(new CadastrarBemComando(
                "Câmera",
                TipoBem.ELETRONICO,
                null,
                null,
                null,
                null
        ));
        var manutencao = new AgendarManutencao(repositorioBem, repositorioManutencao).executar(new AgendarManutencaoComando(
                bem.id(),
                TipoManutencao.CORRETIVA,
                "Trocar lente",
                LocalDate.of(2026, 9, 10)
        ));
        var documento = new RegistrarDocumento(repositorioBem, repositorioDocumento).executar(new RegistrarDocumentoComando(
                bem.id(),
                TipoDocumento.MANUAL,
                "Manual",
                "documentos/manual.pdf",
                LocalDate.of(2026, 1, 1)
        ));
        var alerta = new CriarAlerta(repositorioBem, repositorioAlerta).executar(new CriarAlertaComando(
                bem.id(),
                TipoAlerta.MANUTENCAO_PROXIMA,
                SeveridadeAlerta.INFORMATIVO,
                "Manutenção próxima",
                LocalDate.of(2026, 9, 1),
                LocalDate.of(2026, 9, 10)
        ));

        new ConcluirManutencao(repositorioManutencao).executar(new ConcluirManutencaoComando(
                manutencao.id(),
                LocalDate.of(2026, 9, 9),
                new BigDecimal("150.00"),
                "BRL"
        ));
        new MarcarDocumentoAusente(repositorioDocumento).executar(documento.id());
        new TomarCienciaAlerta(repositorioAlerta).executar(alerta.id());
        new ResolverAlerta(repositorioAlerta).executar(alerta.id());

        assertThat(manutencao.status()).isEqualTo(StatusManutencao.CONCLUIDA);
        assertThat(manutencao.custo()).hasValueSatisfying(custo -> assertThat(custo.valor()).isEqualByComparingTo("150.00"));
        assertThat(documento.status()).isEqualTo(StatusDocumento.AUSENTE);
        assertThat(alerta.status()).isEqualTo(StatusAlerta.RESOLVIDO);
    }

    @Test
    void deveFalharQuandoRecursoNaoExiste() {
        var repositorioBem = new RepositorioBemMemoria();
        var repositorioGarantia = new RepositorioGarantiaMemoria();
        var bemIdInexistente = BemId.novo();

        assertThatThrownBy(() -> new BuscarBem(repositorioBem).executar(bemIdInexistente))
                .isInstanceOf(RecursoNaoEncontradoExcecao.class)
                .hasMessageContaining("Bem não encontrado");

        assertThatThrownBy(() -> new RegistrarGarantia(repositorioBem, repositorioGarantia).executar(new RegistrarGarantiaComando(
                bemIdInexistente,
                TipoGarantia.FABRICANTE,
                "Fabricante",
                LocalDate.of(2026, 1, 1),
                LocalDate.of(2026, 12, 31),
                null
        )))
                .isInstanceOf(RecursoNaoEncontradoExcecao.class)
                .hasMessageContaining("Bem não encontrado");
    }

    private static final class RepositorioBemMemoria implements RepositorioBem {

        private final Map<BemId, Bem> dados = new LinkedHashMap<>();

        @Override
        public Optional<Bem> buscarPorId(BemId id) {
            return Optional.ofNullable(dados.get(id));
        }

        @Override
        public List<Bem> listar() {
            return new ArrayList<>(dados.values());
        }

        @Override
        public void salvar(Bem bem) {
            dados.put(bem.id(), bem);
        }
    }

    private static final class RepositorioGarantiaMemoria implements RepositorioGarantia {

        private final Map<GarantiaId, Garantia> dados = new LinkedHashMap<>();

        @Override
        public Optional<Garantia> buscarPorId(GarantiaId id) {
            return Optional.ofNullable(dados.get(id));
        }

        @Override
        public List<Garantia> listar() {
            return new ArrayList<>(dados.values());
        }

        @Override
        public List<Garantia> listarPorBem(BemId bemId) {
            return dados.values().stream()
                    .filter(garantia -> garantia.bemId().equals(bemId))
                    .toList();
        }

        @Override
        public void salvar(Garantia garantia) {
            dados.put(garantia.id(), garantia);
        }
    }

    private static final class RepositorioManutencaoMemoria implements RepositorioManutencao {

        private final Map<ManutencaoId, Manutencao> dados = new LinkedHashMap<>();

        @Override
        public Optional<Manutencao> buscarPorId(ManutencaoId id) {
            return Optional.ofNullable(dados.get(id));
        }

        @Override
        public List<Manutencao> listar() {
            return new ArrayList<>(dados.values());
        }

        @Override
        public List<Manutencao> listarPorBem(BemId bemId) {
            return dados.values().stream()
                    .filter(manutencao -> manutencao.bemId().equals(bemId))
                    .toList();
        }

        @Override
        public void salvar(Manutencao manutencao) {
            dados.put(manutencao.id(), manutencao);
        }
    }

    private static final class RepositorioDocumentoMemoria implements RepositorioDocumento {

        private final Map<DocumentoId, Documento> dados = new LinkedHashMap<>();

        @Override
        public Optional<Documento> buscarPorId(DocumentoId id) {
            return Optional.ofNullable(dados.get(id));
        }

        @Override
        public List<Documento> listar() {
            return new ArrayList<>(dados.values());
        }

        @Override
        public List<Documento> listarPorBem(BemId bemId) {
            return dados.values().stream()
                    .filter(documento -> documento.bemId().equals(bemId))
                    .toList();
        }

        @Override
        public void salvar(Documento documento) {
            dados.put(documento.id(), documento);
        }
    }

    private static final class RepositorioAlertaMemoria implements RepositorioAlerta {

        private final Map<AlertaId, Alerta> dados = new LinkedHashMap<>();

        @Override
        public Optional<Alerta> buscarPorId(AlertaId id) {
            return Optional.ofNullable(dados.get(id));
        }

        @Override
        public List<Alerta> listar() {
            return new ArrayList<>(dados.values());
        }

        @Override
        public List<Alerta> listarPorBem(BemId bemId) {
            return dados.values().stream()
                    .filter(alerta -> alerta.bemId().equals(bemId))
                    .toList();
        }

        @Override
        public void salvar(Alerta alerta) {
            dados.put(alerta.id(), alerta);
        }
    }
}
