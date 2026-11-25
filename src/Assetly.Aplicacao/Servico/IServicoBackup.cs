namespace Assetly.Aplicacao.Servico;

/// <summary>O que foi conferido num backup sem restaurá-lo.</summary>
public sealed record ResultadoTesteBackup(int VersaoBanco, int Bens, int Documentos, int DocumentosSemArquivo)
{
    public bool Completo => DocumentosSemArquivo == 0;
}

public interface IServicoBackup
{
    /// <summary>Abre o backup (pasta ou arquivo <c>.assetly</c>, com a senha se protegido) numa pasta de trabalho ao
    /// lado de <paramref name="diretorioDocumentos"/>, confere a integridade do banco e se cada documento tem o seu
    /// arquivo — sem tocar nos dados atuais. Lança se o backup não puder ser lido.</summary>
    ResultadoTesteBackup TestarBackup(string caminhoBackup, string diretorioDocumentos, string? senha);

    /// <summary>Copia o banco e a pasta de documentos para dentro do diretório de destino e devolve o
    /// caminho gerado: uma pasta <c>Assetly-Backup-…</c> ou, com <paramref name="senha"/>, um único arquivo
    /// <c>Assetly-Backup-….assetly</c> criptografado (AES-256-GCM, chave derivada da senha).</summary>
    string ExecutarBackup(string arquivoBanco, string diretorioDocumentos, string diretorioDestino, string? senha = null);

    /// <summary>Substitui o banco atual pelo do backup e copia de volta os documentos do backup (sem apagar
    /// arquivos que só existem hoje). Aceita a pasta de um backup comum ou um arquivo <c>.assetly</c>
    /// (neste caso, protegido ou não por senha). Tudo é validado e preparado antes de tocar nos dados; se a
    /// troca falhar no meio, lança <see cref="Excecao.RestauracaoIncompletaExcecao"/>.</summary>
    void RestaurarBackup(string caminhoBackup, string arquivoBanco, string diretorioDocumentos, string? senha = null);

    /// <summary>Apaga os backups (<c>Assetly-Backup-…</c>) mais antigos do diretório, mantendo os
    /// <paramref name="quantidade"/> mais recentes. Outros arquivos do diretório não são tocados.</summary>
    void ManterUltimos(string diretorio, int quantidade)
    {
    }

    /// <summary>Gera um arquivo único com banco e documentos (criptografado se houver senha) em
    /// <paramref name="arquivoDestino"/>, substituindo-o de forma atômica — usado pela sincronização.</summary>
    void GerarArquivo(string arquivoBanco, string diretorioDocumentos, string arquivoDestino, string? senha);
}
