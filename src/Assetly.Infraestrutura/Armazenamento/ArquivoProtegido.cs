using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Assetly.Infraestrutura.Armazenamento;

/// <summary>
/// Formato de arquivo único dos backups/sincronização (<c>.assetly</c>): um ZIP com banco e documentos,
/// opcionalmente criptografado.
/// <list type="bullet">
/// <item>Sem senha: cabeçalho <c>ASSETLY-ZIP-1</c> + ZIP.</item>
/// <item>Com senha: cabeçalho <c>ASSETLY-AES-1</c> + sal + iterações + blocos de até 1 MiB cifrados com
/// AES-256-GCM. A chave vem da senha por PBKDF2-SHA256; cada bloco leva seu índice e a marca de "último
/// bloco" como dado autenticado, então trocar a ordem, cortar ou alterar qualquer byte é detectado.</item>
/// </list>
/// </summary>
public static class ArquivoProtegido
{
    public const string Extensao = ".assetly";
    private const int IteracoesPadrao = 600_000;
    private const int TamanhoBloco = 1024 * 1024;
    private const int TamanhoSal = 16;
    private const int TamanhoNonce = 12;
    private const int TamanhoTag = 16;
    private static readonly byte[] CabecalhoZip = Encoding.ASCII.GetBytes("ASSETLY-ZIP-1\n");
    private static readonly byte[] CabecalhoAes = Encoding.ASCII.GetBytes("ASSETLY-AES-1\n");

    public static bool EhProtegidoPorSenha(string arquivo)
    {
        using var entrada = File.OpenRead(arquivo);
        return LerCabecalho(entrada) == true;
    }

    /// <summary>Grava <paramref name="zip"/> no formato .assetly (cifrado se houver senha).</summary>
    public static void Escrever(Stream zip, Stream saida, string? senha)
    {
        if (string.IsNullOrEmpty(senha))
        {
            saida.Write(CabecalhoZip);
            zip.CopyTo(saida);
            return;
        }

        var sal = RandomNumberGenerator.GetBytes(TamanhoSal);
        var chave = DerivarChave(senha, sal, IteracoesPadrao);
        saida.Write(CabecalhoAes);
        saida.Write(sal);
        EscreverInteiro(saida, IteracoesPadrao);

        using var aes = new AesGcm(chave, TamanhoTag);
        var atual = new byte[TamanhoBloco];
        var proximo = new byte[TamanhoBloco];
        var lidosAtual = LerBloco(zip, atual);
        long indice = 0;
        while (true)
        {
            var lidosProximo = lidosAtual == TamanhoBloco ? LerBloco(zip, proximo) : 0;
            var ultimo = lidosProximo == 0;
            var nonce = RandomNumberGenerator.GetBytes(TamanhoNonce);
            var cifrado = new byte[lidosAtual];
            var tag = new byte[TamanhoTag];
            aes.Encrypt(nonce, atual.AsSpan(0, lidosAtual), cifrado, tag, DadosAssociados(indice, ultimo));
            saida.Write(nonce);
            EscreverInteiro(saida, lidosAtual);
            saida.Write(cifrado);
            saida.Write(tag);
            if (ultimo)
            {
                break;
            }

            (atual, proximo) = (proximo, atual);
            lidosAtual = lidosProximo;
            indice++;
        }

        CryptographicOperations.ZeroMemory(chave);
    }

    /// <summary>Lê um arquivo .assetly e grava o ZIP original em <paramref name="zip"/>.</summary>
    public static void Ler(Stream entrada, Stream zip, string? senha)
    {
        var protegido = LerCabecalho(entrada)
            ?? throw new InvalidOperationException("o arquivo não é um backup do Assetly (formato desconhecido).");
        if (!protegido)
        {
            entrada.CopyTo(zip);
            return;
        }

        if (string.IsNullOrEmpty(senha))
        {
            throw new InvalidOperationException("este backup é protegido por senha. Informe a senha para restaurá-lo.");
        }

        var sal = LerExato(entrada, TamanhoSal);
        var iteracoes = LerInteiro(entrada);
        if (iteracoes is < 100_000 or > 10_000_000)
        {
            throw new InvalidOperationException("o arquivo de backup está corrompido.");
        }

        var chave = DerivarChave(senha, sal, iteracoes);
        try
        {
            using var aes = new AesGcm(chave, TamanhoTag);
            long indice = 0;
            var terminou = false;
            while (!terminou)
            {
                var nonce = LerExato(entrada, TamanhoNonce);
                var tamanho = LerInteiro(entrada);
                if (tamanho is < 0 or > TamanhoBloco)
                {
                    throw new InvalidOperationException("o arquivo de backup está corrompido.");
                }

                var cifrado = LerExato(entrada, tamanho);
                var tag = LerExato(entrada, TamanhoTag);
                var claro = new byte[tamanho];
                // O último bloco é o que não tem nada depois; a marca autenticada confirma que não houve corte.
                terminou = entrada.Position >= entrada.Length;
                try
                {
                    aes.Decrypt(nonce, cifrado, tag, claro, DadosAssociados(indice, terminou));
                }
                catch (AuthenticationTagMismatchException excecao)
                {
                    throw new InvalidOperationException(
                        indice == 0 ? "senha incorreta (ou arquivo de backup alterado)." : "o arquivo de backup está incompleto ou foi alterado.", excecao);
                }

                zip.Write(claro);
                indice++;
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(chave);
        }
    }

    private static bool? LerCabecalho(Stream entrada)
    {
        var cabecalho = new byte[CabecalhoZip.Length];
        var lidos = entrada.Read(cabecalho);
        if (lidos == cabecalho.Length && cabecalho.SequenceEqual(CabecalhoAes))
        {
            return true;
        }

        return lidos == cabecalho.Length && cabecalho.SequenceEqual(CabecalhoZip) ? false : null;
    }

    private static byte[] DerivarChave(string senha, byte[] sal, int iteracoes) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(senha), sal, iteracoes, HashAlgorithmName.SHA256, 32);

    private static byte[] DadosAssociados(long indice, bool ultimo)
    {
        var dados = new byte[9];
        BinaryPrimitives.WriteInt64LittleEndian(dados, indice);
        dados[8] = ultimo ? (byte)1 : (byte)0;
        return dados;
    }

    private static int LerBloco(Stream entrada, byte[] destino)
    {
        var total = 0;
        while (total < destino.Length)
        {
            var lidos = entrada.Read(destino, total, destino.Length - total);
            if (lidos == 0)
            {
                break;
            }

            total += lidos;
        }

        return total;
    }

    private static byte[] LerExato(Stream entrada, int tamanho)
    {
        var dados = new byte[tamanho];
        if (LerBloco(entrada, dados) != tamanho)
        {
            throw new InvalidOperationException("o arquivo de backup está incompleto ou corrompido.");
        }

        return dados;
    }

    private static void EscreverInteiro(Stream saida, int valor)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, valor);
        saida.Write(bytes);
    }

    private static int LerInteiro(Stream entrada) => BinaryPrimitives.ReadInt32LittleEndian(LerExato(entrada, 4));
}
