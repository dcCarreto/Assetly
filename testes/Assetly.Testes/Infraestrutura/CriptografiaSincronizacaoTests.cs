using System.IO.Compression;
using Assetly.Aplicacao.CasoDeUso;
using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;
using Assetly.Dominio.Backup;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Documento;
using Assetly.Infraestrutura.Armazenamento;
using Assetly.Infraestrutura.Banco;
using Assetly.Infraestrutura.Leitura;
using Assetly.Infraestrutura.Repositorio;
using Assetly.Infraestrutura.Sincronizacao;
using Assetly.Testes.Dominio;
using Microsoft.Data.Sqlite;
using Shouldly;
using ZXing;
using ZXing.Common;

namespace Assetly.Testes.Infraestrutura;

/// <summary>Backup criptografado (.assetly), sincronização entre dois "computadores", persistência dos campos
/// novos (V008) e leitura de QR Code — tudo com SQLite e arquivos reais numa pasta temporária.</summary>
public sealed class CriptografiaSincronizacaoTests : IDisposable
{
    private const string Senha = "senha-forte-de-teste";
    private readonly string _raiz = Path.Combine(Path.GetTempPath(), $"assetly-cripto-{Guid.NewGuid()}");

    // ---------- Arquivo protegido ----------

    [Theory]
    [InlineData(3 * 1024 * 1024 + 123)]
    [InlineData(0)]
    [InlineData(1024 * 1024)]
    [InlineData(2 * 1024 * 1024)]
    public void ArquivoProtegidoDeveIrEVoltarComESemSenha(int tamanho)
    {
        var conteudo = new byte[tamanho];
        Random.Shared.NextBytes(conteudo);

        foreach (var senha in new[] { Senha, null })
        {
            using var cifrado = new MemoryStream();
            ArquivoProtegido.Escrever(new MemoryStream(conteudo), cifrado, senha);
            cifrado.Position = 0;
            using var decifrado = new MemoryStream();
            ArquivoProtegido.Ler(cifrado, decifrado, senha);
            decifrado.ToArray().ShouldBe(conteudo);
        }
    }

    [Fact]
    public void ArquivoProtegidoDeveRecusarSenhaErradaAlteracaoETruncamento()
    {
        var conteudo = new byte[2 * 1024 * 1024 + 7];
        Random.Shared.NextBytes(conteudo);
        using var cifrado = new MemoryStream();
        ArquivoProtegido.Escrever(new MemoryStream(conteudo), cifrado, Senha);
        var bytes = cifrado.ToArray();

        Should.Throw<InvalidOperationException>(() => ArquivoProtegido.Ler(new MemoryStream(bytes), new MemoryStream(), "outra-senha"))
            .Message.ShouldContain("senha incorreta");

        var alterado = (byte[])bytes.Clone();
        alterado[bytes.Length / 2] ^= 0x5A;
        Should.Throw<InvalidOperationException>(() => ArquivoProtegido.Ler(new MemoryStream(alterado), new MemoryStream(), Senha));

        var truncado = bytes[..(bytes.Length - 1024 * 1024)];
        Should.Throw<InvalidOperationException>(() => ArquivoProtegido.Ler(new MemoryStream(truncado), new MemoryStream(), Senha));

        Should.Throw<InvalidOperationException>(() => ArquivoProtegido.Ler(new MemoryStream(bytes), new MemoryStream(), null));
    }

    // ---------- Backup criptografado de ponta a ponta ----------

    [Fact]
    public void BackupComSenhaDeveGerarArquivoUnicoCriptografadoERestaurar()
    {
        var origem = new Computador(Path.Combine(_raiz, "origem"));
        var bem = Bem.Criar(new NomeBem("Notebook secreto"), TipoBem.Eletronico, new DateOnly(2025, 1, 10), Dinheiro.De(5000m, "BRL"));
        origem.Bens.Salvar(bem);
        File.WriteAllText(Path.Combine(origem.DiretorioDocumentos, "nota.txt"), "conteúdo da nota");
        var servico = new ServicoBackupLocal();

        var gerado = servico.ExecutarBackup(origem.ArquivoBanco, origem.DiretorioDocumentos, Path.Combine(_raiz, "destino"), Senha);

        gerado.ShouldEndWith(ArquivoProtegido.Extensao);
        ArquivoProtegido.EhProtegidoPorSenha(gerado).ShouldBeTrue();
        File.ReadAllBytes(gerado).AsSpan().IndexOf("Notebook secreto"u8).ShouldBe(-1);

        var destino = new Computador(Path.Combine(_raiz, "destino-restauracao"));
        Should.Throw<InvalidOperationException>(() => servico.RestaurarBackup(gerado, destino.ArquivoBanco, destino.DiretorioDocumentos, "errada"));
        destino.Bens.Listar().ShouldBeEmpty();

        servico.RestaurarBackup(gerado, destino.ArquivoBanco, destino.DiretorioDocumentos, Senha);
        SqliteConnection.ClearAllPools();
        destino.Bens.Listar().Single().Nome.Valor.ShouldBe("Notebook secreto");
        File.ReadAllText(Path.Combine(destino.DiretorioDocumentos, "nota.txt")).ShouldBe("conteúdo da nota");
    }

