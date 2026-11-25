using Assetly.Aplicacao.CasoDeUso;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;
using Assetly.Infraestrutura.Armazenamento;
using Assetly.Infraestrutura.Banco;
using Assetly.Infraestrutura.Repositorio;
using Shouldly;

namespace Assetly.Testes.Infraestrutura;

public sealed class ServicoBackupLocalTests : IDisposable
{
    private readonly string _raiz = Path.Combine(Path.GetTempPath(), $"assetly-backup-{Guid.NewGuid()}");

    private string ArquivoBanco => Path.Combine(_raiz, "dados", "banco", "assetly.sqlite3");

    private string DiretorioDocumentos => Path.Combine(_raiz, "dados", "documentos");

    private string DiretorioDestino => Path.Combine(_raiz, "destino");

    [Fact]
    public void DeveCopiarBancoEDocumentosParaUmInstantaneoNoDestino()
    {
        var banco = CriarBanco();
        new RepositorioBemSqlite(banco.FabricaConexao).Salvar(Bem.Criar(new NomeBem("Notebook"), TipoBem.Eletronico));
        Directory.CreateDirectory(Path.Combine(DiretorioDocumentos, "subpasta"));
        File.WriteAllText(Path.Combine(DiretorioDocumentos, "nota.pdf"), "conteudo-nota");
        File.WriteAllText(Path.Combine(DiretorioDocumentos, "subpasta", "recibo.pdf"), "conteudo-recibo");

        var caminhoGerado = new ServicoBackupLocal().ExecutarBackup(ArquivoBanco, DiretorioDocumentos, DiretorioDestino);

        Directory.Exists(caminhoGerado).ShouldBeTrue();
        var bancoCopiado = Path.Combine(caminhoGerado, "banco", "assetly.sqlite3");
        new RepositorioBemSqlite(new FabricaConexaoSqlite(bancoCopiado)).Listar().Single().Nome.Valor.ShouldBe("Notebook");
        File.ReadAllText(Path.Combine(caminhoGerado, "documentos", "nota.pdf")).ShouldBe("conteudo-nota");
        File.ReadAllText(Path.Combine(caminhoGerado, "documentos", "subpasta", "recibo.pdf")).ShouldBe("conteudo-recibo");
    }

    [Fact]
    public void DeveGerarInstantaneosDistintosNoMesmoSegundo()
    {
        CriarBanco();
        var servico = new ServicoBackupLocal();

        var primeiro = servico.ExecutarBackup(ArquivoBanco, DiretorioDocumentos, DiretorioDestino);
        var segundo = servico.ExecutarBackup(ArquivoBanco, DiretorioDocumentos, DiretorioDestino);

        segundo.ShouldNotBe(primeiro);
        Directory.Exists(segundo).ShouldBeTrue();
    }

    [Fact]
    public void DeveRecusarDestinoDentroDaPastaDeDados()
    {
        CriarBanco();
        Directory.CreateDirectory(DiretorioDocumentos);
        var servico = new ServicoBackupLocal();

        Should.Throw<InvalidOperationException>(() => servico.ExecutarBackup(ArquivoBanco, DiretorioDocumentos, Path.Combine(DiretorioDocumentos, "backups")));
        Should.Throw<InvalidOperationException>(() => servico.ExecutarBackup(ArquivoBanco, DiretorioDocumentos, Path.GetDirectoryName(ArquivoBanco)!));
        ServicoBackupLocal.DestinoConflitaComDados(DiretorioDestino, ArquivoBanco, DiretorioDocumentos).ShouldBeFalse();
        ServicoBackupLocal.DestinoConflitaComDados(DiretorioDocumentos + "-backup", ArquivoBanco, DiretorioDocumentos).ShouldBeFalse();
    }

    [Fact]
    public void DeveRestaurarBackupGuardandoCopiaDeSegurancaERelocalizandoDocumentos()
    {
        var banco = CriarBanco();
        var repositorioBem = new RepositorioBemSqlite(banco.FabricaConexao);
        var repositorioDocumento = new RepositorioDocumentoSqlite(banco.FabricaConexao);
        var repositorioConfiguracao = new RepositorioConfiguracaoBackupSqlite(banco.FabricaConexao);
        var armazenamento = new ArmazenamentoDocumentoLocal(DiretorioDocumentos);
        var servico = new ServicoBackupLocal();

        var original = Bem.Criar(new NomeBem("Geladeira"), TipoBem.Eletrodomestico);
        repositorioBem.Salvar(original);
        Directory.CreateDirectory(DiretorioDocumentos);
        File.WriteAllText(Path.Combine(DiretorioDocumentos, "nota-geladeira.pdf"), "nota");
        // Documento registrado com caminho de outro perfil — como num backup vindo de outro computador.
        repositorioDocumento.Salvar(Documento.Registrar(original.Id, TipoDocumento.NotaFiscal, new NomeDocumento("Nota"),
            new CaminhoDocumentoLocal(@"C:\Users\outra-pessoa\.assetly\documentos\nota-geladeira.pdf"), new DateOnly(2026, 1, 1)));
        var instantaneo = servico.ExecutarBackup(ArquivoBanco, DiretorioDocumentos, DiretorioDestino);

        new ExcluirBem(repositorioBem, repositorioDocumento, armazenamento).Executar(original.Id);
        repositorioBem.Salvar(Bem.Criar(new NomeBem("Bem criado depois do backup"), TipoBem.Outro));
        File.Delete(Path.Combine(DiretorioDocumentos, "nota-geladeira.pdf"));
        new DefinirDestinoBackup(repositorioConfiguracao).Executar(new Assetly.Aplicacao.Dados.DefinirDestinoBackupComando(DiretorioDestino));

        var resultado = new RestaurarBackup(repositorioConfiguracao, repositorioDocumento, servico, armazenamento, banco.Inicializar,
            ArquivoBanco, DiretorioDocumentos, Path.Combine(_raiz, "dados", "seguranca")).Executar(instantaneo);

        repositorioBem.Listar().Select(b => b.Nome.Valor).ShouldBe(["Geladeira"]);
        var documento = repositorioDocumento.Listar().Single();
        documento.CaminhoLocal.Valor.ShouldBe(Path.Combine(DiretorioDocumentos, "nota-geladeira.pdf"));
        File.Exists(documento.CaminhoLocal.Valor).ShouldBeTrue();
        resultado.DocumentosRelocalizados.ShouldBe(1);
        resultado.DocumentosSemArquivo.ShouldBe(0);
        repositorioConfiguracao.Obter().CaminhoDestino.ShouldBe(DiretorioDestino);

        var bancoSeguranca = Path.Combine(resultado.CaminhoCopiaSeguranca, "banco", "assetly.sqlite3");
        new RepositorioBemSqlite(new FabricaConexaoSqlite(bancoSeguranca)).Listar().Single().Nome.Valor.ShouldBe("Bem criado depois do backup");
    }

