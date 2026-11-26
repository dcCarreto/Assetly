using System.IO.Compression;
using Assetly.Aplicacao.Excecao;
using Assetly.Aplicacao.Servico;
using Microsoft.Data.Sqlite;

namespace Assetly.Infraestrutura.Armazenamento;

/// <summary>Copia o banco SQLite e a pasta de documentos do ambiente atual para um instantâneo com
/// carimbo de data/hora dentro do diretório de destino escolhido pelo usuário — e faz o caminho
/// inverso na restauração.
/// <para>Arquivos intermediários (instantâneo do banco, backup extraído) ficam numa pasta de preparação ao
/// lado da pasta de documentos — dentro da pasta de dados do Assetly e no mesmo disco —, nunca na pasta
/// temporária do Windows: nada de cópia dos dados pessoais em claro fora da pasta de dados, e os documentos
/// restaurados são movidos (não copiados de novo) para o lugar.</para></summary>
public sealed class ServicoBackupLocal : IServicoBackup
{
    internal const string PrefixoInstantaneo = "Assetly-Backup-";
    private const string PastaBanco = "banco";
    private const string PastaDocumentos = "documentos";
    private const string PrefixoPreparacao = ".assetly-preparacao-";

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
        ArgumentNullException.ThrowIfNull(arquivoBanco);
        ArgumentNullException.ThrowIfNull(diretorioDocumentos);