    [Fact]
    public void ExecutarBackupAgoraDeveUsarASenhaGuardada()
    {
        var computador = new Computador(Path.Combine(_raiz, "pc"));
        new DefinirDestinoBackup(computador.ConfiguracaoBackup).Executar(new Assetly.Aplicacao.Dados.DefinirDestinoBackupComando(Path.Combine(_raiz, "backups")));
        new AtivarCriptografiaBackup(computador.ConfiguracaoBackup, computador.Protecao).Executar(Senha, Senha);

        var caminho = new ExecutarBackupAgora(computador.ConfiguracaoBackup, new ServicoBackupLocal(), computador.ArquivoBanco, computador.DiretorioDocumentos, computador.Protecao).Executar();

        ArquivoProtegido.EhProtegidoPorSenha(caminho).ShouldBeTrue();
        var configuracao = computador.ConfiguracaoBackup.Obter();
        configuracao.Criptografado.ShouldBeTrue();
        configuracao.SenhaProtegida.ShouldNotBe(Senha);
        SenhaBackup.Obter(configuracao, computador.Protecao).ShouldBe(Senha);
    }

    [Fact]
    public void SenhaCurtaOuSemConfirmacaoDeveSerRecusada()
    {
        var computador = new Computador(Path.Combine(_raiz, "pc"));
        var ativar = new AtivarCriptografiaBackup(computador.ConfiguracaoBackup, computador.Protecao);

        Should.Throw<ArgumentException>(() => ativar.Executar("curta", "curta"));
        Should.Throw<ArgumentException>(() => ativar.Executar(Senha, Senha + "x"));
        computador.ConfiguracaoBackup.Obter().Criptografado.ShouldBeFalse();
    }

    // ---------- Sincronização entre dois computadores ----------

    [Fact]
    public void DoisComputadoresDevemSincronizarPelaPastaEDetectarConflito()
    {
        var pasta = Path.Combine(_raiz, "nuvem");
        Directory.CreateDirectory(pasta);
        var casa = new Computador(Path.Combine(_raiz, "casa"));
        var trabalho = new Computador(Path.Combine(_raiz, "trabalho"));
        foreach (var pc in new[] { casa, trabalho })
        {
            new ConfigurarSincronizacao(pc.ConfiguracaoSincronizacao).Ativar(pasta);
            new AtivarCriptografiaBackup(pc.ConfiguracaoBackup, pc.Protecao).Executar(Senha, Senha);
        }

        casa.Bens.Salvar(Bem.Criar(new NomeBem("Geladeira"), TipoBem.Eletrodomestico));
        casa.Sincronizar(EscolhaConflito.Cancelar).Acao.ShouldBe(AcaoSincronizacao.Enviar);
        ArquivoProtegido.EhProtegidoPorSenha(Path.Combine(pasta, "Assetly-Sincronizacao.assetly")).ShouldBeTrue();

        trabalho.Sincronizar(EscolhaConflito.Cancelar).Acao.ShouldBe(AcaoSincronizacao.Receber);
        SqliteConnection.ClearAllPools();
        trabalho.Bens.Listar().Single().Nome.Valor.ShouldBe("Geladeira");
        // A configuração de sincronização de cada computador sobrevive à restauração.
        trabalho.ConfiguracaoSincronizacao.Obter().Ativa.ShouldBeTrue();

        trabalho.Sincronizar(EscolhaConflito.Cancelar).Acao.ShouldBe(AcaoSincronizacao.NadaAFazer);
        casa.Sincronizar(EscolhaConflito.Cancelar).Acao.ShouldBe(AcaoSincronizacao.NadaAFazer);

        // Os dois mudam: conflito; cancelar não altera nada, escolher resolve.
        casa.Bens.Salvar(Bem.Criar(new NomeBem("Sofá"), TipoBem.Movel));
        trabalho.Bens.Salvar(Bem.Criar(new NomeBem("Monitor"), TipoBem.Eletronico));
        trabalho.Sincronizar(EscolhaConflito.Cancelar).Acao.ShouldBe(AcaoSincronizacao.Enviar);
        var conflito = casa.Sincronizar(EscolhaConflito.Cancelar);
        conflito.Acao.ShouldBe(AcaoSincronizacao.Conflito);
        conflito.Executada.ShouldBeFalse();
        casa.Bens.Listar().Count.ShouldBe(2);

        casa.Sincronizar(EscolhaConflito.UsarDaPasta).Acao.ShouldBe(AcaoSincronizacao.Receber);
        SqliteConnection.ClearAllPools();
        casa.Bens.Listar().Select(b => b.Nome.Valor).OrderBy(n => n).ShouldBe(["Geladeira", "Monitor"]);
    }