    [Fact]
    public void RestauracaoApagaArquivosQueNenhumDocumentoRestauradoUsa()
    {
        var banco = CriarBanco();
        var repositorioBem = new RepositorioBemSqlite(banco.FabricaConexao);
        var repositorioDocumento = new RepositorioDocumentoSqlite(banco.FabricaConexao);
        var armazenamento = new ArmazenamentoDocumentoLocal(DiretorioDocumentos);
        var servico = new ServicoBackupLocal();
        var bem = Bem.Criar(new NomeBem("Geladeira"), TipoBem.Eletrodomestico);
        repositorioBem.Salvar(bem);
        Directory.CreateDirectory(DiretorioDocumentos);
        var nota = Path.Combine(DiretorioDocumentos, "nota.pdf");
        File.WriteAllText(nota, "nota");
        repositorioDocumento.Salvar(Documento.Registrar(bem.Id, TipoDocumento.NotaFiscal, new NomeDocumento("Nota"), new CaminhoDocumentoLocal(nota), new DateOnly(2026, 1, 1)));
        var instantaneo = servico.ExecutarBackup(ArquivoBanco, DiretorioDocumentos, DiretorioDestino);

        // Depois do backup: um arquivo que o backup não conhece (e que nenhum documento restaurado usa).
        var sobra = Path.Combine(DiretorioDocumentos, "recibo-cadastrado-depois.pdf");
        File.WriteAllText(sobra, "recibo");
        var seguranca = Path.Combine(_raiz, "dados", "seguranca");

        new RestaurarBackup(new RepositorioConfiguracaoBackupSqlite(banco.FabricaConexao), repositorioDocumento, servico, armazenamento, banco.Inicializar,
            ArquivoBanco, DiretorioDocumentos, seguranca).Executar(instantaneo);

        File.Exists(nota).ShouldBeTrue();
        File.Exists(sobra).ShouldBeFalse();
        // A cópia de segurança guardou o arquivo antes de ele ser apagado.
        Directory.GetFiles(seguranca, "recibo-cadastrado-depois.pdf", SearchOption.AllDirectories).Length.ShouldBe(1);
        Directory.GetDirectories(Path.Combine(_raiz, "dados"), ".assetly-preparacao-*").ShouldBeEmpty();
    }

    [Fact]
    public void DeveManterSoOsBackupsMaisRecentes()
    {
        Directory.CreateDirectory(DiretorioDestino);
        foreach (var dia in new[] { "01", "02", "03", "04" })
        {
            Directory.CreateDirectory(Path.Combine(DiretorioDestino, $"Assetly-Backup-2026-11-{dia}_120000"));
        }

        File.WriteAllText(Path.Combine(DiretorioDestino, "Assetly-Backup-2026-11-05_120000.assetly"), "cifrado");
        File.WriteAllText(Path.Combine(DiretorioDestino, "outro-arquivo.txt"), "não é backup");

        new ServicoBackupLocal().ManterUltimos(DiretorioDestino, 2);

        Directory.GetFileSystemEntries(DiretorioDestino).Select(Path.GetFileName).Order().ShouldBe(
            ["Assetly-Backup-2026-11-04_120000", "Assetly-Backup-2026-11-05_120000.assetly", "outro-arquivo.txt"]);
    }

    [Fact]
    public void DeveRecusarRestaurarPastaQueNaoEhBackup()
    {
        CriarBanco();
        Directory.CreateDirectory(Path.Combine(_raiz, "qualquer", "banco"));
        File.WriteAllText(Path.Combine(_raiz, "qualquer", "banco", "falso.sqlite3"), "não sou um banco");
        var servico = new ServicoBackupLocal();

        Should.Throw<InvalidOperationException>(() => servico.RestaurarBackup(Path.Combine(_raiz, "vazia"), ArquivoBanco, DiretorioDocumentos));
        Should.Throw<InvalidOperationException>(() => servico.RestaurarBackup(Path.Combine(_raiz, "qualquer"), ArquivoBanco, DiretorioDocumentos));
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (Directory.Exists(_raiz))
        {
            Directory.Delete(_raiz, recursive: true);
        }
    }

    private BancoDadosAssetly CriarBanco()
    {
        var banco = new BancoDadosAssetly(new FabricaConexaoSqlite(ArquivoBanco));
        banco.Inicializar();
        return banco;
    }
}
