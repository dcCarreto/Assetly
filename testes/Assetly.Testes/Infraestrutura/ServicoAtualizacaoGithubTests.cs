using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Assetly.Aplicacao.Atualizacao;
using Assetly.Infraestrutura.Atualizacao;
using Shouldly;

namespace Assetly.Testes.Infraestrutura;

/// <summary>Atualização automática pelas Releases do GitHub: só aceita versões mais novas, publicadas (não rascunho
/// nem prévia), com instalador e SHA-256 assinado e conferido. Nada aqui acessa a internet — as respostas são simuladas
/// e a assinatura usa uma chave criada só para o teste.</summary>
public sealed class ServicoAtualizacaoGithubTests : IDisposable
{
    private readonly ECDsa _chave = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private readonly string _raiz = Path.Combine(Path.GetTempPath(), $"assetly-github-{Guid.NewGuid()}");
    private readonly byte[] _instalador = Encoding.UTF8.GetBytes("conteúdo simulado do instalador");

    private string DiretorioPrograma => Path.Combine(_raiz, "programa");

    private string DiretorioDownloads => Path.Combine(_raiz, "downloads");

    public ServicoAtualizacaoGithubTests()
    {
        Directory.CreateDirectory(DiretorioPrograma);
        File.WriteAllText(Path.Combine(DiretorioPrograma, "unins000.exe"), "marcador do instalador");
    }

    [Fact]
    public async Task DeveEncontrarEBaixarVersaoMaisNovaConferindoOSha256()
    {
        var servico = Criar("2026.11.20", ReleaseJson("2026.11.23"), ShaDe(_instalador, "2026.11.23"));

        servico.Disponivel.ShouldBeTrue();
        servico.VersaoAtual.ShouldBe("20/11/2026");
        (await servico.ProcurarAsync()).ShouldBe("23/11/2026");
        Directory.CreateDirectory(DiretorioDownloads);
        var antigo = Path.Combine(DiretorioDownloads, "Assetly-Setup-2026.10.01.exe");
        File.WriteAllText(antigo, "instalador de uma versão anterior");

        var progresso = new List<int>();
        await servico.BaixarAsync(progresso.Add);

        var baixado = Path.Combine(DiretorioDownloads, "Assetly-Setup-2026.11.23.exe");
        File.ReadAllBytes(baixado).ShouldBe(_instalador);
        progresso.ShouldContain(100);
        File.Exists(baixado + ".parcial").ShouldBeFalse();
        // Instaladores de versões anteriores não se acumulam na pasta temporária.
        File.Exists(antigo).ShouldBeFalse();
    }

    [Fact]
    public async Task DeveDescartarDownloadComSha256Diferente()
    {
        var servico = Criar("2026.11.20", ReleaseJson("2026.11.23"), $"{new string('0', 64)}  Assetly-Setup-2026.11.23.exe");
        (await servico.ProcurarAsync()).ShouldNotBeNull();

        var excecao = await Should.ThrowAsync<InvalidOperationException>(() => servico.BaixarAsync(null));

        excecao.Message.ShouldContain("SHA-256 diferente");
        Directory.EnumerateFiles(DiretorioDownloads).ShouldBeEmpty();
        Should.Throw<InvalidOperationException>(() => servico.IniciarInstalacao(false));
    }

    [Theory]
    [InlineData("2026.11.23", "2026.11.23")]
    [InlineData("2026.11.23.2", "2026.11.23")]
    [InlineData("2026.12.01", "2026.11.23")]
    public async Task NaoDeveOferecerVersaoIgualOuMaisAntiga(string instalada, string publicada)
    {
        var servico = Criar(instalada, ReleaseJson(publicada), new string('0', 64));

        (await servico.ProcurarAsync()).ShouldBeNull();
    }

    [Fact]
    public async Task ForaDeUmaInstalacaoNaoProcuraAtualizacoes()
    {
        File.Delete(Path.Combine(DiretorioPrograma, "unins000.exe"));
        var servico = Criar("2026.11.20", ReleaseJson("2026.11.23"), string.Empty);

        servico.Disponivel.ShouldBeFalse();
        (await servico.ProcurarAsync()).ShouldBeNull();
    }

    [Fact]
    public async Task CompilacaoDeDesenvolvimentoNaoProcuraAtualizacoes()
    {
        var servico = new ServicoAtualizacaoGithub(new HttpClient(new RespostasFalsas(_ => throw new InvalidOperationException("não deveria acessar a rede"))),
            DiretorioPrograma, DiretorioDownloads, versaoAtual: null);

        servico.Disponivel.ShouldBeFalse();
        servico.VersaoAtual.ShouldBe("desenvolvimento");
        (await servico.ProcurarAsync()).ShouldBeNull();
    }

    [Fact]
    public void DeveIgnorarRascunhosPreviasEReleasesSemInstalador()
    {
        Interpretar(ReleaseJson("2026.11.23", rascunho: true)).ShouldBeNull();
        Interpretar(ReleaseJson("2026.11.23", previa: true)).ShouldBeNull();
        Interpretar(ReleaseJson("v1.2.3")).ShouldBeNull();
        Interpretar("""{"tag_name":"2026.11.23","draft":false,"prerelease":false,"assets":[]}""").ShouldBeNull();
    }

    [Fact]
    public void DeveEncontrarOSha256ESuaAssinatura()
    {
        var release = Interpretar(ReleaseJson("2026.11.23.2"))!;

        release.Versao.ShouldBe(new VersaoPorData(new DateOnly(2026, 11, 23), 2));
        release.UrlSha256.ShouldBe("https://exemplo/Assetly-Setup-2026.11.23.2.sha256");
        release.UrlAssinatura.ShouldBe("https://exemplo/Assetly-Setup-2026.11.23.2.sha256.sig");
    }

