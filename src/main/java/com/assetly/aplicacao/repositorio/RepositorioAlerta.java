package com.assetly.aplicacao.repositorio;

import com.assetly.dominio.alerta.Alerta;
import com.assetly.dominio.alerta.AlertaId;
import com.assetly.dominio.bem.BemId;

import java.util.List;
import java.util.Optional;

public interface RepositorioAlerta {

    Optional<Alerta> buscarPorId(AlertaId id);

    List<Alerta> listar();

    List<Alerta> listarPorBem(BemId bemId);

    void salvar(Alerta alerta);
}
