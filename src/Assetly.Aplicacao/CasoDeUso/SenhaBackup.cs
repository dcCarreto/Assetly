using Assetly.Aplicacao.Servico;
using Assetly.Dominio.Backup;

namespace Assetly.Aplicacao.CasoDeUso;

public static class SenhaBackup
{
    public const int TamanhoMinimo = 8;

    /// <summary>Senha em uso para os backups (nula se a proteção está desligada).</summary>
    public static string? Obter(ConfiguracaoBackup configuracao, IProtecaoSenha? protecao)
    {
        if (!configuracao.Criptografado || configuracao.SenhaProtegida is null)
        {
            return null;
        }

        if (protecao is null)
        {
            throw new InvalidOperationException("a proteção de senha não está disponível neste computador.");
        }

        try
        {
            return protecao.Revelar(configuracao.SenhaProtegida);
        }
        catch (Exception excecao) when (excecao is not OutOfMemoryException)
        {
            throw new InvalidOperationException(
                "não foi possível ler a senha dos backups (ela só pode ser lida pelo mesmo usuário do Windows que a definiu). " +
                "Defina a senha de novo no módulo Backup.", excecao);
        }
    }
}