    [Fact]
    public async Task DeveRecusarSha256SemAssinaturaOuAssinadoPorOutraChave()
    {
        var sha = ShaDe(_instalador, "2026.11.23");

        // Release sem o arquivo de assinatura (ex.: publicada por quem não tem a chave privada).
        var semAssinatura = Criar("2026.11.20", ReleaseJson("2026.11.23", comAssinatura: false), sha);
        (await semAssinatura.ProcurarAsync()).ShouldNotBeNull();
        (await Should.ThrowAsync<InvalidOperationException>(() => semAssinatura.BaixarAsync(null))).Message.ShouldContain("assinado");

        using var outraChave = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var assinadoPorOutro = Criar("2026.11.20", ReleaseJson("2026.11.23"), sha, outraChave);
        (await assinadoPorOutro.ProcurarAsync()).ShouldNotBeNull();
        (await Should.ThrowAsync<InvalidOperationException>(() => assinadoPorOutro.BaixarAsync(null))).Message.ShouldContain("não confere");
        File.Exists(Path.Combine(DiretorioDownloads, "Assetly-Setup-2026.11.23.exe")).ShouldBeFalse();
    }

    [Fact]
    public void Sha256AssinadoDeOutraVersaoNaoServe()
    {
        var conteudo = Encoding.UTF8.GetBytes(ShaDe(_instalador, "2026.10.01"));
        var assinatura = Convert.ToBase64String(_chave.SignData(conteudo, HashAlgorithmName.SHA256));

        Should.Throw<InvalidOperationException>(() => ServicoAtualizacaoGithub.ConferirSha256Assinado(
            conteudo, assinatura, "Assetly-Setup-2026.11.23.exe", _chave.ExportSubjectPublicKeyInfoPem())).Message.ShouldContain("não corresponde");
        ServicoAtualizacaoGithub.ConferirSha256Assinado(conteudo, assinatura, "Assetly-Setup-2026.10.01.exe", _chave.ExportSubjectPublicKeyInfoPem())
            .ShouldBe(Convert.ToHexString(SHA256.HashData(_instalador)));
    }

    [Fact]
    public void ChavePublicaEmbutidaEhUmaChaveEcdsaValida()
    {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(ServicoAtualizacaoGithub.ChavePublica);
        ecdsa.KeySize.ShouldBe(256);
    }

    public void Dispose()
    {
        _chave.Dispose();
        if (Directory.Exists(_raiz))
        {
            Directory.Delete(_raiz, recursive: true);
        }
    }

    private static ServicoAtualizacaoGithub.ReleaseEncontrada? Interpretar(string json)
    {
        using var documento = JsonDocument.Parse(json);
        return ServicoAtualizacaoGithub.InterpretarRelease(documento.RootElement);
    }

    private static string ShaDe(byte[] instalador, string versao) => $"{Convert.ToHexString(SHA256.HashData(instalador))}  Assetly-Setup-{versao}.exe";

    private ServicoAtualizacaoGithub Criar(string versaoInstalada, string jsonRelease, string conteudoSha, ECDsa? assinante = null)
    {
        var bytesSha = Encoding.UTF8.GetBytes(conteudoSha);
        var assinatura = Convert.ToBase64String((assinante ?? _chave).SignData(bytesSha, HashAlgorithmName.SHA256));
        var respostas = new RespostasFalsas(url => url switch
        {
            _ when url.EndsWith("/releases/latest", StringComparison.Ordinal) => Resposta(Encoding.UTF8.GetBytes(jsonRelease)),
            _ when url.EndsWith(".sha256", StringComparison.Ordinal) => Resposta(bytesSha),
            _ when url.EndsWith(".sha256.sig", StringComparison.Ordinal) => Resposta(Encoding.ASCII.GetBytes(assinatura)),
            _ when url.EndsWith(".exe", StringComparison.Ordinal) => Resposta(_instalador),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
        return new ServicoAtualizacaoGithub(new HttpClient(respostas), DiretorioPrograma, DiretorioDownloads, VersaoPorData.TentarLer(versaoInstalada),
            _chave.ExportSubjectPublicKeyInfoPem());
    }

    private static HttpResponseMessage Resposta(byte[] conteudo) => new(HttpStatusCode.OK) { Content = new ByteArrayContent(conteudo) };

    private static string ReleaseJson(string tag, bool rascunho = false, bool previa = false, bool comAssinatura = true) => $$"""
        {
          "tag_name": "{{tag}}",
          "draft": {{(rascunho ? "true" : "false")}},
          "prerelease": {{(previa ? "true" : "false")}},
          "assets": [
            { "name": "Assetly-Setup-{{tag}}.exe", "browser_download_url": "https://exemplo/Assetly-Setup-{{tag}}.exe" },
            {{(comAssinatura ? $$$"""{ "name": "Assetly-Setup-{{{tag}}}.sha256.sig", "browser_download_url": "https://exemplo/Assetly-Setup-{{{tag}}}.sha256.sig" },""" : "")}}
            { "name": "Assetly-Setup-{{tag}}.sha256", "browser_download_url": "https://exemplo/Assetly-Setup-{{tag}}.sha256" }
          ]
        }
        """;

    private sealed class RespostasFalsas(Func<string, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage requisicao, CancellationToken cancelamento) =>
            Task.FromResult(responder(requisicao.RequestUri!.ToString()));
    }
}
