namespace Assetly.Aplicacao.Servico;

public interface IArmazenamentoDocumento
{
    /// <summary>Copia o arquivo de origem para o armazenamento privado e devolve o caminho armazenado.</summary>
    string Armazenar(string arquivoOrigem);

    /// <summary>Remove a cópia privada de um documento excluído. Só apaga arquivos que estão dentro do
    /// armazenamento gerenciado — nunca um arquivo original do usuário.</summary>
    void Remover(string caminhoArmazenado)
    {
    }

    /// <summary>Devolve o caminho onde o arquivo do documento existe hoje: o próprio caminho registrado
    /// ou, se ele não existir mais, um arquivo de mesmo nome no armazenamento atual (caso de backup
    /// restaurado em outra pasta ou perfil). Nulo quando o arquivo não é encontrado.</summary>
    string? Localizar(string caminhoRegistrado) => caminhoRegistrado;

    /// <summary>Apaga do armazenamento privado os arquivos que nenhum documento usa (sobras de uma restauração
    /// que juntou os arquivos do backup aos de hoje). Devolve quantos foram apagados.</summary>
    int RemoverNaoReferenciados(IReadOnlyCollection<string> caminhosReferenciados) => 0;
}
