using Assetly.Dominio.Alerta;
using Assetly.Dominio.Backup;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Despesa;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Manutencao;
using Assetly.Dominio.Preferencias;
using Assetly.Dominio.Saude;
using Assetly.Infraestrutura.Banco;
using Assetly.Infraestrutura.Repositorio;
using Shouldly;

namespace Assetly.Testes.Infraestrutura;

public class RepositoriosSqliteTests
{
    [Fact]
    public void DeveMigrarBancoDeFormaIdempotente()
    {
        var banco = BancoTemporario();

        banco.Inicializar();
        banco.Inicializar();

        File.Exists(banco.FabricaConexao.ArquivoBanco).ShouldBeTrue();
        using var conexao = banco.FabricaConexao.Abrir();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT COUNT(*) AS total FROM schema_migrations";
        var total = (long)comando.ExecuteScalar()!;
        total.ShouldBe(MigradorBancoDados.VersaoMaisRecente);
    }

    [Fact]
    public void MigracaoExecutaGatilhosTextosComPontoEVirgulaEComentarios()
    {
        var banco = BancoTemporario();
        banco.Inicializar();
        using var conexao = banco.FabricaConexao.Abrir();

        MigradorBancoDados.ExecutarScript(conexao, """
            -- comentário com ; no meio
            CREATE TABLE teste_gatilho (id INTEGER PRIMARY KEY, texto TEXT NOT NULL, alterado INTEGER NOT NULL DEFAULT 0);
            CREATE TRIGGER marcar_alteracao AFTER UPDATE OF texto ON teste_gatilho
            BEGIN
                UPDATE teste_gatilho SET alterado = 1 WHERE id = NEW.id;
            END;
            INSERT INTO teste_gatilho (id, texto) VALUES (1, 'a; b; c'); -- comentário no fim da linha
            UPDATE teste_gatilho SET texto = 'novo' WHERE id = 1;
            """);

        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT alterado FROM teste_gatilho WHERE id = 1";
        ((long)comando.ExecuteScalar()!).ShouldBe(1);
    }

    [Fact]
    public void SalvarVariosEhTudoOuNada()
    {
        var banco = BancoTemporario();
        banco.Inicializar();
        var bem = Bem.Criar(new NomeBem("Carro"), TipoBem.Veiculo);
        new RepositorioBemSqlite(banco.FabricaConexao).Salvar(bem);
        var despesas = new RepositorioDespesaSqlite(banco.FabricaConexao);
        var alertas = new RepositorioAlertaSqlite(banco.FabricaConexao);
        var semBem = new BemId(Guid.NewGuid());

        // O segundo item aponta para um bem que não existe (chave estrangeira): nada é gravado.
        Should.Throw<ExcecaoBancoDados>(() => despesas.SalvarTodos(
        [
            Despesa.Registrar(bem.Id, TipoDespesa.Ipva, "IPVA (1/2)", new DateOnly(2027, 1, 10), Dinheiro.De(500m, "BRL")),
            Despesa.Registrar(semBem, TipoDespesa.Ipva, "IPVA (2/2)", new DateOnly(2027, 2, 10), Dinheiro.De(500m, "BRL"))
        ]));
        despesas.Listar().ShouldBeEmpty();

        Should.Throw<ExcecaoBancoDados>(() => alertas.SalvarTodos(
        [
            Alerta.CriarAutomatico(bem.Id, TipoAlerta.ContaVencida, SeveridadeAlerta.Critico, "Conta vencida.", new DateOnly(2027, 1, 1), null, Guid.NewGuid()),
            Alerta.CriarAutomatico(semBem, TipoAlerta.ContaVencida, SeveridadeAlerta.Critico, "Conta vencida.", new DateOnly(2027, 1, 1), null)
        ]));
        alertas.Listar().ShouldBeEmpty();

        var origem = Guid.NewGuid();
        alertas.SalvarTodos([Alerta.CriarAutomatico(bem.Id, TipoAlerta.ContaVencida, SeveridadeAlerta.Critico, "Conta vencida.", new DateOnly(2027, 1, 1), null, origem)]);
        alertas.Listar().Single().OrigemId.ShouldBe(origem);
    }

