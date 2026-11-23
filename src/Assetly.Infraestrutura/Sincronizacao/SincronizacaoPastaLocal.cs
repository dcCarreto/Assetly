using System.Text.Json;
using Assetly.Aplicacao.Servico;

namespace Assetly.Infraestrutura.Sincronizacao;

/// <summary>Mantém na pasta sincronizada dois arquivos: a cópia completa (<c>Assetly-Sincronizacao.assetly</c>)
/// e os metadados (<c>Assetly-Sincronizacao.json</c>), gravados nessa ordem para que os metadados nunca
/// descrevam uma cópia que ainda não chegou.</summary>
public sealed class SincronizacaoPastaLocal(IServicoBackup servicoBackup) : ISincronizacaoPasta
{
    private const string NomeCopia = "Assetly-Sincronizacao.assetly";
    private const string NomeMetadados = "Assetly-Sincronizacao.json";
    private static readonly JsonSerializerOptions Opcoes = new() { WriteIndented = true };

    private readonly IServicoBackup _servicoBackup = servicoBackup ?? throw new ArgumentNullException(nameof(servicoBackup));

    public MetadadosSincronizacao? LerMetadados(string pasta)
    {
        var arquivo = Path.Combine(pasta, NomeMetadados);
        if (!File.Exists(arquivo) || !File.Exists(CaminhoCopia(pasta)))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<MetadadosSincronizacao>(File.ReadAllText(arquivo), Opcoes);
        }
        catch (JsonException excecao)
        {
            throw new InvalidOperationException($"o arquivo de sincronização na pasta está corrompido: {arquivo}", excecao);
        }
    }

    public void Enviar(string pasta, string arquivoBanco, string diretorioDocumentos, string? senha, MetadadosSincronizacao metadados)
    {
        _servicoBackup.GerarArquivo(arquivoBanco, diretorioDocumentos, CaminhoCopia(pasta), senha);
        var temporario = Path.Combine(pasta, NomeMetadados + ".parcial");
        File.WriteAllText(temporario, JsonSerializer.Serialize(metadados, Opcoes));
        File.Move(temporario, Path.Combine(pasta, NomeMetadados), overwrite: true);
    }

    public string CaminhoCopia(string pasta) => Path.Combine(pasta, NomeCopia);
}
