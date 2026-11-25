using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Assetly.Aplicacao.Atualizacao;
using Assetly.Aplicacao.Servico;

namespace Assetly.Infraestrutura.Atualizacao;

/// <summary>
/// Atualizações a partir das Releases do GitHub (repositório público, sem token). Cada release publicada
/// pelo workflow <c>release.yml</c> tem como tag a data (<c>AAAA.MM.DD</c>) e traz o instalador Inno Setup
/// <c>Assetly-Setup-AAAA.MM.DD.exe</c> e o arquivo <c>.sha256</c> correspondente. O download só é aceito
/// se o SHA-256 conferir.
/// </summary>
public sealed class ServicoAtualizacaoGithub : IServicoAtualizacao
{
    public const string Repositorio = "dcCarreto/Assetly";
    public const string PaginaReleases = "https://github.com/" + Repositorio + "/releases";
    private const string UrlUltimaRelease = "https://api.github.com/repos/" + Repositorio + "/releases/latest";

    /// <summary>Arquivo criado pelo instalador Inno Setup na pasta do programa (desinstalador).</summary>
    internal const string MarcadorInstalacao = "unins000.exe";

    private readonly HttpClient _http;
    private readonly string _diretorioPrograma;
    private readonly string _diretorioDownloads;
    private readonly VersaoPorData? _versaoAtual;
    private ReleaseEncontrada? _encontrada;
    private string? _instaladorBaixado;

    public ServicoAtualizacaoGithub()
        : this(new HttpClient(), AppContext.BaseDirectory, Path.Combine(Path.GetTempPath(), "Assetly-Atualizacao"), VersaoDoPrograma())
    {
    }

