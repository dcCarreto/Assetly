using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Assetly.Aplicacao.Atualizacao;
using Assetly.Infraestrutura.Atualizacao;
using Shouldly;

namespace Assetly.Testes.Infraestrutura;

/// <summary>Atualização automática pelas Releases do GitHub: só aceita versões mais novas, publicadas (não rascunho
/// nem prévia), com instalador e SHA-256 conferido. Nada aqui acessa a internet — as respostas são simuladas.</summary>
public sealed class ServicoAtualizacaoGithubTests : IDisposable
{
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
        var sha = Convert.ToHexString(SHA256.HashData(_instalador));
        var servico = Criar("2026.11.20", ReleaseJson("2026.11.23"), $"{sha}  Assetly-Setup-2026.11.23.exe");

        servico.Disponivel.ShouldBeTrue();
        servico.VersaoAtual.ShouldBe("20/11/2026");
        (await servico.ProcurarAsync()).ShouldBe("23/11/2026");

        var progresso = new List<int>();
        await servico.BaixarAsync(progresso.Add);

        var baixado = Path.Combine(DiretorioDownloads, "Assetly-Setup-2026.11.23.exe");
        File.ReadAllBytes(baixado).ShouldBe(_instalador);
        progresso.ShouldContain(100);
        File.Exists(baixado + ".parcial").ShouldBeFalse();
    }

    [Fact]
    public async Task DeveDescartarDownloadComSha256Diferente()
    {
        var servico = Criar("2026.11.20", ReleaseJson("2026.11.23"), new string('0', 64));
        (await servico.ProcurarAsync()).ShouldNotBeNull();

        var excecao = await Should.ThrowAsync<InvalidOperationException>(() => servico.BaixarAsync(null));

        excecao.Message.ShouldContain("SHA-256");
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
    public void DeveUsarODigestDaApiQuandoNaoHaArquivoSha256()
    {
        var json = """
            {"tag_name":"2026.11.23.2","draft":false,"prerelease":false,"assets":[
              {"name":"Assetly-Setup-2026.11.23.2.exe","browser_download_url":"https://exemplo/setup.exe","digest":"sha256:ABCDEF"}]}
            """;

        var release = Interpretar(json)!;

        release.Versao.ShouldBe(new VersaoPorData(new DateOnly(2026, 11, 23), 2));
        release.UrlSha256.ShouldBeNull();
        release.Sha256Digest.ShouldBe("ABCDEF");
    }

    public void Dispose()
    {
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

    private ServicoAtualizacaoGithub Criar(string versaoInstalada, string jsonRelease, string conteudoSha)
    {
        var respostas = new RespostasFalsas(url => url switch
        {
            _ when url.EndsWith("/releases/latest", StringComparison.Ordinal) => Resposta(Encoding.UTF8.GetBytes(jsonRelease)),
            _ when url.EndsWith(".sha256", StringComparison.Ordinal) => Resposta(Encoding.UTF8.GetBytes(conteudoSha)),
            _ when url.EndsWith(".exe", StringComparison.Ordinal) => Resposta(_instalador),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
        return new ServicoAtualizacaoGithub(new HttpClient(respostas), DiretorioPrograma, DiretorioDownloads, VersaoPorData.TentarLer(versaoInstalada));
    }

    private static HttpResponseMessage Resposta(byte[] conteudo) => new(HttpStatusCode.OK) { Content = new ByteArrayContent(conteudo) };

    private static string ReleaseJson(string tag, bool rascunho = false, bool previa = false) => $$"""
        {
          "tag_name": "{{tag}}",
          "draft": {{(rascunho ? "true" : "false")}},
          "prerelease": {{(previa ? "true" : "false")}},
          "assets": [
            { "name": "Assetly-Setup-{{tag}}.exe", "browser_download_url": "https://exemplo/Assetly-Setup-{{tag}}.exe" },
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
