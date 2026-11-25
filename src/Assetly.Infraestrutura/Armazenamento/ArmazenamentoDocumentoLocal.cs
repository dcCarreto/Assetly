using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Assetly.Aplicacao.Servico;

namespace Assetly.Infraestrutura.Armazenamento;

public sealed partial class ArmazenamentoDocumentoLocal : IArmazenamentoDocumento
{
    private const string NomePadrao = "documento";

    private readonly string _diretorioDocumentos;

    public ArmazenamentoDocumentoLocal(string diretorioDocumentos)
    {
        ArgumentNullException.ThrowIfNull(diretorioDocumentos);
        _diretorioDocumentos = Path.GetFullPath(diretorioDocumentos);
    }

    public static ArmazenamentoDocumentoLocal Padrao() =>
        new(CaminhosArmazenamentoLocal.DiretorioDocumentos());

    public string Armazenar(string arquivoOrigem)
    {
        ArgumentNullException.ThrowIfNull(arquivoOrigem);
        var origem = Path.GetFullPath(arquivoOrigem);
        if (!File.Exists(origem))
        {
            throw new ArgumentException($"arquivo do documento não encontrado: {origem}");
        }

        if (EstaDentroDoDestino(origem))
        {
            return origem;
        }

        try
        {
            Directory.CreateDirectory(_diretorioDocumentos);
            var destino = Path.Combine(_diretorioDocumentos, NomeDestino(origem));
            File.Copy(origem, destino);
            return Path.GetFullPath(destino);
        }
        catch (IOException excecao)
        {
            throw new InvalidOperationException("não foi possível armazenar o documento localmente", excecao);
        }
    }

    public void Remover(string caminhoArmazenado)
    {
        if (string.IsNullOrWhiteSpace(caminhoArmazenado))
        {
            return;
        }

        var caminho = Path.GetFullPath(caminhoArmazenado);
        if (!EstaDentroDoDestino(caminho) || !File.Exists(caminho))
        {
            return;
        }

        try
        {
            File.Delete(caminho);
        }
        catch (Exception excecao) when (excecao is IOException or UnauthorizedAccessException)
        {
            // Arquivo em uso (aberto em outro programa) ou protegido: o registro já foi excluído, a cópia
            // órfã não atrapalha nada e não vale interromper o usuário por isso.
        }
    }

    public string? Localizar(string caminhoRegistrado)
    {
        if (string.IsNullOrWhiteSpace(caminhoRegistrado))
        {
            return null;
        }

        if (File.Exists(caminhoRegistrado))
        {
            return caminhoRegistrado;
        }

        var nomeArquivo = Path.GetFileName(caminhoRegistrado.Replace('/', Path.DirectorySeparatorChar));
        if (string.IsNullOrEmpty(nomeArquivo))
        {
            return null;
        }

        var candidato = Path.Combine(_diretorioDocumentos, nomeArquivo);
        return File.Exists(candidato) ? candidato : null;
    }

    public int RemoverNaoReferenciados(IReadOnlyCollection<string> caminhosReferenciados)
    {
        ArgumentNullException.ThrowIfNull(caminhosReferenciados);
        if (!Directory.Exists(_diretorioDocumentos))
        {
            return 0;
        }

        var referenciados = caminhosReferenciados
            .Where(caminho => !string.IsNullOrWhiteSpace(caminho))
            .Select(Path.GetFullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removidos = 0;
        foreach (var arquivo in Directory.EnumerateFiles(_diretorioDocumentos, "*", SearchOption.AllDirectories))
        {
            if (referenciados.Contains(Path.GetFullPath(arquivo)))
            {
                continue;
            }

            try
            {
                File.Delete(arquivo);
                removidos++;
            }
            catch (Exception excecao) when (excecao is IOException or UnauthorizedAccessException)
            {
                // Arquivo aberto em outro programa: fica para a próxima restauração.
            }
        }

        return removidos;
    }

    private bool EstaDentroDoDestino(string origem)
    {
        var relativo = Path.GetRelativePath(_diretorioDocumentos, origem);
        return !relativo.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relativo);
    }

    private static string NomeDestino(string origem)
    {
        var nomeOriginal = Path.GetFileName(origem);
        var nomeSeguro = NormalizarNomeArquivo(string.IsNullOrEmpty(nomeOriginal) ? NomePadrao : nomeOriginal);
        return $"{Guid.NewGuid()}-{nomeSeguro}";
    }

    private static string NormalizarNomeArquivo(string nome)
    {
        var normalizado = nome.Normalize(NormalizationForm.FormD);
        var semAcentos = new StringBuilder();
        foreach (var caractere in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
            {
                semAcentos.Append(caractere);
            }
        }

        var seguro = CaracteresInvalidos().Replace(semAcentos.ToString().ToLowerInvariant(), "-");
        seguro = HifensRepetidos().Replace(seguro, "-");
        seguro = BordasInvalidas().Replace(seguro, "");
        return seguro.Length == 0 ? NomePadrao : seguro;
    }

    [GeneratedRegex("[^a-z0-9._-]")]
    private static partial Regex CaracteresInvalidos();

    [GeneratedRegex("-+")]
    private static partial Regex HifensRepetidos();

    [GeneratedRegex(@"(^[-.]+)|([- .]+$)")]
    private static partial Regex BordasInvalidas();
}
