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