    [Fact]
    public void ReceberCopiaProtegidaSemSenhaConfiguradaDeveExplicarOProblema()
    {
        var pasta = Path.Combine(_raiz, "nuvem");
        Directory.CreateDirectory(pasta);
        var casa = new Computador(Path.Combine(_raiz, "casa"));
        var trabalho = new Computador(Path.Combine(_raiz, "trabalho"));
        new ConfigurarSincronizacao(casa.ConfiguracaoSincronizacao).Ativar(pasta);
        new ConfigurarSincronizacao(trabalho.ConfiguracaoSincronizacao).Ativar(pasta);
        new AtivarCriptografiaBackup(casa.ConfiguracaoBackup, casa.Protecao).Executar(Senha, Senha);
        casa.Bens.Salvar(Bem.Criar(new NomeBem("TV"), TipoBem.Eletronico));
        casa.Sincronizar(EscolhaConflito.Cancelar);

        // O trabalho é um computador novo (sem dados): tenta receber, mas a cópia exige a senha que ele não tem.
        Should.Throw<InvalidOperationException>(() => trabalho.Sincronizar(EscolhaConflito.UsarDaPasta))
            .Message.ShouldContain("protegida por senha");
    }

    [Fact]
    public void EscritaEmPedacosGeraOMesmoConteudoQueAEscritaDeUmaVez()
    {
        var conteudo = new byte[1024 * 1024 + 4096];
        Random.Shared.NextBytes(conteudo);
        using var cifrado = new MemoryStream();
        using (var escrita = ArquivoProtegido.AbrirEscrita(cifrado, Senha))
        {
            for (var inicio = 0; inicio < conteudo.Length; inicio += 3000)
            {
                escrita.Write(conteudo, inicio, Math.Min(3000, conteudo.Length - inicio));
            }
        }

        // O fluxo de saída continua aberto (quem o criou é quem fecha).
        cifrado.Position = 0;
        using var decifrado = new MemoryStream();
        ArquivoProtegido.Ler(cifrado, decifrado, Senha);
        decifrado.ToArray().ShouldBe(conteudo);
    }

    [Fact]
    public void EnvioAutomaticoNaoRecebeNemMarcaComoSincronizado()
    {
        var pasta = Path.Combine(_raiz, "nuvem");
        Directory.CreateDirectory(pasta);
        var casa = new Computador(Path.Combine(_raiz, "casa"));
        var trabalho = new Computador(Path.Combine(_raiz, "trabalho"));
        new ConfigurarSincronizacao(casa.ConfiguracaoSincronizacao).Ativar(pasta);
        new ConfigurarSincronizacao(trabalho.ConfiguracaoSincronizacao).Ativar(pasta);
        casa.Bens.Salvar(Bem.Criar(new NomeBem("Geladeira"), TipoBem.Eletrodomestico));
        casa.Sincronizar(EscolhaConflito.Cancelar);

        // Durante o uso e ao fechar ("só enviar"): a mudança da pasta não substitui os dados deste computador.
        var automatico = trabalho.Sincronizar(EscolhaConflito.Cancelar, permitirReceber: false);
        automatico.Acao.ShouldBe(AcaoSincronizacao.Receber);
        automatico.Executada.ShouldBeFalse();
        trabalho.Bens.Listar().ShouldBeEmpty();
        trabalho.ConfiguracaoSincronizacao.Obter().UltimaImpressao.ShouldBeNull();

        // Ao abrir (ou em "Sincronizar agora") a mudança continua pendente e é recebida.
        trabalho.Sincronizar(EscolhaConflito.Cancelar).Executada.ShouldBeTrue();
        SqliteConnection.ClearAllPools();
        trabalho.Bens.Listar().Single().Nome.Valor.ShouldBe("Geladeira");
    }

