using System.IO.Compression;
using Assetly.Aplicacao.Servico;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Armazenamento;

/// <summary>Copia o banco SQLite e a pasta de documentos do ambiente atual para um instantâneo com
/// carimbo de data/hora dentro do diretório de destino escolhido pelo usuário — e faz o caminho
/// inverso na restauração.</summary>
public sealed class ServicoBackupLocal : IServicoBackup
{
    internal const string PrefixoInstantaneo = "Assetly-Backup-";
    private const string PastaBanco = "banco";
    private const string PastaDocumentos = "documentos";

    public string ExecutarBackup(string arquivoBanco, string diretorioDocumentos, string diretorioDestino, string? senha = null)
    {
        ArgumentNullException.ThrowIfNull(arquivoBanco);
        ArgumentNullException.ThrowIfNull(diretorioDocumentos);
        ArgumentNullException.ThrowIfNull(diretorioDestino);

        if (DestinoConflitaComDados(diretorioDestino, arquivoBanco, diretorioDocumentos))
        {
            throw new InvalidOperationException("o destino do backup não pode ficar dentro da pasta de dados do Assetly. Escolha outra pasta (de preferência em outro disco ou pendrive).");
        }

        if (!string.IsNullOrEmpty(senha))
        {
            var arquivo = ArquivoLivre(diretorioDestino);
            Directory.CreateDirectory(diretorioDestino);
            GerarArquivo(arquivoBanco, diretorioDocumentos, arquivo, senha);
            return arquivo;
        }

        var caminhoInstantaneo = CaminhoInstantaneoLivre(diretorioDestino);

        try
        {
            Directory.CreateDirectory(caminhoInstantaneo);

            if (File.Exists(arquivoBanco))
            {
                var destinoBanco = Path.Combine(caminhoInstantaneo, PastaBanco);
                Directory.CreateDirectory(destinoBanco);
                CopiarBanco(arquivoBanco, Path.Combine(destinoBanco, Path.GetFileName(arquivoBanco)));
            }

            if (Directory.Exists(diretorioDocumentos))
            {
                CopiarDiretorio(diretorioDocumentos, Path.Combine(caminhoInstantaneo, PastaDocumentos));
            }

            return caminhoInstantaneo;
        }
        catch (Exception excecao) when (excecao is IOException or SqliteException)
        {
            throw new InvalidOperationException($"não foi possível concluir o backup no destino escolhido: {excecao.Message}", excecao);
        }
        catch (UnauthorizedAccessException excecao)
        {
            throw new InvalidOperationException("sem permissão para gravar no destino escolhido.", excecao);
        }
    }

