using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Assetly.Aplicacao.Servico;

namespace Assetly.Infraestrutura.Seguranca;

/// <summary>Protege segredos com a DPAPI do Windows (escopo do usuário atual): o texto guardado no banco
/// só pode ser revertido por este usuário neste computador — outra pessoa com uma cópia do banco não o lê.</summary>
[SupportedOSPlatform("windows")]
public sealed class ProtecaoSenhaWindows : IProtecaoSenha
{
    private static readonly byte[] Entropia = Encoding.UTF8.GetBytes("Assetly.SenhaBackup.v1");

    public string Proteger(string segredo) =>
        Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(segredo), Entropia, DataProtectionScope.CurrentUser));

    public string Revelar(string protegido) =>
        Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(protegido), Entropia, DataProtectionScope.CurrentUser));
}