    [Fact]
    public void RestauracoesGuardamCopiasDeSegurancaCifradasEMantemSoAsUltimas()
    {
        var pc = new Computador(Path.Combine(_raiz, "pc"));
        new AtivarCriptografiaBackup(pc.ConfiguracaoBackup, pc.Protecao).Executar(Senha, Senha);
        pc.Bens.Salvar(Bem.Criar(new NomeBem("TV"), TipoBem.Eletronico));
        var backup = new ServicoBackupLocal().ExecutarBackup(pc.ArquivoBanco, pc.DiretorioDocumentos, Path.Combine(_raiz, "backups"), Senha);
        var seguranca = Path.Combine(pc.Pasta, "seguranca");
        Directory.CreateDirectory(seguranca);
        var copiaMigrador = Path.Combine(seguranca, "antes-da-atualizacao-v008-2026-01-01_000000.sqlite3");
        File.WriteAllText(copiaMigrador, "cópia do migrador");

        for (var vez = 0; vez < RestaurarBackup.CopiasSegurancaRetidas + 2; vez++)
        {
            pc.Restaurar().Executar(backup, Senha);
            SqliteConnection.ClearAllPools();
        }

        var copias = Directory.GetFileSystemEntries(seguranca, "Assetly-Backup-*");
        copias.Length.ShouldBe(RestaurarBackup.CopiasSegurancaRetidas);
        copias.ShouldAllBe(copia => copia.EndsWith(ArquivoProtegido.Extensao) && ArquivoProtegido.EhProtegidoPorSenha(copia));
        File.Exists(copiaMigrador).ShouldBeTrue();
        pc.Bens.Listar().Single().Nome.Valor.ShouldBe("TV");
        // Nenhuma pasta de preparação sobra ao lado dos dados.
        Directory.GetDirectories(pc.Pasta, ".assetly-preparacao-*").ShouldBeEmpty();
    }

    // ---------- Persistência dos campos novos (V008) ----------

    [Fact]
    public void DevePersistirResponsavelChaveDeAcessoESincronizacao()
    {
        var pc = new Computador(Path.Combine(_raiz, "pc"));
        var pessoa = Bem.Criar(new NomeBem("Ana"), TipoBem.Pessoa);
        var bem = Bem.Criar(new NomeBem("Notebook"), TipoBem.Eletronico);
        bem.DefinirResponsavel(pessoa.Id);
        pc.Bens.Salvar(pessoa);
        pc.Bens.Salvar(bem);
        var repositorioDocumento = new RepositorioDocumentoSqlite(pc.Banco.FabricaConexao);
        var documento = Documento.Registrar(bem.Id, TipoDocumento.NotaFiscal, new NomeDocumento("Nota"), new CaminhoDocumentoLocal(@"C:\nota.pdf"), new DateOnly(2026, 11, 1));
        documento.VincularNotaEletronica(ChaveAcessoNfe.Criar(NotaFamiliaDepreciacaoTests.ChaveValida()), "https://sefaz/consulta");
        repositorioDocumento.Salvar(documento);
        new ConfigurarSincronizacao(pc.ConfiguracaoSincronizacao).Ativar(@"C:\Nuvem");

        var bemLido = pc.Bens.BuscarPorId(bem.Id)!;
        bemLido.ResponsavelId.ShouldBe(pessoa.Id);
        var documentoLido = repositorioDocumento.BuscarPorId(documento.Id)!;
        documentoLido.ChaveAcesso.ShouldBe(NotaFamiliaDepreciacaoTests.ChaveValida());
        documentoLido.UrlConsulta.ShouldBe("https://sefaz/consulta");
        pc.ConfiguracaoSincronizacao.Obter().Pasta.ShouldBe(@"C:\Nuvem");

        // Excluir a pessoa não apaga o bem: só desfaz o vínculo.
        pc.Bens.Excluir(pessoa.Id);
        pc.Bens.BuscarPorId(bem.Id)!.ResponsavelId.ShouldBeNull();
    }

