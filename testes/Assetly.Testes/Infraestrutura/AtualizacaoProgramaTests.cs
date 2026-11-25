using Assetly.Dominio.Bem;
using Assetly.Infraestrutura.Armazenamento;
using Assetly.Infraestrutura.Atualizacao;
using Assetly.Infraestrutura.Banco;
using Assetly.Infraestrutura.Repositorio;
using Microsoft.Data.Sqlite;
using Shouldly;

namespace Assetly.Testes.Infraestrutura;

/// <summary>Garantias que tornam a atualização automática do programa segura para os dados.</summary>
public sealed class AtualizacaoProgramaTests : IDisposable
{
    private readonly string _raiz = Path.Combine(Path.GetTempPath(), $"assetly-atualizacao-{Guid.NewGuid()}");

    private string ArquivoBanco => Path.Combine(_raiz, "banco", "assetly.sqlite3");

    private string DiretorioSeguranca => Path.Combine(_raiz, "seguranca");

    [Fact]
    public void BancoNovoNaoGeraCopiaDeSeguranca()
    {
        new BancoDadosAssetly(new FabricaConexaoSqlite(ArquivoBanco), DiretorioSeguranca).Inicializar();

        Directory.Exists(DiretorioSeguranca).ShouldBeFalse();
    }

    [Fact]
    public void DeveCopiarOBancoAntesDeAplicarMigracoesDeUmaVersaoNova()
    {
        // Banco idêntico ao gravado pela versão anterior do programa (sem a migração mais recente).
        var fabrica = new FabricaConexaoSqlite(ArquivoBanco);
        new MigradorBancoDados(fabrica).Migrar(MigradorBancoDados.VersaoMaisRecente - 1);
        using (var conexao = fabrica.Abrir())
        using (var comando = conexao.CreateCommand())
        {
            comando.CommandText = "INSERT INTO bens (id, nome, tipo, status, comprado_em, preco_compra, moeda, observacoes) " +
                                  "VALUES ('11111111-1111-1111-1111-111111111111', 'Dado que precisa sobreviver', 'OUTRO', 'ATIVO', NULL, NULL, NULL, '')";
            comando.ExecuteNonQuery();
        }

        var banco = new BancoDadosAssetly(fabrica, DiretorioSeguranca);
        banco.Inicializar();

        var copia = Directory.EnumerateFiles(DiretorioSeguranca, "antes-da-atualizacao-*.sqlite3").Single();
        Path.GetFileName(copia).ShouldStartWith($"antes-da-atualizacao-v{MigradorBancoDados.VersaoMaisRecente - 1:D3}-");
        new RepositorioBemSqlite(banco.FabricaConexao).Listar().Single().Nome.Valor.ShouldBe("Dado que precisa sobreviver");
        new RepositorioConfiguracaoPreferenciasSqlite(banco.FabricaConexao).Obter().AtualizarAutomaticamente.ShouldBeTrue();
    }

    [Fact]
    public void QuemJaUsavaOProgramaContinuaMantendoTodosOsBackups()
    {
        // Banco de antes da retenção (V010), com destino de backup já configurado.
        var fabrica = new FabricaConexaoSqlite(ArquivoBanco);
        new MigradorBancoDados(fabrica).Migrar(9);
        using (var conexao = fabrica.Abrir())
        using (var comando = conexao.CreateCommand())
        {
            comando.CommandText = "INSERT INTO configuracao_backup (id, caminho_destino, auto_backup_habilitado, frequencia) VALUES (1, 'E:\\Backups', 1, 'DIARIA')";
            comando.ExecuteNonQuery();
        }

        var banco = new BancoDadosAssetly(fabrica, DiretorioSeguranca);
        banco.Inicializar();

        var configuracao = new RepositorioConfiguracaoBackupSqlite(banco.FabricaConexao).Obter();
        configuracao.CaminhoDestino.ShouldBe(@"E:\Backups");
        configuracao.ManterUltimos.ShouldBe(0);
    }

    [Fact]
    public void DeveRecusarBancoGravadoPorVersaoMaisNovaSemAlterarNada()
    {
        var banco = new BancoDadosAssetly(new FabricaConexaoSqlite(ArquivoBanco), DiretorioSeguranca);
        banco.Inicializar();
        Executar(banco, "INSERT INTO schema_migrations (versao, nome, aplicada_em) VALUES (999, 'V999__futuro.sql', '2030-01-01')");

        var excecao = Should.Throw<ExcecaoBancoDados>(() => banco.Inicializar());

        excecao.Message.ShouldContain("versão mais nova");
        Directory.Exists(DiretorioSeguranca).ShouldBeFalse();
    }

    [Fact]
    public void DeveRecusarRestaurarBackupDeVersaoMaisNova()
    {
        var banco = new BancoDadosAssetly(new FabricaConexaoSqlite(ArquivoBanco));
        banco.Inicializar();
        var servico = new ServicoBackupLocal();
        var documentos = Path.Combine(_raiz, "documentos");
        var instantaneo = servico.ExecutarBackup(ArquivoBanco, documentos, Path.Combine(_raiz, "destino"));
        var bancoDoBackup = new BancoDadosAssetly(new FabricaConexaoSqlite(Path.Combine(instantaneo, "banco", "assetly.sqlite3")));
        Executar(bancoDoBackup, "INSERT INTO schema_migrations (versao, nome, aplicada_em) VALUES (999, 'V999__futuro.sql', '2030-01-01')");

        Should.Throw<InvalidOperationException>(() => servico.RestaurarBackup(instantaneo, ArquivoBanco, documentos))
            .Message.ShouldContain("versão mais nova");
    }

    [Fact]
    public void DevePersistirPreferenciaDeAtualizacaoAutomatica()
    {
        var banco = new BancoDadosAssetly(new FabricaConexaoSqlite(ArquivoBanco));
        banco.Inicializar();
        var repositorio = new RepositorioConfiguracaoPreferenciasSqlite(banco.FabricaConexao);
        repositorio.Obter().AtualizarAutomaticamente.ShouldBeTrue();

        new Assetly.Aplicacao.CasoDeUso.DefinirAtualizacaoAutomatica(repositorio).Executar(false);

        repositorio.Obter().AtualizarAutomaticamente.ShouldBeFalse();
    }


    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_raiz))
        {
            Directory.Delete(_raiz, recursive: true);
        }
    }

    private static void Executar(BancoDadosAssetly banco, string sql)
    {
        using var conexao = banco.FabricaConexao.Abrir();
        using var comando = conexao.CreateCommand();
        comando.CommandText = sql;
        comando.ExecuteNonQuery();
    }
}
