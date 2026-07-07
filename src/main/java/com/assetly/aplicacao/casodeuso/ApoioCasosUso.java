package com.assetly.aplicacao.casodeuso;

import com.assetly.aplicacao.excecao.RecursoNaoEncontradoExcecao;
import com.assetly.aplicacao.repositorio.RepositorioAlerta;
import com.assetly.aplicacao.repositorio.RepositorioBem;
import com.assetly.aplicacao.repositorio.RepositorioDocumento;
import com.assetly.aplicacao.repositorio.RepositorioGarantia;
import com.assetly.aplicacao.repositorio.RepositorioManutencao;
import com.assetly.dominio.alerta.Alerta;
import com.assetly.dominio.alerta.AlertaId;
import com.assetly.dominio.bem.Bem;
import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.compartilhado.Dinheiro;
import com.assetly.dominio.documento.Documento;
import com.assetly.dominio.documento.DocumentoId;
import com.assetly.dominio.garantia.Garantia;
import com.assetly.dominio.garantia.GarantiaId;
import com.assetly.dominio.manutencao.Manutencao;
import com.assetly.dominio.manutencao.ManutencaoId;

import java.math.BigDecimal;

final class ApoioCasosUso {

    private ApoioCasosUso() {
    }

    static Bem exigirBem(RepositorioBem repositorio, BemId id) {
        return repositorio.buscarPorId(id)
                .orElseThrow(() -> new RecursoNaoEncontradoExcecao("Bem não encontrado: " + id.valor()));
    }

    static Garantia exigirGarantia(RepositorioGarantia repositorio, GarantiaId id) {
        return repositorio.buscarPorId(id)
                .orElseThrow(() -> new RecursoNaoEncontradoExcecao("Garantia não encontrada: " + id.valor()));
    }

    static Manutencao exigirManutencao(RepositorioManutencao repositorio, ManutencaoId id) {
        return repositorio.buscarPorId(id)
                .orElseThrow(() -> new RecursoNaoEncontradoExcecao("Manutenção não encontrada: " + id.valor()));
    }

    static Documento exigirDocumento(RepositorioDocumento repositorio, DocumentoId id) {
        return repositorio.buscarPorId(id)
                .orElseThrow(() -> new RecursoNaoEncontradoExcecao("Documento não encontrado: " + id.valor()));
    }

    static Alerta exigirAlerta(RepositorioAlerta repositorio, AlertaId id) {
        return repositorio.buscarPorId(id)
                .orElseThrow(() -> new RecursoNaoEncontradoExcecao("Alerta não encontrado: " + id.valor()));
    }

    static Dinheiro dinheiroOpcional(BigDecimal valor, String codigoMoeda) {
        if (valor == null) {
            return null;
        }
        if (codigoMoeda == null || codigoMoeda.isBlank()) {
            throw new IllegalArgumentException("código da moeda é obrigatório quando valor é informado");
        }
        return Dinheiro.de(valor, codigoMoeda);
    }
}
