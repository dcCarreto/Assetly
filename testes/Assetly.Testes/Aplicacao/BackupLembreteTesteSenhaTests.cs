using Assetly.Aplicacao.CasoDeUso;
using Assetly.Dominio.Backup;
using Assetly.Infraestrutura.Armazenamento;
using Assetly.Infraestrutura.Banco;
using Assetly.Infraestrutura.Repositorio;
using Microsoft.Data.Sqlite;
using Shouldly;

namespace Assetly.Testes.Aplicacao;

public sealed class BackupLembreteTesteSenhaTests : IDisposable
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 25, 10, 0, 0, TimeSpan.Zero);
    private readonly string _raiz = Path.Combine(Path.GetTempPath(), $"assetly-teste-backup-{Guid.NewGuid()}");

    [Fact]
    public void LembreteInsisteQuandoNuncaHouveBackup()
    {
        var aviso = LembreteBackup.Avaliar(ConfiguracaoBackup.Padrao(), Agora, temDados: true, falhaAutomatica: null);

        aviso.ShouldNotBeNull();
        aviso.Critico.ShouldBeTrue();
        LembreteBackup.Avaliar(ConfiguracaoBackup.Padrao(), Agora, temDados: false, falhaAutomatica: null).ShouldBeNull();
    }

    [Fact]
    public void LembreteAvisaBackupAntigoESilenciaBackupRecente()
    {
        var antigo = ConfiguracaoBackup.Restaurar(@"E:\b", false, FrequenciaBackup.Diaria, Agora.AddDays(-45));
        var recente = ConfiguracaoBackup.Restaurar(@"E:\b", false, FrequenciaBackup.Diaria, Agora.AddDays(-3));

        var aviso = LembreteBackup.Avaliar(antigo, Agora, true, null)!;
        aviso.Critico.ShouldBeFalse();
        aviso.Mensagem.ShouldContain("45 dias");
        LembreteBackup.Avaliar(recente, Agora, true, null).ShouldBeNull();
    }

    [Fact]
    public void LembreteMostraFalhaDoBackupAutomatico()
    {
        var configuracao = ConfiguracaoBackup.Restaurar(@"E:\b", true, FrequenciaBackup.Diaria, Agora.AddHours(-2));

        var aviso = LembreteBackup.Avaliar(configuracao, Agora, true, "pendrive desconectado")!;

        aviso.Critico.ShouldBeTrue();
        aviso.Mensagem.ShouldContain("pendrive desconectado");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("senha-do-teste")]
    public void TestarBackupConfereSemAlterarOsDados(string? senha)
    {
        var (banco, documentos) = CriarDados();
        var servico = new ServicoBackupLocal();
        var backup = servico.ExecutarBackup(banco, documentos, Path.Combine(_raiz, "destino"), senha);

        var resultado = new TestarBackup(servico, documentos).Executar(backup, senha);

        resultado.Bens.ShouldBe(1);
        resultado.Documentos.ShouldBe(1);
        resultado.Completo.ShouldBeTrue();
        Directory.EnumerateDirectories(_raiz, ".assetly-preparacao-*").ShouldBeEmpty();
    }

    [Fact]
    public void TestarBackupApontaDocumentoSemArquivoESenhaErrada()
    {
        var (banco, documentos) = CriarDados();
        var servico = new ServicoBackupLocal();
        var pasta = servico.ExecutarBackup(banco, documentos, Path.Combine(_raiz, "destino"));
        foreach (var arquivo in Directory.EnumerateFiles(Path.Combine(pasta, "documentos")))
        {
            File.Delete(arquivo);
        }

        servico.TestarBackup(pasta, documentos, null).DocumentosSemArquivo.ShouldBe(1);

        var protegido = servico.ExecutarBackup(banco, documentos, Path.Combine(_raiz, "destino2"), "senha-certa-123");
        Should.Throw<InvalidOperationException>(() => servico.TestarBackup(protegido, documentos, "senha-errada-123"))
            .Message.ShouldContain("senha incorreta");
    }

    private (string Banco, string Documentos) CriarDados()
    {
        var arquivoBanco = Path.Combine(_raiz, "banco", "assetly.sqlite3");
        var documentos = Path.Combine(_raiz, "documentos");
        var bancoDados = new BancoDadosAssetly(new FabricaConexaoSqlite(arquivoBanco));
        bancoDados.Inicializar();
        var bem = Assetly.Dominio.Bem.Bem.Criar(new Assetly.Dominio.Bem.NomeBem("TV"), Assetly.Dominio.Bem.TipoBem.Eletronico);
        new RepositorioBemSqlite(bancoDados.FabricaConexao).Salvar(bem);
        Directory.CreateDirectory(documentos);
        var arquivo = Path.Combine(documentos, "nota.pdf");
        File.WriteAllText(arquivo, "nota");
        new RepositorioDocumentoSqlite(bancoDados.FabricaConexao).Salvar(Assetly.Dominio.Documento.Documento.Registrar(bem.Id,
            Assetly.Dominio.Documento.TipoDocumento.NotaFiscal, new Assetly.Dominio.Documento.NomeDocumento("Nota"), new Assetly.Dominio.Documento.CaminhoDocumentoLocal(arquivo),
            DateOnly.FromDateTime(DateTime.Today)));
        return (arquivoBanco, documentos);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_raiz))
        {
            Directory.Delete(_raiz, recursive: true);
        }
    }
}
