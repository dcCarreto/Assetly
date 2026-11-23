using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;
using Assetly.Dominio.Backup;

namespace Assetly.Aplicacao.CasoDeUso;

/// <summary>Liga a proteção por senha dos backups. A senha fica guardada protegida pelo Windows para os
/// backups automáticos; o usuário precisa dela para restaurar em outro computador.</summary>
public sealed class AtivarCriptografiaBackup(IRepositorioConfiguracaoBackup repositorio, IProtecaoSenha protecao)
{
    private readonly IRepositorioConfiguracaoBackup _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
    private readonly IProtecaoSenha _protecao = protecao ?? throw new ArgumentNullException(nameof(protecao));

    public ConfiguracaoBackup Executar(string? senha, string? confirmacao)
    {
        if (string.IsNullOrEmpty(senha) || senha.Length < SenhaBackup.TamanhoMinimo)
        {
            throw new ArgumentException($"a senha dos backups precisa ter pelo menos {SenhaBackup.TamanhoMinimo} caracteres.");
        }

        if (senha != confirmacao)
        {
            throw new ArgumentException("a confirmação não é igual à senha.");
        }

        var configuracao = _repositorio.Obter();
        configuracao.AtivarCriptografia(_protecao.Proteger(senha));
        _repositorio.Salvar(configuracao);
        return configuracao;
    }
}
