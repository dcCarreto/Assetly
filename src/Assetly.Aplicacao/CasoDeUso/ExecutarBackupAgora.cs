using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class ExecutarBackupAgora(
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

    public string Executar()
    {
        var configuracao = _repositorioConfiguracaoBackup.Obter();
        if (string.IsNullOrWhiteSpace(configuracao.CaminhoDestino))
        {
            throw new InvalidOperationException("defina o destino do backup antes de executar.");
        }

        var caminhoGerado = _servicoBackup.ExecutarBackup(_arquivoBanco, _diretorioDocumentos, configuracao.CaminhoDestino, SenhaBackup.Obter(configuracao, _protecaoSenha));
        configuracao.RegistrarBackupConcluido(DateTimeOffset.Now);
        _repositorioConfiguracaoBackup.Salvar(configuracao);
        return caminhoGerado;
    }
}