    public void RestaurarBackup(string caminhoInstantaneo, string arquivoBanco, string diretorioDocumentos, string? senha = null)
    {
        ArgumentNullException.ThrowIfNull(caminhoInstantaneo);
        if (File.Exists(caminhoInstantaneo))
        {
            RestaurarArquivo(caminhoInstantaneo, arquivoBanco, diretorioDocumentos, senha);
            return;
        }

        ArgumentNullException.ThrowIfNull(arquivoBanco);
        ArgumentNullException.ThrowIfNull(diretorioDocumentos);

        var bancoDoBackup = LocalizarBancoDoBackup(caminhoInstantaneo)
            ?? throw new InvalidOperationException("a pasta escolhida não contém um backup do Assetly (esperado: uma pasta \"banco\" com o arquivo .sqlite3).");
        ValidarBancoAssetly(bancoDoBackup);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(arquivoBanco))!);
            CopiarBanco(bancoDoBackup, arquivoBanco);
            SqliteConnection.ClearAllPools();

            var documentosDoBackup = Path.Combine(caminhoInstantaneo, PastaDocumentos);
            if (Directory.Exists(documentosDoBackup))
            {
                CopiarDiretorio(documentosDoBackup, diretorioDocumentos);
            }
        }
        catch (Exception excecao) when (excecao is IOException or SqliteException)
        {
            throw new InvalidOperationException($"não foi possível restaurar o backup: {excecao.Message}", excecao);
        }
        catch (UnauthorizedAccessException excecao)
        {
            throw new InvalidOperationException("sem permissão para gravar na pasta de dados do Assetly.", excecao);
        }
    }

    /// <summary>Gera o arquivo único (.assetly) com banco e documentos. Grava primeiro num arquivo temporário
    /// ao lado e só então substitui o destino — quem lê o arquivo (outro computador sincronizando) nunca vê
    /// um arquivo pela metade.</summary>
    public void GerarArquivo(string arquivoBanco, string diretorioDocumentos, string arquivoDestino, string? senha)
    {
        ArgumentNullException.ThrowIfNull(arquivoDestino);
        var temporaria = Path.Combine(Path.GetTempPath(), $"assetly-{Guid.NewGuid():N}");
        var temporario = arquivoDestino + ".parcial";
        try
        {
            var instantaneo = Path.Combine(temporaria, "instantaneo");
            CriarInstantaneoEm(instantaneo, arquivoBanco, diretorioDocumentos);
            var zip = Path.Combine(temporaria, "dados.zip");
            ZipFile.CreateFromDirectory(instantaneo, zip, CompressionLevel.Optimal, includeBaseDirectory: false);

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(arquivoDestino))!);
            using (var entrada = File.OpenRead(zip))
            using (var saida = File.Create(temporario))
            {
                ArquivoProtegido.Escrever(entrada, saida, senha);
            }

            File.Move(temporario, arquivoDestino, overwrite: true);
        }
        catch (Exception excecao) when (excecao is IOException or SqliteException)
        {
            throw new InvalidOperationException($"não foi possível gerar o arquivo de backup: {excecao.Message}", excecao);
        }
        catch (UnauthorizedAccessException excecao)
        {
            throw new InvalidOperationException("sem permissão para gravar no destino escolhido.", excecao);
        }
        finally
        {
            ApagarSilenciosamente(temporario);
            if (Directory.Exists(temporaria))
            {
                Directory.Delete(temporaria, recursive: true);
            }
        }
    }

    private void RestaurarArquivo(string arquivo, string arquivoBanco, string diretorioDocumentos, string? senha)
    {
        var temporaria = Path.Combine(Path.GetTempPath(), $"assetly-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(temporaria);
            var zip = Path.Combine(temporaria, "dados.zip");
            using (var entrada = File.OpenRead(arquivo))
            using (var saida = File.Create(zip))
            {
                ArquivoProtegido.Ler(entrada, saida, senha);
            }

            var extraido = Path.Combine(temporaria, "instantaneo");
            try
            {
                ZipFile.ExtractToDirectory(zip, extraido);
            }
            catch (InvalidDataException excecao)
            {
                throw new InvalidOperationException("o arquivo de backup está corrompido.", excecao);
            }

            RestaurarBackup(extraido, arquivoBanco, diretorioDocumentos);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(temporaria))
            {
                Directory.Delete(temporaria, recursive: true);
            }
        }
    }

    private static void CriarInstantaneoEm(string pasta, string arquivoBanco, string diretorioDocumentos)
    {
        Directory.CreateDirectory(pasta);
        if (File.Exists(arquivoBanco))
        {
            var destinoBanco = Path.Combine(pasta, PastaBanco);
            Directory.CreateDirectory(destinoBanco);
            CopiarBanco(arquivoBanco, Path.Combine(destinoBanco, Path.GetFileName(arquivoBanco)));
        }

        if (Directory.Exists(diretorioDocumentos))
        {
            CopiarDiretorio(diretorioDocumentos, Path.Combine(pasta, PastaDocumentos));
        }
    }

    private static string ArquivoLivre(string diretorioDestino)
    {
        var baseNome = Path.Combine(diretorioDestino, $"{PrefixoInstantaneo}{DateTime.Now:yyyy-MM-dd_HHmmss}");
        var caminho = baseNome + ArquivoProtegido.Extensao;
        for (var sufixo = 2; File.Exists(caminho); sufixo++)
        {
            caminho = $"{baseNome}-{sufixo}{ArquivoProtegido.Extensao}";
        }

        return caminho;
    }

    private static void ApagarSilenciosamente(string arquivo)
    {
        try
        {
            if (File.Exists(arquivo))
            {
                File.Delete(arquivo);
            }
        }
        catch (Exception excecao) when (excecao is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>Verdadeiro quando o destino fica dentro da pasta de documentos ou da pasta do banco —
    /// copiar os documentos para dentro deles mesmos geraria uma cópia recursiva sem fim.</summary>
    public static bool DestinoConflitaComDados(string diretorioDestino, string arquivoBanco, string diretorioDocumentos)
    {
        var destino = Normalizar(diretorioDestino);
        var pastaBanco = Normalizar(Path.GetDirectoryName(Path.GetFullPath(arquivoBanco)) ?? arquivoBanco);
        var documentos = Normalizar(diretorioDocumentos);
        return EstaDentro(destino, documentos) || EstaDentro(destino, pastaBanco);
    }

    /// <summary>Usa a API de backup online do SQLite em vez de copiar o arquivo: gera uma cópia
    /// consistente mesmo com conexões abertas (o pool do Microsoft.Data.Sqlite mantém o arquivo aberto).</summary>
    private static void CopiarBanco(string arquivoOrigem, string arquivoDestino)
    {
        using var origem = new SqliteConnection(TextoConexao(arquivoOrigem, SqliteOpenMode.ReadOnly));
        using var destino = new SqliteConnection(TextoConexao(arquivoDestino, SqliteOpenMode.ReadWriteCreate));
        origem.Open();
        destino.Open();
        origem.BackupDatabase(destino);
    }

    private static string TextoConexao(string arquivo, SqliteOpenMode modo) =>
        new SqliteConnectionStringBuilder { DataSource = Path.GetFullPath(arquivo), Mode = modo, Pooling = false }.ToString();

    private static string? LocalizarBancoDoBackup(string caminhoInstantaneo)
    {
        var pastaBanco = Path.Combine(caminhoInstantaneo, PastaBanco);
        if (!Directory.Exists(pastaBanco))
        {
            return null;
        }

        return Directory.EnumerateFiles(pastaBanco, "*.sqlite3").OrderBy(caminho => caminho, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
    }

    private static void ValidarBancoAssetly(string arquivoBanco)
    {
        try
        {
            using var conexao = new SqliteConnection(TextoConexao(arquivoBanco, SqliteOpenMode.ReadOnly));
            conexao.Open();
            using var comando = conexao.CreateCommand();
            comando.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN ('bens', 'schema_migrations')";
            if ((long)comando.ExecuteScalar()! < 2)
            {
                throw new InvalidOperationException("o arquivo de banco do backup não é um banco do Assetly.");
            }

            comando.CommandText = "SELECT COALESCE(MAX(versao), 0) FROM schema_migrations";
            if ((long)comando.ExecuteScalar()! > Banco.MigradorBancoDados.VersaoMaisRecente)
            {
                throw new InvalidOperationException("este backup foi feito por uma versão mais nova do Assetly. Atualize o programa antes de restaurá-lo (nada foi alterado).");
            }
        }
        catch (SqliteException excecao)
        {
            throw new InvalidOperationException("o arquivo de banco do backup está corrompido ou não é um banco SQLite.", excecao);
        }
    }

    private static string CaminhoInstantaneoLivre(string diretorioDestino)
    {
        var baseNome = Path.Combine(diretorioDestino, $"{PrefixoInstantaneo}{DateTime.Now:yyyy-MM-dd_HHmmss}");
        var caminho = baseNome;
        for (var sufixo = 2; Directory.Exists(caminho); sufixo++)
        {
            caminho = $"{baseNome}-{sufixo}";
        }

        return caminho;
    }

    private static void CopiarDiretorio(string origem, string destino)
    {
        Directory.CreateDirectory(destino);
        foreach (var arquivo in Directory.EnumerateFiles(origem))
        {
            File.Copy(arquivo, Path.Combine(destino, Path.GetFileName(arquivo)), overwrite: true);
        }

        foreach (var subdiretorio in Directory.EnumerateDirectories(origem))
        {
            CopiarDiretorio(subdiretorio, Path.Combine(destino, Path.GetFileName(subdiretorio)));
        }
    }

    private static string Normalizar(string caminho) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(caminho));

    private static bool EstaDentro(string caminho, string pasta) =>
        string.Equals(caminho, pasta, StringComparison.OrdinalIgnoreCase)
        || caminho.StartsWith(pasta + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
