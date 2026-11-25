using Assetly.Aplicacao.Servico;

namespace Assetly.Aplicacao.CasoDeUso;

/// <summary>Confere se um backup abre e está completo, sem restaurá-lo — backup nunca testado não é backup.</summary>
public sealed class TestarBackup(IServicoBackup servicoBackup, string diretorioDocumentos)
{
    private readonly IServicoBackup _servicoBackup = servicoBackup ?? throw new ArgumentNullException(nameof(servicoBackup));
    private readonly string _diretorioDocumentos = diretorioDocumentos ?? throw new ArgumentNullException(nameof(diretorioDocumentos));

    public ResultadoTesteBackup Executar(string caminhoBackup, string? senha = null)
    {
        if (string.IsNullOrWhiteSpace(caminhoBackup))
        {
            throw new ArgumentException("selecione o backup a testar.");
        }

        return _servicoBackup.TestarBackup(caminhoBackup, _diretorioDocumentos, senha);
    }
}
