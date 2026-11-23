using Assetly.Aplicacao.CasoDeUso;
using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Excecao;
using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Manutencao;
using Assetly.Dominio.Saude;
using Shouldly;

namespace Assetly.Testes.Aplicacao;

public class CasosUsoAplicacaoTests
{
    [Fact]
    public void DeveCadastrarEditarListarEAlterarStatusDoBem()
    {
        var repositorioBem = new RepositorioBemMemoria();
        var cadastro = new CadastrarBem(repositorioBem);
        var edicao = new EditarBem(repositorioBem);
        var alteracaoStatus = new AlterarStatusBem(repositorioBem);
        var listagem = new ListarBens(repositorioBem);

        var bem = cadastro.Executar(new CadastrarBemComando(
            "Notebook",
            TipoBem.Eletronico,
            new DateOnly(2026, 1, 20),
            3200.00m,
            "BRL",
            "Uso diário"));

        var editado = edicao.Executar(new EditarBemComando(
            bem.Id,
            "Notebook trabalho",
            TipoBem.Eletronico,
            new DateOnly(2026, 1, 20),
            3100.00m,
            "BRL",
            "Atualizado"));

        alteracaoStatus.Executar(new AlterarStatusBemComando(bem.Id, StatusBem.EmManutencao));

        editado.Nome.Valor.ShouldBe("Notebook trabalho");
        editado.PrecoCompra.ShouldNotBeNull();
        editado.PrecoCompra!.Valor.ShouldBe(3100.00m);
        editado.Observacoes.ShouldBe("Atualizado");
        listagem.Executar().ShouldBe([editado]);
        editado.Status.ShouldBe(StatusBem.EmManutencao);
    }