    public ServicoAtualizacaoGithub(HttpClient http, string diretorioPrograma, string diretorioDownloads, VersaoPorData? versaoAtual)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _diretorioPrograma = diretorioPrograma;
        _diretorioDownloads = diretorioDownloads;
        _versaoAtual = versaoAtual;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Assetly-Atualizador");
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _http.Timeout = TimeSpan.FromMinutes(10);
    }

    /// <summary>Versão do executável em execução (atributo de versão gravado na compilação da release).</summary>
    public static VersaoPorData? VersaoDoPrograma() =>
        VersaoPorData.TentarLer(Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    public bool Disponivel => _versaoAtual is not null && File.Exists(Path.Combine(_diretorioPrograma, MarcadorInstalacao));

    public string VersaoAtual => _versaoAtual?.Exibicao ?? "desenvolvimento";

    public async Task<string?> ProcurarAsync(CancellationToken cancelamento = default)
    {
        if (!Disponivel)
        {
            return null;
        }

        using var resposta = await _http.GetAsync(UrlUltimaRelease, cancelamento).ConfigureAwait(false);
        if (resposta.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        resposta.EnsureSuccessStatusCode();
        await using var conteudo = await resposta.Content.ReadAsStreamAsync(cancelamento).ConfigureAwait(false);
        using var json = await JsonDocument.ParseAsync(conteudo, cancellationToken: cancelamento).ConfigureAwait(false);
        _encontrada = InterpretarRelease(json.RootElement);
        return _encontrada is not null && _encontrada.Versao > _versaoAtual! ? _encontrada.Versao.Exibicao : null;
    }

    public async Task BaixarAsync(Action<int>? progresso, CancellationToken cancelamento = default)
    {
        var release = _encontrada ?? throw new InvalidOperationException("nenhuma atualização foi encontrada ainda. Procure por atualizações primeiro.");
        Directory.CreateDirectory(_diretorioDownloads);
        var destino = Path.Combine(_diretorioDownloads, release.NomeInstalador);
        ApagarInstaladoresAntigos(destino);
        var esperado = await ObterSha256Async(release, cancelamento).ConfigureAwait(false);

        using (var resposta = await _http.GetAsync(release.UrlInstalador, HttpCompletionOption.ResponseHeadersRead, cancelamento).ConfigureAwait(false))
        {
            resposta.EnsureSuccessStatusCode();
            var total = resposta.Content.Headers.ContentLength;
            await using var entrada = await resposta.Content.ReadAsStreamAsync(cancelamento).ConfigureAwait(false);
            await using var saida = File.Create(destino + ".parcial");
            var buffer = new byte[81920];
            long lidos = 0;
            var ultimoPercentual = -1;
            int quantidade;
            while ((quantidade = await entrada.ReadAsync(buffer, cancelamento).ConfigureAwait(false)) > 0)
            {
                await saida.WriteAsync(buffer.AsMemory(0, quantidade), cancelamento).ConfigureAwait(false);
                lidos += quantidade;
                var percentual = total is > 0 ? (int)(lidos * 100 / total.Value) : 0;
                if (percentual != ultimoPercentual)
                {
                    ultimoPercentual = percentual;
                    progresso?.Invoke(percentual);
                }
            }
        }

        string obtido;
        await using (var arquivo = File.OpenRead(destino + ".parcial"))
        {
            obtido = Convert.ToHexString(await SHA256.HashDataAsync(arquivo, cancelamento).ConfigureAwait(false));
        }

        if (!string.Equals(obtido, esperado, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(destino + ".parcial");
            throw new InvalidOperationException("o instalador baixado não confere com o publicado (SHA-256 diferente). A atualização foi descartada por segurança.");
        }

        File.Move(destino + ".parcial", destino, overwrite: true);
        _instaladorBaixado = destino;
        progresso?.Invoke(100);
    }

    public void IniciarInstalacao(bool reabrirDepois)
    {
        var instalador = _instaladorBaixado ?? throw new InvalidOperationException("nenhuma versão nova foi baixada ainda.");
        var argumentos = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /NOCANCEL" + (reabrirDepois ? " /REABRIR" : string.Empty);
        Process.Start(new ProcessStartInfo(instalador, argumentos) { UseShellExecute = true });
    }

    /// <summary>Cada versão baixada deixaria um instalador de dezenas de MB na pasta temporária: só o atual fica.</summary>
    private void ApagarInstaladoresAntigos(string manter)
    {
        foreach (var arquivo in Directory.EnumerateFiles(_diretorioDownloads, "Assetly-Setup-*"))
        {
            if (string.Equals(arquivo, manter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                File.Delete(arquivo);
            }
            catch (Exception excecao) when (excecao is IOException or UnauthorizedAccessException)
            {
                // Um instalador ainda em execução fica para a próxima vez.
            }
        }
    }

    /// <summary>Lê a resposta da API de releases: versão (tag) e os arquivos do instalador e do SHA-256.</summary>
    internal static ReleaseEncontrada? InterpretarRelease(JsonElement release)
    {
        if (release.TryGetProperty("draft", out var rascunho) && rascunho.GetBoolean()
            || release.TryGetProperty("prerelease", out var previa) && previa.GetBoolean())
        {
            return null;
        }

        var versao = VersaoPorData.TentarLer(release.GetProperty("tag_name").GetString());
        if (versao is null || !release.TryGetProperty("assets", out var arquivos))
        {
            return null;
        }

        string? nomeInstalador = null, urlInstalador = null, urlSha = null, digest = null;
        foreach (var arquivo in arquivos.EnumerateArray())
        {
            var nome = arquivo.GetProperty("name").GetString() ?? string.Empty;
            var url = arquivo.GetProperty("browser_download_url").GetString();
            if (nome.StartsWith("Assetly-Setup-", StringComparison.OrdinalIgnoreCase) && nome.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                nomeInstalador = nome;
                urlInstalador = url;
                digest = arquivo.TryGetProperty("digest", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;
            }
            else if (nome.StartsWith("Assetly-Setup-", StringComparison.OrdinalIgnoreCase) && nome.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase))
            {
                urlSha = url;
            }
        }

        if (nomeInstalador is null || urlInstalador is null)
        {
            return null;
        }

        var shaDigest = digest is not null && digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) ? digest[7..] : null;
        return new ReleaseEncontrada(versao, nomeInstalador, urlInstalador, urlSha, shaDigest);
    }

    private async Task<string> ObterSha256Async(ReleaseEncontrada release, CancellationToken cancelamento)
    {
        if (release.UrlSha256 is not null)
        {
            var texto = await _http.GetStringAsync(release.UrlSha256, cancelamento).ConfigureAwait(false);
            var hash = texto.Split([' ', '\t', '\r', '\n', '*'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (hash is { Length: 64 })
            {
                return hash;
            }
        }

        return release.Sha256Digest
            ?? throw new InvalidOperationException("a versão publicada não informa o SHA-256 do instalador; a atualização não será instalada por segurança.");
    }

    internal sealed record ReleaseEncontrada(VersaoPorData Versao, string NomeInstalador, string UrlInstalador, string? UrlSha256, string? Sha256Digest);
}
