using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Assetly.Infraestrutura.Seguranca;

/// <summary>
/// PIN (ou senha) pedido ao abrir o Assetly. Fica num arquivo deste computador, fora do banco — restaurar um backup ou
/// receber a sincronização não troca nem apaga o PIN. Só o resumo PBKDF2 (SHA-256, com sal) é guardado.
/// <para>Protege a abertura do programa, não os arquivos: o banco e os documentos continuam legíveis no perfil do
/// usuário. Esqueceu o PIN: apagar o arquivo remove o bloqueio.</para>
/// </summary>
public sealed class BloqueioPin(string arquivo)
{
    public const int TamanhoMinimo = 4;
    private const int Iteracoes = 210_000;

    public string Arquivo { get; } = arquivo ?? throw new ArgumentNullException(nameof(arquivo));

    public bool Ativo => File.Exists(Arquivo);

    public void Definir(string? pin, string? confirmacao)
    {
        if (string.IsNullOrEmpty(pin) || pin.Length < TamanhoMinimo)
        {
            throw new ArgumentException($"o PIN precisa ter pelo menos {TamanhoMinimo} caracteres.");
        }

        if (pin != confirmacao)
        {
            throw new ArgumentException("a confirmação não é igual ao PIN.");
        }

        var sal = RandomNumberGenerator.GetBytes(16);
        var registro = new Registro(1, Convert.ToBase64String(sal), Iteracoes, Convert.ToBase64String(Resumo(pin, sal, Iteracoes)));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Arquivo))!);
        var temporario = Arquivo + ".novo";
        File.WriteAllText(temporario, JsonSerializer.Serialize(registro));
        File.Move(temporario, Arquivo, overwrite: true);
    }

    /// <summary>Confere o PIN. Arquivo ilegível nunca desbloqueia.</summary>
    public bool Conferir(string? pin)
    {
        if (string.IsNullOrEmpty(pin) || !Ativo)
        {
            return !Ativo;
        }

        try
        {
            var registro = JsonSerializer.Deserialize<Registro>(File.ReadAllText(Arquivo));
            if (registro is null || registro.Iteracoes is < 10_000 or > 10_000_000)
            {
                return false;
            }

            var esperado = Convert.FromBase64String(registro.Resumo);
            return CryptographicOperations.FixedTimeEquals(esperado, Resumo(pin, Convert.FromBase64String(registro.Sal), registro.Iteracoes));
        }
        catch (Exception excecao) when (excecao is JsonException or FormatException or IOException)
        {
            return false;
        }
    }

    public void Remover()
    {
        if (File.Exists(Arquivo))
        {
            File.Delete(Arquivo);
        }
    }

    private static byte[] Resumo(string pin, byte[] sal, int iteracoes) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(pin), sal, iteracoes, HashAlgorithmName.SHA256, 32);

    private sealed record Registro(int Versao, string Sal, int Iteracoes, string Resumo);
}
