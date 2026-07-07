package com.assetly.aplicacao.repositorio;

import com.assetly.dominio.bem.BemId;
import com.assetly.dominio.garantia.Garantia;
import com.assetly.dominio.garantia.GarantiaId;

import java.util.List;
import java.util.Optional;

public interface RepositorioGarantia {

    Optional<Garantia> buscarPorId(GarantiaId id);

    List<Garantia> listar();

    List<Garantia> listarPorBem(BemId bemId);

    void salvar(Garantia garantia);
}