    [Fact]
    public void DeveRegistrarItensRelacionadosAoBemEListarPorBem()
    {
        var repositorioBem = new RepositorioBemMemoria();
        var repositorioGarantia = new RepositorioGarantiaMemoria();
        var repositorioManutencao = new RepositorioManutencaoMemoria();
        var repositorioDocumento = new RepositorioDocumentoMemoria();
        var repositorioAlerta = new RepositorioAlertaMemoria();
        var bem = new CadastrarBem(repositorioBem).Executar(new CadastrarBemComando(
            "Impressora", TipoBem.Eletronico, null, null, null, null));

        var garantia = new RegistrarGarantia(repositorioBem, repositorioGarantia).Executar(new RegistrarGarantiaComando(
            bem.Id, TipoGarantia.Fabricante, "Fabricante", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), "0800"));
        var manutencao = new AgendarManutencao(repositorioBem, repositorioManutencao).Executar(new AgendarManutencaoComando(
            bem.Id, TipoManutencao.Preventiva, "Limpeza", new DateOnly(2026, 8, 1)));
        var documento = new RegistrarDocumento(repositorioBem, repositorioDocumento).Executar(new RegistrarDocumentoComando(
            bem.Id, TipoDocumento.NotaFiscal, "Nota fiscal", "documentos/nota.pdf", new DateOnly(2026, 1, 2)));
        var alerta = new CriarAlerta(repositorioBem, repositorioAlerta).Executar(new CriarAlertaComando(
            bem.Id, TipoAlerta.GarantiaVencendo, SeveridadeAlerta.Aviso, "Garantia vence em breve", new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31)));

        new ListarGarantiasDoBem(repositorioBem, repositorioGarantia).Executar(bem.Id).ShouldBe([garantia]);
        new ListarManutencoesDoBem(repositorioBem, repositorioManutencao).Executar(bem.Id).ShouldBe([manutencao]);
        new ListarDocumentosDoBem(repositorioBem, repositorioDocumento).Executar(bem.Id).ShouldBe([documento]);
        new ListarAlertasDoBem(repositorioBem, repositorioAlerta).Executar(bem.Id).ShouldBe([alerta]);
    }

    [Fact]
    public void DeveExecutarAcoesDeManutencaoDocumentoEAlerta()
    {
        var repositorioBem = new RepositorioBemMemoria();
        var repositorioManutencao = new RepositorioManutencaoMemoria();
        var repositorioDocumento = new RepositorioDocumentoMemoria();
        var repositorioAlerta = new RepositorioAlertaMemoria();
        var bem = new CadastrarBem(repositorioBem).Executar(new CadastrarBemComando(
            "Câmera", TipoBem.Eletronico, null, null, null, null));
        var manutencao = new AgendarManutencao(repositorioBem, repositorioManutencao).Executar(new AgendarManutencaoComando(
            bem.Id, TipoManutencao.Corretiva, "Trocar lente", new DateOnly(2026, 9, 10)));
        var documento = new RegistrarDocumento(repositorioBem, repositorioDocumento).Executar(new RegistrarDocumentoComando(
            bem.Id, TipoDocumento.Manual, "Manual", "documentos/manual.pdf", new DateOnly(2026, 1, 1)));
        var alerta = new CriarAlerta(repositorioBem, repositorioAlerta).Executar(new CriarAlertaComando(
            bem.Id, TipoAlerta.ManutencaoProxima, SeveridadeAlerta.Informativo, "Manutenção próxima", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 10)));

        new ConcluirManutencao(repositorioManutencao).Executar(new ConcluirManutencaoComando(
            manutencao.Id, new DateOnly(2026, 9, 9), 150.00m, "BRL"));
        new MarcarDocumentoAusente(repositorioDocumento).Executar(documento.Id);
        new TomarCienciaAlerta(repositorioAlerta).Executar(alerta.Id);
        new ResolverAlerta(repositorioAlerta).Executar(alerta.Id);

        manutencao.Status.ShouldBe(StatusManutencao.Concluida);
        manutencao.Custo.ShouldNotBeNull();
        manutencao.Custo!.Valor.ShouldBe(150.00m);
        documento.Status.ShouldBe(StatusDocumento.Ausente);
        alerta.Status.ShouldBe(StatusAlerta.Resolvido);
    }

    [Fact]
    public void DeveGerarAlertasAutomaticosSemDuplicarAlertasExistentes()
    {
        var dataReferencia = new DateOnly(2026, 7, 7);
        var repositorioBem = new RepositorioBemMemoria();
        var repositorioGarantia = new RepositorioGarantiaMemoria();
        var repositorioManutencao = new RepositorioManutencaoMemoria();
        var repositorioDocumento = new RepositorioDocumentoMemoria();
        var repositorioAlerta = new RepositorioAlertaMemoria();
        var bemComNota = new CadastrarBem(repositorioBem).Executar(new CadastrarBemComando(
            "Notebook", TipoBem.Eletronico, null, null, null, null));
        var bemSemNota = new CadastrarBem(repositorioBem).Executar(new CadastrarBemComando(
            "Câmera", TipoBem.Eletronico, null, null, null, null));

        new RegistrarDocumento(repositorioBem, repositorioDocumento).Executar(new RegistrarDocumentoComando(
            bemComNota.Id, TipoDocumento.NotaFiscal, "Nota fiscal", "documentos/nota.pdf", dataReferencia));
        new RegistrarGarantia(repositorioBem, repositorioGarantia).Executar(new RegistrarGarantiaComando(
            bemComNota.Id, TipoGarantia.Fabricante, "Fabricante", dataReferencia.AddMonths(-6), dataReferencia.AddDays(20), null));
        new RegistrarGarantia(repositorioBem, repositorioGarantia).Executar(new RegistrarGarantiaComando(
            bemSemNota.Id, TipoGarantia.Estendida, "Loja", dataReferencia.AddMonths(-12), dataReferencia.AddDays(-1), null));
        new AgendarManutencao(repositorioBem, repositorioManutencao).Executar(new AgendarManutencaoComando(
            bemComNota.Id, TipoManutencao.Preventiva, "Limpeza", dataReferencia.AddDays(3)));
        new AgendarManutencao(repositorioBem, repositorioManutencao).Executar(new AgendarManutencaoComando(
            bemSemNota.Id, TipoManutencao.Corretiva, "Trocar lente", dataReferencia.AddDays(-2)));

        var gerador = new GerarAlertasAutomaticos(
            repositorioBem, repositorioGarantia, repositorioManutencao, repositorioDocumento, repositorioAlerta);

        var gerados = gerador.Executar(dataReferencia);
        var segundaExecucao = gerador.Executar(dataReferencia);

        gerados.Count.ShouldBe(5);
        segundaExecucao.ShouldBeEmpty();
        repositorioAlerta.Listar().Count.ShouldBe(5);
        gerados.Select(a => a.Tipo).ShouldBe(
            [
                TipoAlerta.GarantiaVencendo,
                TipoAlerta.GarantiaVencida,
                TipoAlerta.ManutencaoProxima,
                TipoAlerta.ManutencaoAtrasada,
                TipoAlerta.DocumentoAusente
            ],
            ignoreOrder: true);

        gerados.Single(a => a.Tipo == TipoAlerta.GarantiaVencida).Severidade.ShouldBe(SeveridadeAlerta.Critico);
        gerados.Single(a => a.Tipo == TipoAlerta.DocumentoAusente).BemId.ShouldBe(bemSemNota.Id);
    }

    [Fact]
    public void DeveFalharQuandoRecursoNaoExiste()
    {
        var repositorioBem = new RepositorioBemMemoria();
        var repositorioGarantia = new RepositorioGarantiaMemoria();
        var bemIdInexistente = BemId.Novo();

        var excecaoBusca = Should.Throw<RecursoNaoEncontradoExcecao>(() => new BuscarBem(repositorioBem).Executar(bemIdInexistente));
        excecaoBusca.Message.ShouldContain("Bem não encontrado");

        var excecaoGarantia = Should.Throw<RecursoNaoEncontradoExcecao>(() => new RegistrarGarantia(repositorioBem, repositorioGarantia).Executar(new RegistrarGarantiaComando(
            bemIdInexistente, TipoGarantia.Fabricante, "Fabricante", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), null)));
        excecaoGarantia.Message.ShouldContain("Bem não encontrado");
    }

    [Fact]
    public void DeveRegistrarEditarECancelarSaude()
    {
        var repositorioBem = new RepositorioBemMemoria();
        var repositorioSaude = new RepositorioSaudeMemoria();
        var bem = new CadastrarBem(repositorioBem).Executar(new CadastrarBemComando(
            "Titular do plano", TipoBem.Outro, null, null, null, null));

        var saude = new RegistrarSaude(repositorioBem, repositorioSaude).Executar(new RegistrarSaudeComando(
            bem.Id, TipoSaude.Convenio, "Amil", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), "0800-123456"));

        var editada = new EditarSaude(repositorioSaude).Executar(new EditarSaudeComando(
            saude.Id, TipoSaude.Convenio, "Amil One", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), "0800-654321"));

        new ListarSaudeDoBem(repositorioBem, repositorioSaude).Executar(bem.Id).ShouldBe([editada]);
        editada.Fornecedor.ShouldBe("Amil One");

        new CancelarSaude(repositorioSaude).Executar(saude.Id, new DateOnly(2026, 6, 1));
        editada.StatusEm(new DateOnly(2026, 7, 1)).ShouldBe(StatusSaude.Cancelada);
    }

    [Fact]
    public void DeveRegistrarEEditarGastoDeSaude()
    {
        var repositorioBem = new RepositorioBemMemoria();
        var repositorioGastoSaude = new RepositorioGastoSaudeMemoria();
        var bem = new CadastrarBem(repositorioBem).Executar(new CadastrarBemComando(
            "Titular do plano", TipoBem.Outro, null, null, null, null));

        var gasto = new RegistrarGastoSaude(repositorioBem, repositorioGastoSaude).Executar(new RegistrarGastoSaudeComando(
            bem.Id, TipoGastoSaude.Farmacia, "Antibiótico", new DateOnly(2026, 3, 10), 89.90m, "BRL"));

        var editado = new EditarGastoSaude(repositorioGastoSaude).Executar(new EditarGastoSaudeComando(
            gasto.Id, TipoGastoSaude.Farmacia, "Antibiótico + analgésico", new DateOnly(2026, 3, 11), 120.00m, "BRL"));

        new ListarGastosSaudeDoBem(repositorioBem, repositorioGastoSaude).Executar(bem.Id).ShouldBe([editado]);
        editado.Descricao.ShouldBe("Antibiótico + analgésico");
        editado.Valor.Valor.ShouldBe(120.00m);
        new ListarGastosSaude(repositorioGastoSaude).Executar().ShouldBe([editado]);
    }

    [Fact]
    public void DeveResolverAutomaticamenteAlertasCujaCondicaoDeixouDeExistirSemTocarAlertasManuais()
    {
        var hoje = new DateOnly(2026, 7, 7);
        var (repositorioBem, repositorioGarantia, repositorioManutencao, repositorioDocumento, repositorioAlerta) = Repositorios();
        var bem = new CadastrarBem(repositorioBem).Executar(new CadastrarBemComando("Câmera", TipoBem.Eletronico, null, null, null, null));
        var garantia = new RegistrarGarantia(repositorioBem, repositorioGarantia).Executar(new RegistrarGarantiaComando(
            bem.Id, TipoGarantia.Fabricante, "Loja", hoje.AddYears(-1), hoje.AddDays(10), null));
        var manutencao = new AgendarManutencao(repositorioBem, repositorioManutencao).Executar(new AgendarManutencaoComando(
            bem.Id, TipoManutencao.Corretiva, "Trocar lente", hoje.AddDays(-2)));
        var manual = new CriarAlerta(repositorioBem, repositorioAlerta).Executar(new CriarAlertaComando(
            bem.Id, TipoAlerta.GarantiaVencendo, SeveridadeAlerta.Aviso, "Lembrete pessoal", hoje, null));
        var gerador = new GerarAlertasAutomaticos(repositorioBem, repositorioGarantia, repositorioManutencao, repositorioDocumento, repositorioAlerta);

        gerador.Executar(hoje).Count.ShouldBe(3);

        new RegistrarDocumento(repositorioBem, repositorioDocumento).Executar(new RegistrarDocumentoComando(
            bem.Id, TipoDocumento.NotaFiscal, "Nota", "nota.pdf", hoje));
        new EditarGarantia(repositorioGarantia).Executar(new EditarGarantiaComando(
            garantia.Id, TipoGarantia.Fabricante, "Loja", hoje.AddYears(-1), hoje.AddYears(1), null));
        new ConcluirManutencao(repositorioManutencao).Executar(new ConcluirManutencaoComando(manutencao.Id, hoje, null, null));
        gerador.Executar(hoje).ShouldBeEmpty();

        repositorioAlerta.Listar().Where(a => a.Automatico).ShouldAllBe(a => a.Status == StatusAlerta.Resolvido);
        manual.Status.ShouldBe(StatusAlerta.Aberto);
    }

    [Fact]
    public void DeveIgnorarEResolverAlertasDeBensArquivadosOuDescartados()
    {
        var hoje = new DateOnly(2026, 7, 7);
        var (repositorioBem, repositorioGarantia, repositorioManutencao, repositorioDocumento, repositorioAlerta) = Repositorios();
        var bem = new CadastrarBem(repositorioBem).Executar(new CadastrarBemComando("Impressora", TipoBem.Eletronico, null, null, null, null));
        new RegistrarGarantia(repositorioBem, repositorioGarantia).Executar(new RegistrarGarantiaComando(
            bem.Id, TipoGarantia.Fabricante, "Loja", hoje.AddYears(-2), hoje.AddDays(-1), null));
        var gerador = new GerarAlertasAutomaticos(repositorioBem, repositorioGarantia, repositorioManutencao, repositorioDocumento, repositorioAlerta);
        gerador.Executar(hoje).Count.ShouldBe(2);

        new AlterarStatusBem(repositorioBem).Executar(new AlterarStatusBemComando(bem.Id, StatusBem.Descartado));
        gerador.Executar(hoje).ShouldBeEmpty();

        repositorioAlerta.Listar().ShouldAllBe(a => a.Status == StatusAlerta.Resolvido);
    }

    [Fact]
    public void DeveGerarAlertasDeConvenioVencendoEVencido()
    {
        var hoje = new DateOnly(2026, 7, 7);
        var (repositorioBem, repositorioGarantia, repositorioManutencao, repositorioDocumento, repositorioAlerta) = Repositorios();
        var repositorioSaude = new RepositorioSaudeMemoria();
        // Pessoa (titular) não exige nota fiscal: só os dois alertas de convênio devem surgir.
        var titular = new CadastrarBem(repositorioBem).Executar(new CadastrarBemComando("Titular", TipoBem.Pessoa, null, null, null, null));
        new RegistrarSaude(repositorioBem, repositorioSaude).Executar(new RegistrarSaudeComando(
            titular.Id, TipoSaude.Convenio, "Amil", hoje.AddYears(-1), hoje.AddDays(15), null));
        new RegistrarSaude(repositorioBem, repositorioSaude).Executar(new RegistrarSaudeComando(
            titular.Id, TipoSaude.Ortodontia, "OdontoPrev", hoje.AddYears(-1), hoje.AddDays(-3), null));
        var cancelado = new RegistrarSaude(repositorioBem, repositorioSaude).Executar(new RegistrarSaudeComando(
            titular.Id, TipoSaude.Farmacia, "Drogaria", hoje.AddYears(-1), hoje.AddDays(-3), null));
        new CancelarSaude(repositorioSaude).Executar(cancelado.Id, hoje.AddDays(-10));

        var gerados = new GerarAlertasAutomaticos(repositorioBem, repositorioGarantia, repositorioManutencao, repositorioDocumento, repositorioAlerta, repositorioSaude)
            .Executar(hoje);

        gerados.Select(a => a.Tipo).ShouldBe([TipoAlerta.ConvenioVencendo, TipoAlerta.ConvenioVencido], ignoreOrder: true);
        gerados.Single(a => a.Tipo == TipoAlerta.ConvenioVencido).Mensagem.ShouldContain("OdontoPrev");
    }

    [Fact]
    public void DeveEditarDocumentoSemExigirOArquivoQuandoOCaminhoNaoMuda()
    {
        var (repositorioBem, _, _, repositorioDocumento, _) = Repositorios();
        var armazenamento = new ArmazenamentoFalso();
        var bem = new CadastrarBem(repositorioBem).Executar(new CadastrarBemComando("Sofá", TipoBem.Movel, null, null, null, null));
        var documento = new RegistrarDocumento(repositorioBem, repositorioDocumento, armazenamento).Executar(new RegistrarDocumentoComando(
            bem.Id, TipoDocumento.Recibo, "Recibo", "origem/recibo.pdf", new DateOnly(2026, 1, 1)));
        new MarcarDocumentoAusente(repositorioDocumento).Executar(documento.Id);

        new EditarDocumento(repositorioDocumento, armazenamento).Executar(new EditarDocumentoComando(
            documento.Id, TipoDocumento.NotaFiscal, "Nota fiscal do sofá", documento.CaminhoLocal.Valor, new DateOnly(2026, 2, 1)));

        armazenamento.Armazenados.Count.ShouldBe(1);
        documento.Status.ShouldBe(StatusDocumento.Ausente);
        documento.Nome.Valor.ShouldBe("Nota fiscal do sofá");
        documento.RegistradoEm.ShouldBe(new DateOnly(2026, 2, 1));

        new EditarDocumento(repositorioDocumento, armazenamento).Executar(new EditarDocumentoComando(
            documento.Id, TipoDocumento.NotaFiscal, "Nota fiscal do sofá", "origem/nova.pdf", new DateOnly(2026, 2, 1)));

        armazenamento.Armazenados.Count.ShouldBe(2);
        documento.Status.ShouldBe(StatusDocumento.Disponivel);
    }

    [Fact]
    public void DeveExcluirRegistrosERemoverArquivosSemReferencia()
    {
        var (repositorioBem, repositorioGarantia, repositorioManutencao, repositorioDocumento, repositorioAlerta) = Repositorios();
        var armazenamento = new ArmazenamentoFalso();
        var bem = new CadastrarBem(repositorioBem).Executar(new CadastrarBemComando("Notebook", TipoBem.Eletronico, null, null, null, null));
        var outroBem = new CadastrarBem(repositorioBem).Executar(new CadastrarBemComando("Monitor", TipoBem.Eletronico, null, null, null, null));
        var garantia = new RegistrarGarantia(repositorioBem, repositorioGarantia).Executar(new RegistrarGarantiaComando(
            bem.Id, TipoGarantia.Loja, "Loja", new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), null));
        var manutencao = new AgendarManutencao(repositorioBem, repositorioManutencao).Executar(new AgendarManutencaoComando(
            bem.Id, TipoManutencao.Limpeza, "Limpeza", new DateOnly(2026, 8, 1)));
        var alerta = new CriarAlerta(repositorioBem, repositorioAlerta).Executar(new CriarAlertaComando(
            bem.Id, TipoAlerta.ManutencaoProxima, SeveridadeAlerta.Informativo, "Teste", new DateOnly(2026, 7, 1), null));
        var compartilhado = new RegistrarDocumento(repositorioBem, repositorioDocumento, armazenamento).Executar(new RegistrarDocumentoComando(
            bem.Id, TipoDocumento.NotaFiscal, "Nota", "origem/nota.pdf", new DateOnly(2026, 1, 1)));
        var copia = new RegistrarDocumento(repositorioBem, repositorioDocumento).Executar(new RegistrarDocumentoComando(
            outroBem.Id, TipoDocumento.NotaFiscal, "Mesma nota", compartilhado.CaminhoLocal.Valor, new DateOnly(2026, 1, 1)));

        new ExcluirGarantia(repositorioGarantia).Executar(garantia.Id);
        new ExcluirManutencao(repositorioManutencao).Executar(manutencao.Id);
        new ExcluirAlerta(repositorioAlerta).Executar(alerta.Id);
        new ExcluirBem(repositorioBem, repositorioDocumento, armazenamento).Executar(bem.Id);

        repositorioGarantia.Listar().ShouldBeEmpty();
        repositorioManutencao.Listar().ShouldBeEmpty();
        repositorioAlerta.Listar().ShouldBeEmpty();
        repositorioBem.Listar().ShouldBe([outroBem]);
        armazenamento.Removidos.ShouldBeEmpty();

        new ExcluirDocumento(repositorioDocumento, armazenamento).Executar(copia.Id);
        armazenamento.Removidos.ShouldBe([compartilhado.CaminhoLocal.Valor]);
        Should.Throw<RecursoNaoEncontradoExcecao>(() => new ExcluirDocumento(repositorioDocumento, armazenamento).Executar(copia.Id));
    }

    [Fact]
    public void DeveReabrirAlertaResolvidoEEditarManutencaoConcluidaSemReagendar()
    {
        var (repositorioBem, _, repositorioManutencao, _, repositorioAlerta) = Repositorios();
        var bem = new CadastrarBem(repositorioBem).Executar(new CadastrarBemComando("Bicicleta", TipoBem.Veiculo, null, null, null, null));
        var alerta = new CriarAlerta(repositorioBem, repositorioAlerta).Executar(new CriarAlertaComando(
            bem.Id, TipoAlerta.ManutencaoProxima, SeveridadeAlerta.Informativo, "Revisão", new DateOnly(2026, 7, 1), null));
        new ResolverAlerta(repositorioAlerta).Executar(alerta.Id);
        new ReabrirAlerta(repositorioAlerta).Executar(alerta.Id).Status.ShouldBe(StatusAlerta.Aberto);

        var manutencao = new AgendarManutencao(repositorioBem, repositorioManutencao).Executar(new AgendarManutencaoComando(
            bem.Id, TipoManutencao.Inspecao, "Revisão", new DateOnly(2026, 6, 1)));
        new ConcluirManutencao(repositorioManutencao).Executar(new ConcluirManutencaoComando(manutencao.Id, new DateOnly(2026, 6, 2), 120m, "BRL"));

        new EditarManutencao(repositorioManutencao).Executar(new EditarManutencaoComando(
            manutencao.Id, TipoManutencao.Preventiva, "Revisão completa", new DateOnly(2026, 6, 1)));

        manutencao.Descricao.ShouldBe("Revisão completa");
        manutencao.Status.ShouldBe(StatusManutencao.Concluida);
        Should.Throw<InvalidOperationException>(() => new EditarManutencao(repositorioManutencao).Executar(new EditarManutencaoComando(
            manutencao.Id, TipoManutencao.Preventiva, "Revisão completa", new DateOnly(2026, 6, 20))));
    }

    [Fact]
    public void DeveAceitarPrecoSemDataELimparCompraNaEdicao()
    {
        var repositorioBem = new RepositorioBemMemoria();
        var bem = new CadastrarBem(repositorioBem).Executar(new CadastrarBemComando(
            "Relógio herdado", TipoBem.Outro, null, 900m, "BRL", null));
        bem.CompradoEm.ShouldBeNull();
        bem.PrecoCompra!.Valor.ShouldBe(900m);

        new EditarBem(repositorioBem).Executar(new EditarBemComando(bem.Id, "Relógio herdado", TipoBem.Outro, new DateOnly(2020, 5, 1), 900m, "BRL", null));
        bem.CompradoEm.ShouldBe(new DateOnly(2020, 5, 1));

        new EditarBem(repositorioBem).Executar(new EditarBemComando(bem.Id, "Relógio herdado", TipoBem.Outro, null, null, null, null));
        bem.CompradoEm.ShouldBeNull();
        bem.PrecoCompra.ShouldBeNull();

        Should.Throw<ArgumentException>(() => new EditarBem(repositorioBem).Executar(new EditarBemComando(
            bem.Id, "Relógio herdado", TipoBem.Outro, DateOnly.FromDateTime(DateTime.Now).AddDays(5), null, null, null)));
    }

    [Theory]
    [InlineData("1234,56", 1234.56)]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("1,234.56", 1234.56)]
    [InlineData("R$ 89,90", 89.90)]
    [InlineData("89.90", 89.90)]
    [InlineData("1.234", 1234)]
    [InlineData("1.234.567", 1234567)]
    [InlineData("1.234.567,8", 1234567.8)]
    [InlineData("0,5", 0.5)]
    [InlineData("150", 150)]
    public void DeveInterpretarValoresMonetariosNosFormatosBrasileiroEInternacional(string texto, double esperado)
    {
        ValorMonetario.InterpretarOpcional(texto).ShouldBe((decimal)esperado);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1-2")]
    public void DeveRejeitarValoresMonetariosInvalidos(string texto)
    {
        Should.Throw<ArgumentException>(() => ValorMonetario.InterpretarOpcional(texto));
    }

    [Fact]
    public void ValorMonetarioVazioEhNulo()
    {
        ValorMonetario.InterpretarOpcional("  ").ShouldBeNull();
    }

    private static (RepositorioBemMemoria, RepositorioGarantiaMemoria, RepositorioManutencaoMemoria, RepositorioDocumentoMemoria, RepositorioAlertaMemoria) Repositorios() =>
        (new RepositorioBemMemoria(), new RepositorioGarantiaMemoria(), new RepositorioManutencaoMemoria(), new RepositorioDocumentoMemoria(), new RepositorioAlertaMemoria());

    private sealed class ArmazenamentoFalso : IArmazenamentoDocumento
    {
        internal List<string> Armazenados { get; } = [];

        internal List<string> Removidos { get; } = [];

        public string Armazenar(string arquivoOrigem)
        {
            var destino = $"armazenado/{Armazenados.Count}-{Path.GetFileName(arquivoOrigem)}";
            Armazenados.Add(destino);
            return destino;
        }

        public void Remover(string caminhoArmazenado) => Removidos.Add(caminhoArmazenado);
    }
}
