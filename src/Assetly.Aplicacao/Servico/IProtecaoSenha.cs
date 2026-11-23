namespace Assetly.Aplicacao.Servico;

/// <summary>Guarda segredos (a senha dos backups) de forma que só o usuário atual do Windows, neste
/// computador, consiga recuperá-los — nunca em texto puro no banco.</summary>
public interface IProtecaoSenha
{
    string Proteger(string segredo);

    string Revelar(string protegido);
}