    [Fact]
    public void DeveSalvarERecuperarBens()
    {
        var banco = BancoTemporario();
        banco.Inicializar();
        var repositorio = new RepositorioBemSqlite(banco.FabricaConexao);
        var bem = Bem.Criar(
            new NomeBem("Notebook"),
            TipoBem.Eletronico,
            new DateOnly(2026, 1, 10),
            Dinheiro.De(2500.00m, "BRL"));

        bem.AtualizarObservacoes("Uso de trabalho");
        bem.MarcarEmManutencao();
        repositorio.Salvar(bem);

        var recuperado = repositorio.BuscarPorId(bem.Id) ?? throw new InvalidOperationException("bem não encontrado");

        recuperado.Nome.Valor.ShouldBe("Notebook");
        recuperado.Status.ShouldBe(StatusBem.EmManutencao);
        recuperado.PrecoCompra.ShouldNotBeNull();
        recuperado.PrecoCompra!.Valor.ShouldBe(2500.00m);
        repositorio.Listar().Select(item => item.Id).ShouldBe([bem.Id]);
    }

    [Fact]
    public void DeveSalvarEListarDadosRelacionadosAoBem()
    {
        var banco = BancoTemporario();
        banco.Inicializar();
        var repositorioBem = new RepositorioBemSqlite(banco.FabricaConexao);
        var repositorioGarantia = new RepositorioGarantiaSqlite(banco.FabricaConexao);
        var repositorioManutencao = new RepositorioManutencaoSqlite(banco.FabricaConexao);
        var repositorioDocumento = new RepositorioDocumentoSqlite(banco.FabricaConexao);
        var repositorioAlerta = new RepositorioAlertaSqlite(banco.FabricaConexao);
        var repositorioSaude = new RepositorioSaudeSqlite(banco.FabricaConexao);
        var repositorioGastoSaude = new RepositorioGastoSaudeSqlite(banco.FabricaConexao);
        var bem = Bem.Criar(new NomeBem("Impressora"), TipoBem.Eletronico);

        repositorioBem.Salvar(bem);

        var garantia = Garantia.Criar(
            bem.Id, TipoGarantia.Fabricante, "Fabricante", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        garantia.AtualizarContatoSuporte("0800");
        repositorioGarantia.Salvar(garantia);

        var manutencao = Manutencao.Agendar(bem.Id, TipoManutencao.Preventiva, "Limpeza", new DateOnly(2026, 8, 1));
        manutencao.Concluir(new DateOnly(2026, 8, 1), Dinheiro.De(120.00m, "BRL"));
        repositorioManutencao.Salvar(manutencao);

        var documento = Documento.Registrar(
            bem.Id, TipoDocumento.NotaFiscal, new NomeDocumento("Nota fiscal"),
            CaminhoDocumentoLocal.De("documentos/nota.pdf"), new DateOnly(2026, 1, 2));
        documento.MarcarAusente();
        repositorioDocumento.Salvar(documento);

        var alerta = Alerta.Criar(
            bem.Id, TipoAlerta.GarantiaVencendo, SeveridadeAlerta.Aviso, "Garantia vence em breve",
            new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31));
        alerta.TomarCiencia();
        repositorioAlerta.Salvar(alerta);

        var saude = Saude.Criar(
            bem.Id, TipoSaude.Convenio, "Amil", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), "0800-123456");
        saude.Cancelar(new DateOnly(2026, 6, 1));
        repositorioSaude.Salvar(saude);

        var gastoSaude = GastoSaude.Registrar(
            bem.Id, TipoGastoSaude.Farmacia, "Antibiótico", new DateOnly(2026, 3, 10), Dinheiro.De(89.90m, "BRL"));
        repositorioGastoSaude.Salvar(gastoSaude);

