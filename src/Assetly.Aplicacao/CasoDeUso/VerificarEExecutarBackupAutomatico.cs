using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;

namespace Assetly.Aplicacao.CasoDeUso;

/// <summary>Chamado a cada atualização da tela principal (mesmo padrão de GerarAlertasAutomaticos):
/// só executa um backup de fato quando a configuração indica que já é devido.</summary>
public sealed class VerificarEExecutarBackupAutomatico(
    IRepositorioConfiguracaoBackup repositorioConfiguracaoBackup,
    IServicoBackup servicoBackup,
    string arquivoBanco,
    string diretorioDocumentos,
    IProtecaoSenha? protecaoSenha = null)
{
    private readonly IProtecaoSenha? _protecaoSenha = protecaoSenha;
    private readonly IRepositorioConfiguracaoBackup _repositorioConfiguracaoBackup = repositorioConfiguracaoBackup ?? throw new ArgumentNullException(nameof(repositorioConfiguracaoBackup), "repositório de configuração de backup é obrigatório");
    private readonly IServicoBackup _servicoBackup = servicoBackup ?? throw new ArgumentNullException(nameof(servicoBackup), "serviço de backup é obrigatório");
    private readonly string _arquivoBanco = arquivoBanco ?? throw new ArgumentNullException(nameof(arquivoBanco), "caminho do banco é obrigatório");
    private readonly string _diretorioDocumentos = diretorioDocumentos ?? throw new ArgumentNullException(nameof(diretorioDocumentos), "caminho dos documentos é obrigatório");

    public bool Executar(DateTimeOffset agora)
    {
        var configuracao = _repositorioConfiguracaoBackup.Obter();
        if (!configuracao.DeveExecutarAutoBackup(agora))
        {
            return false;
        }

        _servicoBackup.ExecutarBackup(_arquivoBanco, _diretorioDocumentos, configuracao.CaminhoDestino!, SenhaBackup.Obter(configuracao, _protecaoSenha));
        configuracao.RegistrarBackupConcluido(agora);
        _repositorioConfiguracaoBackup.Salvar(configuracao);
        ExecutarBackupAgora.AplicarRetencao(_servicoBackup, configuracao);
        return true;
    }
}
