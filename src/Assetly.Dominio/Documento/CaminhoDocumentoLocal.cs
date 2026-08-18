namespace Assetly.Dominio.Documento;

public sealed record CaminhoDocumentoLocal
{
    public string Valor { get; }

    public CaminhoDocumentoLocal(string valor)
    {
        if (valor is null)
        {
            throw new ArgumentNullException(nameof(valor), "caminho do documento é obrigatório");
        }

        var normalizado = Normalizar(valor);
        if (string.IsNullOrWhiteSpace(normalizado))
        {
            throw new ArgumentException("caminho do documento não pode ficar em branco");
        }

        Valor = normalizado;
    }

    public static CaminhoDocumentoLocal De(string valor) => new(valor);

    private static string Normalizar(string valor)
    {
        var raiz = Path.GetPathRoot(valor) ?? string.Empty;
        var resto = valor[raiz.Length..];
        var segmentos = resto.Split(['/', '\\'], StringSplitOptions.None);
        var pilha = new List<string>();

        foreach (var segmento in segmentos)
        {
            if (segmento.Length == 0 || segmento == ".")
            {
                continue;
            }

            if (segmento == "..")
            {
                if (pilha.Count > 0 && pilha[^1] != "..")
                {
                    pilha.RemoveAt(pilha.Count - 1);
                }
                else if (raiz.Length == 0)
                {
                    pilha.Add("..");
                }

                continue;
            }

            pilha.Add(segmento);
        }

        var caminhoRelativo = string.Join(Path.DirectorySeparatorChar, pilha);
        return raiz.Length > 0 ? Path.Combine(raiz, caminhoRelativo) : caminhoRelativo;
    }
}