        var preparacao = CriarPastaPreparacao(diretorioDocumentos);
        try
        {
            string pastaBackup;
            bool documentosDescartaveis;
            if (File.Exists(caminhoInstantaneo))
            {
                pastaBackup = ExtrairArquivo(caminhoInstantaneo, preparacao, senha);
                documentosDescartaveis = true;
            }
            else
            {
                pastaBackup = caminhoInstantaneo;
                documentosDescartaveis = false;
            }

            var bancoDoBackup = LocalizarBancoDoBackup(pastaBackup)
                ?? throw new InvalidOperationException("a pasta escolhida não contém um backup do Assetly (esperado: uma pasta \"banco\" com o arquivo .sqlite3).");
            ValidarBancoAssetly(bancoDoBackup);

            // Documentos de uma pasta do usuário são copiados primeiro para a preparação: uma falha nessa cópia
            // (pendrive removido, disco cheio) acontece antes de qualquer alteração nos dados atuais.
            var documentosDoBackup = Path.Combine(pastaBackup, PastaDocumentos);
            if (!documentosDescartaveis && Directory.Exists(documentosDoBackup))
            {
                var copia = Path.Combine(preparacao, PastaDocumentos);
                CopiarDiretorio(documentosDoBackup, copia);
                documentosDoBackup = copia;
            }

            Substituir(bancoDoBackup, documentosDoBackup, arquivoBanco, diretorioDocumentos);
        }
        catch (Exception excecao) when (excecao is IOException or SqliteException)
        {
            throw new InvalidOperationException($"não foi possível restaurar o backup: {excecao.Message}", excecao);
        }
        catch (UnauthorizedAccessException excecao)
        {
            throw new InvalidOperationException("sem permissão para ler o backup ou gravar na pasta de dados do Assetly.", excecao);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            ApagarPastaSilenciosamente(preparacao);
        }
    }

    public ResultadoTesteBackup TestarBackup(string caminhoBackup, string diretorioDocumentos, string? senha)
    {
        ArgumentNullException.ThrowIfNull(caminhoBackup);
        var preparacao = CriarPastaPreparacao(diretorioDocumentos);
        try
        {
            var pastaBackup = File.Exists(caminhoBackup) ? ExtrairArquivo(caminhoBackup, preparacao, senha) : caminhoBackup;
            var banco = LocalizarBancoDoBackup(pastaBackup)
                ?? throw new InvalidOperationException("não é um backup do Assetly (esperado: uma pasta \"banco\" com o arquivo .sqlite3).");
            ValidarBancoAssetly(banco);

            using var conexao = new SqliteConnection(TextoConexao(banco, SqliteOpenMode.ReadOnly));
            conexao.Open();
            using var comando = conexao.CreateCommand();
            comando.CommandText = "PRAGMA integrity_check";
            var integridade = comando.ExecuteScalar() as string;
            if (!string.Equals(integridade, "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"o banco de dados do backup está danificado ({integridade}).");
            }

            comando.CommandText = "SELECT COALESCE(MAX(versao), 0) FROM schema_migrations";
            var versao = (int)(long)comando.ExecuteScalar()!;
            comando.CommandText = "SELECT COUNT(*) FROM bens";
            var bens = (int)(long)comando.ExecuteScalar()!;
            comando.CommandText = "SELECT caminho_local FROM documentos";
            var documentosDoBackup = Path.Combine(pastaBackup, PastaDocumentos);
            var documentos = 0;
            var semArquivo = 0;
            using (var leitor = comando.ExecuteReader())
            {
                while (leitor.Read())
                {
                    documentos++;
                    var nome = Path.GetFileName(leitor.GetString(0).Replace('/', Path.DirectorySeparatorChar));
                    if (string.IsNullOrEmpty(nome) || !File.Exists(Path.Combine(documentosDoBackup, nome)))
                    {
                        semArquivo++;
                    }
                }
            }

            return new ResultadoTesteBackup(versao, bens, documentos, semArquivo);
        }
        catch (Exception excecao) when (excecao is IOException or SqliteException)
        {
            throw new InvalidOperationException($"não foi possível ler o backup: {excecao.Message}", excecao);
        }
        catch (UnauthorizedAccessException excecao)
        {
            throw new InvalidOperationException("sem permissão para ler o backup.", excecao);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            ApagarPastaSilenciosamente(preparacao);
        }
    }

    public void ManterUltimos(string diretorio, int quantidade)
    {
        if (!Directory.Exists(diretorio))
        {
            return;
        }

        var antigos = Directory.EnumerateFileSystemEntries(diretorio, PrefixoInstantaneo + "*")
            .OrderByDescending(Path.GetFileName, StringComparer.Ordinal)
            .Skip(Math.Max(quantidade, 1));
        foreach (var antigo in antigos)
        {
            try
            {
                if (Directory.Exists(antigo))
                {
                    Directory.Delete(antigo, recursive: true);
                }
                else
                {
                    File.Delete(antigo);
                }
            }
            catch (Exception excecao) when (excecao is IOException or UnauthorizedAccessException)
            {
                // Cópia em uso: tenta de novo na próxima vez.
            }
        }
    }

    /// <summary>Gera o arquivo único (.assetly) com banco e documentos. O ZIP é escrito direto no fluxo
    /// criptografado (sem cópia intermediária dos documentos nem ZIP temporário). Grava primeiro num arquivo
    /// ".parcial" ao lado e só então substitui o destino — quem lê o arquivo (outro computador sincronizando)
    /// nunca vê um arquivo pela metade.</summary>
    public void GerarArquivo(string arquivoBanco, string diretorioDocumentos, string arquivoDestino, string? senha)
    {
        ArgumentNullException.ThrowIfNull(arquivoDestino);
        var preparacao = CriarPastaPreparacao(diretorioDocumentos);
        var temporario = arquivoDestino + ".parcial";
        try
        {
            string? instantaneoBanco = null;
            if (File.Exists(arquivoBanco))
            {
                instantaneoBanco = Path.Combine(preparacao, Path.GetFileName(arquivoBanco));
                CopiarBanco(arquivoBanco, instantaneoBanco);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(arquivoDestino))!);
            using (var saida = File.Create(temporario))
            using (var protegido = ArquivoProtegido.AbrirEscrita(saida, senha))
            using (var zip = new ZipArchive(protegido, ZipArchiveMode.Create))
            {
                if (instantaneoBanco is not null)
                {
                    zip.CreateEntryFromFile(instantaneoBanco, $"{PastaBanco}/{Path.GetFileName(arquivoBanco)}", CompressionLevel.Optimal);
                }

                if (Directory.Exists(diretorioDocumentos))
                {
                    foreach (var arquivo in Directory.EnumerateFiles(diretorioDocumentos, "*", SearchOption.AllDirectories))
                    {
                        var nome = Path.GetRelativePath(diretorioDocumentos, arquivo).Replace(Path.DirectorySeparatorChar, '/');
                        zip.CreateEntryFromFile(arquivo, $"{PastaDocumentos}/{nome}", NivelCompressao(arquivo));
                    }
                }
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
            ApagarPastaSilenciosamente(preparacao);
        }
    }

    /// <summary>Fotos, PDFs e documentos do Office já vêm compactados: comprimir de novo gasta CPU (a maior parte do
    /// tempo de gerar o arquivo, no autoteste de volume) e não diminui nada. Esses entram guardados como estão.</summary>
    private static readonly HashSet<string> JaCompactados = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".webp", ".heic", ".heif", ".mp4", ".mov", ".zip", ".7z", ".rar",
        ".docx", ".xlsx", ".pptx", ".odt", ".ods"
    };

    private static CompressionLevel NivelCompressao(string arquivo) =>
        JaCompactados.Contains(Path.GetExtension(arquivo)) ? CompressionLevel.NoCompression : CompressionLevel.Optimal;

    /// <summary>Banco primeiro (a API de backup do SQLite troca o conteúdo numa transação: ou tudo, ou nada),
    /// depois os documentos já preparados no mesmo disco, movidos para o lugar. Falhar aqui deixa os dados pela
    /// metade — por isso a exceção própria, para quem chamou devolver o estado anterior.</summary>
    private static void Substituir(string bancoDoBackup, string documentosPreparados, string arquivoBanco, string diretorioDocumentos)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(arquivoBanco))!);
            CopiarBanco(bancoDoBackup, arquivoBanco);
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(documentosPreparados))
            {
                MoverConteudo(documentosPreparados, diretorioDocumentos);
            }
        }
        catch (Exception excecao) when (excecao is IOException or SqliteException or UnauthorizedAccessException)
        {
            throw new RestauracaoIncompletaExcecao($"a restauração foi interrompida no meio: {excecao.Message}", excecao);
        }
    }

    /// <summary>Decifra o .assetly para um ZIP na preparação e extrai ali mesmo (apagando o ZIP logo depois).</summary>
    private static string ExtrairArquivo(string arquivo, string preparacao, string? senha)
    {
        var zip = Path.Combine(preparacao, "dados.zip");
        using (var entrada = File.OpenRead(arquivo))
        using (var saida = File.Create(zip))
        {
            ArquivoProtegido.Ler(entrada, saida, senha);
        }

        var extraido = Path.Combine(preparacao, "instantaneo");
        try
        {
            ZipFile.ExtractToDirectory(zip, extraido);
        }
        catch (InvalidDataException excecao)
        {
            throw new InvalidOperationException("o arquivo de backup está corrompido.", excecao);
        }
        finally
        {
            ApagarSilenciosamente(zip);
        }

        return extraido;
    }

    /// <summary>Pasta de trabalho ao lado da pasta de documentos (mesmo disco, dentro da pasta de dados). Sobras
    /// de uma execução interrompida (queda de energia) são apagadas aqui.</summary>
    private static string CriarPastaPreparacao(string diretorioDocumentos)
    {
        var pai = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(diretorioDocumentos))) ?? Path.GetTempPath();
        if (Directory.Exists(pai))
        {
            foreach (var sobra in Directory.EnumerateDirectories(pai, PrefixoPreparacao + "*")
                         .Where(pasta => Directory.GetLastWriteTimeUtc(pasta) < DateTime.UtcNow.AddHours(-1)))
            {
                ApagarPastaSilenciosamente(sobra);
            }
        }

        var preparacao = Path.Combine(pai, $"{PrefixoPreparacao}{Guid.NewGuid():N}");
        Directory.CreateDirectory(preparacao);
        return preparacao;
    }

    private static void MoverConteudo(string origem, string destino)
    {
        Directory.CreateDirectory(destino);
        foreach (var arquivo in Directory.EnumerateFiles(origem))
        {
            File.Move(arquivo, Path.Combine(destino, Path.GetFileName(arquivo)), overwrite: true);
        }

        foreach (var subdiretorio in Directory.EnumerateDirectories(origem))
        {
            MoverConteudo(subdiretorio, Path.Combine(destino, Path.GetFileName(subdiretorio)));
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

    private static void ApagarPastaSilenciosamente(string pasta)
    {
        try
        {
            if (Directory.Exists(pasta))
            {
                Directory.Delete(pasta, recursive: true);
            }
        }
        catch (Exception excecao) when (excecao is IOException or UnauthorizedAccessException)
        {
            // Sobra é apagada na próxima preparação.
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