        repositorioGarantia.ListarPorBem(bem.Id).Single().StatusEm(new DateOnly(2026, 12, 15)).ShouldBe(StatusGarantia.PertoDoVencimento);
        repositorioManutencao.ListarPorBem(bem.Id).Single().Status.ShouldBe(StatusManutencao.Concluida);
        repositorioDocumento.ListarPorBem(bem.Id).Single().Status.ShouldBe(StatusDocumento.Ausente);
        repositorioAlerta.ListarPorBem(bem.Id).Single().Status.ShouldBe(StatusAlerta.Ciente);
        repositorioSaude.ListarPorBem(bem.Id).Single().StatusEm(new DateOnly(2026, 7, 1)).ShouldBe(StatusSaude.Cancelada);
        repositorioGastoSaude.ListarPorBem(bem.Id).Single().Valor.Valor.ShouldBe(89.90m);
    }

    [Fact]
    public void DeveObterConfiguracaoPadraoQuandoNadaFoiSalvo()
    {
        var banco = BancoTemporario();
        banco.Inicializar();
        var repositorio = new RepositorioConfiguracaoBackupSqlite(banco.FabricaConexao);

        var configuracao = repositorio.Obter();

        configuracao.CaminhoDestino.ShouldBeNull();
        configuracao.AutoBackupHabilitado.ShouldBeFalse();
    }

    [Fact]
    public void DeveSalvarERecuperarConfiguracaoDeBackup()
    {
        var banco = BancoTemporario();
        banco.Inicializar();
        var repositorio = new RepositorioConfiguracaoBackupSqlite(banco.FabricaConexao);

        var configuracao = repositorio.Obter();
        configuracao.DefinirDestino(@"D:\Backups\Assetly");
        configuracao.HabilitarAutoBackup(FrequenciaBackup.Semanal);
        configuracao.RegistrarBackupConcluido(new DateTimeOffset(2026, 3, 10, 8, 0, 0, TimeSpan.Zero));
        repositorio.Salvar(configuracao);

        var recuperada = repositorio.Obter();

        recuperada.CaminhoDestino.ShouldBe(@"D:\Backups\Assetly");
        recuperada.AutoBackupHabilitado.ShouldBeTrue();
        recuperada.Frequencia.ShouldBe(FrequenciaBackup.Semanal);
        recuperada.UltimoBackupEm.ShouldBe(new DateTimeOffset(2026, 3, 10, 8, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void DeveObterPreferenciasPadraoQuandoNadaFoiSalvo()
    {
        var banco = BancoTemporario();
        banco.Inicializar();
        var repositorio = new RepositorioConfiguracaoPreferenciasSqlite(banco.FabricaConexao);

        var preferencias = repositorio.Obter();

        preferencias.Tema.ShouldBe(TemaVisual.Escuro);
        preferencias.CorDestaque.ShouldBe(CorDestaque.Ciano);
        preferencias.OrdemModulos.ShouldBeEmpty();
        preferencias.ModuloInicial.ShouldBeNull();
    }

    [Fact]
    public void DeveSalvarERecuperarPreferencias()
    {
        var banco = BancoTemporario();
        banco.Inicializar();
        var repositorio = new RepositorioConfiguracaoPreferenciasSqlite(banco.FabricaConexao);

        var preferencias = repositorio.Obter();
        preferencias.SincronizarModulos(["Painel", "Bens", "Garantias"]);
        preferencias.DefinirTema(TemaVisual.Claro);
        preferencias.DefinirCorDestaque(CorDestaque.Verde);
        preferencias.OcultarModulo("Bens");
        preferencias.DefinirModuloInicial("Garantias");
        repositorio.Salvar(preferencias);

        var recuperada = repositorio.Obter();

        recuperada.Tema.ShouldBe(TemaVisual.Claro);
        recuperada.CorDestaque.ShouldBe(CorDestaque.Verde);
        recuperada.OrdemModulos.ShouldBe(["Painel", "Bens", "Garantias"]);
        recuperada.ModulosOcultos.ShouldContain("Bens");
        recuperada.ModuloInicial.ShouldBe("Garantias");
    }

    [Fact]
    public void DeveExcluirBemEmCascataComTodosOsRegistrosLigados()
    {
        var banco = BancoTemporario();
        banco.Inicializar();
        var fabrica = banco.FabricaConexao;
        var repositorioBem = new RepositorioBemSqlite(fabrica);
        var repositorioGarantia = new RepositorioGarantiaSqlite(fabrica);
        var repositorioManutencao = new RepositorioManutencaoSqlite(fabrica);
        var repositorioAlerta = new RepositorioAlertaSqlite(fabrica);
        var repositorioSaude = new RepositorioSaudeSqlite(fabrica);
        var repositorioGasto = new RepositorioGastoSaudeSqlite(fabrica);
        var bem = Bem.Criar(new NomeBem("Notebook"), TipoBem.Eletronico);
        var outro = Bem.Criar(new NomeBem("Monitor"), TipoBem.Eletronico);
        repositorioBem.Salvar(bem);
        repositorioBem.Salvar(outro);
        repositorioGarantia.Salvar(Garantia.Criar(bem.Id, TipoGarantia.Loja, "Loja", new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1)));
        repositorioManutencao.Salvar(Manutencao.Agendar(bem.Id, TipoManutencao.Limpeza, "Limpeza", new DateOnly(2026, 8, 1)));
        repositorioAlerta.Salvar(Alerta.CriarAutomatico(bem.Id, TipoAlerta.DocumentoAusente, SeveridadeAlerta.Aviso, "Nota fiscal ausente.", new DateOnly(2026, 7, 1), null));
        repositorioSaude.Salvar(Saude.Criar(bem.Id, TipoSaude.Convenio, "Amil", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), null));
        var gastoOutro = GastoSaude.Registrar(outro.Id, TipoGastoSaude.Farmacia, "Remédio", new DateOnly(2026, 3, 1), Dinheiro.De(20m, "BRL"));
        repositorioGasto.Salvar(gastoOutro);

        repositorioBem.Excluir(bem.Id);

        repositorioBem.Listar().Select(b => b.Id).ShouldBe([outro.Id]);
        repositorioGarantia.Listar().ShouldBeEmpty();
        repositorioManutencao.Listar().ShouldBeEmpty();
        repositorioAlerta.Listar().ShouldBeEmpty();
        repositorioSaude.Listar().ShouldBeEmpty();
        repositorioGasto.Listar().Select(g => g.Id).ShouldBe([gastoOutro.Id]);

        repositorioGasto.Excluir(gastoOutro.Id);
        repositorioGasto.Listar().ShouldBeEmpty();
    }

    [Fact]
    public void DevePersistirMarcacaoDeAlertaAutomatico()
    {
        var banco = BancoTemporario();
        banco.Inicializar();
        var repositorioBem = new RepositorioBemSqlite(banco.FabricaConexao);
        var repositorioAlerta = new RepositorioAlertaSqlite(banco.FabricaConexao);
        var bem = Bem.Criar(new NomeBem("Câmera"), TipoBem.Eletronico);
        repositorioBem.Salvar(bem);
        var automatico = Alerta.CriarAutomatico(bem.Id, TipoAlerta.ConvenioVencendo, SeveridadeAlerta.Aviso, "Convênio vence em 01/01/2027.", new DateOnly(2026, 12, 1), null);
        var manual = Alerta.Criar(bem.Id, TipoAlerta.GarantiaVencendo, SeveridadeAlerta.Aviso, "Lembrete", new DateOnly(2026, 12, 1), null);
        repositorioAlerta.Salvar(automatico);
        repositorioAlerta.Salvar(manual);

        repositorioAlerta.BuscarPorId(automatico.Id)!.Automatico.ShouldBeTrue();
        repositorioAlerta.BuscarPorId(automatico.Id)!.Tipo.ShouldBe(TipoAlerta.ConvenioVencendo);
        repositorioAlerta.BuscarPorId(manual.Id)!.Automatico.ShouldBeFalse();
    }

    [Fact]
    public void MigracaoV005DeveMarcarAlertasAutomaticosJaExistentes()
    {
        var banco = BancoTemporario();
        banco.Inicializar();
        var bem = Bem.Criar(new NomeBem("Sofá"), TipoBem.Movel);
        new RepositorioBemSqlite(banco.FabricaConexao).Salvar(bem);

        using (var conexao = banco.FabricaConexao.Abrir())
        {
            using var comando = conexao.CreateCommand();
            // Volta o banco ao estado anterior à V005 e insere alertas como as versões antigas gravavam.
            comando.CommandText = $"""
                ALTER TABLE alertas DROP COLUMN automatico;
                DELETE FROM schema_migrations WHERE versao = 5;
                INSERT INTO alertas (id, bem_id, tipo, severidade, mensagem, criado_em, prazo_em, status)
                VALUES ('{Guid.NewGuid()}', '{bem.Id.Valor}', 'DOCUMENTO_AUSENTE', 'AVISO', 'Nota fiscal ausente.', '2026-01-01', NULL, 'ABERTO'),
                       ('{Guid.NewGuid()}', '{bem.Id.Valor}', 'MANUTENCAO_ATRASADA', 'CRITICO', 'Manutenção atrasada desde 01/01/2026: Limpeza.', '2026-01-02', '2026-01-01', 'ABERTO'),
                       ('{Guid.NewGuid()}', '{bem.Id.Valor}', 'GARANTIA_VENCENDO', 'AVISO', 'Lembrete criado à mão', '2026-01-01', NULL, 'ABERTO');
                """;
            comando.ExecuteNonQuery();
        }

        banco.Inicializar();

        var alertas = new RepositorioAlertaSqlite(banco.FabricaConexao).Listar();
        alertas.Count(a => a.Automatico).ShouldBe(2);
        alertas.Single(a => !a.Automatico).Mensagem.ShouldBe("Lembrete criado à mão");
    }

    private static BancoDadosAssetly BancoTemporario()
    {
        var arquivo = Path.Combine(Path.GetTempPath(), $"assetly-{Guid.NewGuid()}", "assetly.sqlite3");
        return new BancoDadosAssetly(new FabricaConexaoSqlite(arquivo));
    }
}
