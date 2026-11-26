using System.IO;
using System.Text.Json;

namespace Assetly.Apresentacao.Visao;

/// <summary>
/// Colunas que o usuário escondeu e larguras que ajustou, por tabela. Ficam num arquivo deste computador (fora do banco):
/// é preferência da tela de cada máquina — restaurar um backup ou receber a sincronização não mexe nelas.
/// </summary>
internal static class PreferenciasTabelas
{
    internal sealed record Coluna(bool Visivel = true, double? Largura = null);

    private static string? _arquivo;
    private static Dictionary<string, Dictionary<string, Coluna>>? _dados;

    /// <summary>Definido pela tela principal (um arquivo por ambiente). Sem arquivo, nada é lembrado.</summary>
    internal static string? Arquivo
    {
        get => _arquivo;
        set
        {
            _arquivo = value;
            _dados = null;
        }
    }

    internal static Coluna? Obter(string tabela, string coluna) =>
        Dados().TryGetValue(tabela, out var colunas) && colunas.TryGetValue(coluna, out var salva) ? salva : null;

    internal static void Definir(string tabela, string coluna, Func<Coluna, Coluna> alterar)
    {
        var dados = Dados();
        if (!dados.TryGetValue(tabela, out var colunas))
        {
            colunas = [];
            dados[tabela] = colunas;
        }

        colunas[coluna] = alterar(colunas.GetValueOrDefault(coluna) ?? new Coluna());
        Salvar();
    }

    internal static void Limpar(string tabela)
    {
        if (Dados().Remove(tabela))
        {
            Salvar();
        }
    }

    private static Dictionary<string, Dictionary<string, Coluna>> Dados()
    {
        if (_dados is not null)
        {
            return _dados;
        }

        try
        {
            _dados = _arquivo is not null && File.Exists(_arquivo)
                ? JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Coluna>>>(File.ReadAllText(_arquivo)) ?? []
                : [];
        }
        catch (Exception excecao) when (excecao is JsonException or IOException or UnauthorizedAccessException)
        {
            // Arquivo de preferência ilegível: começa do padrão em vez de impedir a tela de abrir.
            RegistroErros.Registrar("Preferências das tabelas ilegíveis", excecao);
            _dados = [];
        }

        return _dados;
    }

    private static void Salvar()
    {
        if (_arquivo is null || _dados is null)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_arquivo))!);
            File.WriteAllText(_arquivo, JsonSerializer.Serialize(_dados));
        }
        catch (Exception excecao) when (excecao is IOException or UnauthorizedAccessException)
        {
            RegistroErros.Registrar("Não foi possível guardar as preferências das tabelas", excecao);
        }
    }
}