    // ---------- QR Code ----------

    [Fact]
    public void DeveLerQrCodeDeUmaImagem()
    {
        var conteudo = $"https://www.fazenda.pr.gov.br/nfce/qrcode?p={NotaFamiliaDepreciacaoTests.ChaveValida()}|2|1|1|ABC";
        var escritor = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions { Width = 400, Height = 400, Margin = 4 }
        };
        var imagem = escritor.Write(conteudo);

        LeitorQrCode.Ler(imagem.Pixels, imagem.Width, imagem.Height).ShouldBe(conteudo);
        LeitorQrCode.Ler(new byte[100 * 100 * 4], 100, 100).ShouldBeNull();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_raiz))
        {
            Directory.Delete(_raiz, recursive: true);
        }
    }

    /// <summary>Proteção de senha reversível só para testes (a real usa o DPAPI do Windows).</summary>
    private sealed class ProtecaoFalsa : IProtecaoSenha
    {
        public string Proteger(string segredo) => "protegido:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(segredo));

        public string Revelar(string protegido) => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(protegido["protegido:".Length..]));
    }

    /// <summary>Um "computador": banco SQLite, pasta de documentos e os casos de uso de backup/sincronização.</summary>
    private sealed class Computador
    {
        private readonly string _pasta;

        internal Computador(string pasta)
        {
            _pasta = pasta;
            DiretorioDocumentos = Path.Combine(pasta, "documentos");
            Directory.CreateDirectory(DiretorioDocumentos);
            Banco = new BancoDadosAssetly(new FabricaConexaoSqlite(Path.Combine(pasta, "banco", "assetly.sqlite3")), Path.Combine(pasta, "seguranca"));
            Banco.Inicializar();
            Bens = new RepositorioBemSqlite(Banco.FabricaConexao);
            ConfiguracaoBackup = new RepositorioConfiguracaoBackupSqlite(Banco.FabricaConexao);
            ConfiguracaoSincronizacao = new RepositorioConfiguracaoSincronizacaoSqlite(Banco.FabricaConexao);
        }

        internal BancoDadosAssetly Banco { get; }

        internal string ArquivoBanco => Banco.FabricaConexao.ArquivoBanco;

        internal string DiretorioDocumentos { get; }

        internal IRepositorioBem Bens { get; }

        internal IRepositorioConfiguracaoBackup ConfiguracaoBackup { get; }

        internal IRepositorioConfiguracaoSincronizacao ConfiguracaoSincronizacao { get; }

        internal IProtecaoSenha Protecao { get; } = new ProtecaoFalsa();

        internal string Pasta => _pasta;

        internal RestaurarBackup Restaurar()
        {
            var fabrica = Banco.FabricaConexao;
            return new RestaurarBackup(ConfiguracaoBackup, new RepositorioDocumentoSqlite(fabrica), new ServicoBackupLocal(), new ArmazenamentoDocumentoLocal(DiretorioDocumentos),
                Banco.Inicializar, ArquivoBanco, DiretorioDocumentos, Path.Combine(_pasta, "seguranca"), ConfiguracaoSincronizacao, Protecao);
        }

        internal ResultadoSincronizacao Sincronizar(EscolhaConflito escolha, bool permitirReceber = true)
        {
            var fabrica = Banco.FabricaConexao;
            var fonte = new FonteDados(Bens, new RepositorioGarantiaSqlite(fabrica), new RepositorioManutencaoSqlite(fabrica), new RepositorioDocumentoSqlite(fabrica),
                new RepositorioAlertaSqlite(fabrica), new RepositorioSaudeSqlite(fabrica), new RepositorioGastoSaudeSqlite(fabrica), new RepositorioVeiculoSqlite(fabrica),
                new RepositorioAbastecimentoSqlite(fabrica), new RepositorioImovelSqlite(fabrica), new RepositorioDespesaSqlite(fabrica));
            var servico = new ServicoBackupLocal();
            return new SincronizarDados(ConfiguracaoSincronizacao, ConfiguracaoBackup, new ExportarDados(fonte), new SincronizacaoPastaLocal(servico),
                Restaurar(), Protecao, ArquivoBanco, DiretorioDocumentos, Path.GetFileName(_pasta)).Executar(_ => escolha, permitirReceber);
        }
    }
}
