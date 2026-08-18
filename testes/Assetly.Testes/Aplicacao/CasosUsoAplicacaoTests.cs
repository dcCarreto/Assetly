using Assetly.Aplicacao.CasoDeUso;
using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Excecao;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Manutencao;
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

    private sealed class RepositorioBemMemoria : IRepositorioBem
    {
        private readonly Dictionary<BemId, Bem> _dados = new();

        public Bem? BuscarPorId(BemId id) => _dados.GetValueOrDefault(id);

        public IReadOnlyList<Bem> Listar() => _dados.Values.ToList();

        public void Salvar(Bem bem) => _dados[bem.Id] = bem;
    }

    private sealed class RepositorioGarantiaMemoria : IRepositorioGarantia
    {
        private readonly Dictionary<GarantiaId, Garantia> _dados = new();

        public Garantia? BuscarPorId(GarantiaId id) => _dados.GetValueOrDefault(id);

        public IReadOnlyList<Garantia> Listar() => _dados.Values.ToList();

        public IReadOnlyList<Garantia> ListarPorBem(BemId bemId) =>
            _dados.Values.Where(g => g.BemId.Equals(bemId)).ToList();

        public void Salvar(Garantia garantia) => _dados[garantia.Id] = garantia;
    }

    private sealed class RepositorioManutencaoMemoria : IRepositorioManutencao
    {
        private readonly Dictionary<ManutencaoId, Manutencao> _dados = new();

        public Manutencao? BuscarPorId(ManutencaoId id) => _dados.GetValueOrDefault(id);

        public IReadOnlyList<Manutencao> Listar() => _dados.Values.ToList();

        public IReadOnlyList<Manutencao> ListarPorBem(BemId bemId) =>
            _dados.Values.Where(m => m.BemId.Equals(bemId)).ToList();

        public void Salvar(Manutencao manutencao) => _dados[manutencao.Id] = manutencao;
    }

    private sealed class RepositorioDocumentoMemoria : IRepositorioDocumento
    {
        private readonly Dictionary<DocumentoId, Documento> _dados = new();

        public Documento? BuscarPorId(DocumentoId id) => _dados.GetValueOrDefault(id);

        public IReadOnlyList<Documento> Listar() => _dados.Values.ToList();

        public IReadOnlyList<Documento> ListarPorBem(BemId bemId) =>
            _dados.Values.Where(d => d.BemId.Equals(bemId)).ToList();

        public void Salvar(Documento documento) => _dados[documento.Id] = documento;
    }

    private sealed class RepositorioAlertaMemoria : IRepositorioAlerta
    {
        private readonly Dictionary<AlertaId, Alerta> _dados = new();

        public Alerta? BuscarPorId(AlertaId id) => _dados.GetValueOrDefault(id);

        public IReadOnlyList<Alerta> Listar() => _dados.Values.ToList();

        public IReadOnlyList<Alerta> ListarPorBem(BemId bemId) =>
            _dados.Values.Where(a => a.BemId.Equals(bemId)).ToList();

        public void Salvar(Alerta alerta) => _dados[alerta.Id] = alerta;
    }
}
