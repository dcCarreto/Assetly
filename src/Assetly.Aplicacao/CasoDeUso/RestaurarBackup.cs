using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;

namespace Assetly.Aplicacao.CasoDeUso;

/// <summary>Resultado de uma restauração: onde ficou a cópia de segurança do estado anterior e quantos
/// documentos tiveram o caminho do arquivo corrigido para a pasta atual.</summary>
public sealed record ResultadoRestauracao(string CaminhoCopiaSeguranca, int DocumentosRelocalizados, int DocumentosSemArquivo);

/// <summary>
/// Restaura um instantâneo gerado pelo backup. Antes de qualquer alteração, o estado atual é salvo
/// em <c>diretorioSeguranca</c> — a restauração pode ser desfeita restaurando essa cópia. Depois:
/// banco substituído, migrações reaplicadas (o backup pode ser de uma versão anterior), configuração
/// de backup atual preservada (destino/agenda são do computador, não dos dados) e caminhos de
/// documentos corrigidos quando o backup veio de outra pasta ou perfil de usuário.
/// </summary>
public sealed class RestaurarBackup(
    IRepositorioConfiguracaoBackup repositorioConfiguracaoBackup,
    IRepositorioDocumento repositorioDocumento,
    IServicoBackup servicoBackup,
    IArmazenamentoDocumento armazenamentoDocumento,
    Action prepararBanco,
    string arquivoBanco,
    string diretorioDocumentos,
    string diretorioSeguranca,
    IRepositorioConfiguracaoSincronizacao? repositorioSincronizacao = null)
{
    private readonly IRepositorioConfiguracaoSincronizacao? _repositorioSincronizacao = repositorioSincronizacao;
    private readonly IRepositorioConfiguracaoBackup _repositorioConfiguracaoBackup = repositorioConfiguracaoBackup ?? throw new ArgumentNullException(nameof(repositorioConfiguracaoBackup));
    private readonly IRepositorioDocumento _repositorioDocumento = repositorioDocumento ?? throw new ArgumentNullException(nameof(repositorioDocumento));
    private readonly IServicoBackup _servicoBackup = servicoBackup ?? throw new ArgumentNullException(nameof(servicoBackup));
    private readonly IArmazenamentoDocumento _armazenamentoDocumento = armazenamentoDocumento ?? throw new ArgumentNullException(nameof(armazenamentoDocumento));
    private readonly Action _prepararBanco = prepararBanco ?? throw new ArgumentNullException(nameof(prepararBanco));
    private readonly string _arquivoBanco = arquivoBanco ?? throw new ArgumentNullException(nameof(arquivoBanco));
    private readonly string _diretorioDocumentos = diretorioDocumentos ?? throw new ArgumentNullException(nameof(diretorioDocumentos));
    private readonly string _diretorioSeguranca = diretorioSeguranca ?? throw new ArgumentNullException(nameof(diretorioSeguranca));

    /// <param name="caminhoInstantaneo">Pasta de um backup comum ou arquivo <c>.assetly</c>.</param>
    /// <param name="senha">Senha do arquivo, quando ele é protegido.</param>
    public ResultadoRestauracao Executar(string caminhoInstantaneo, string? senha = null)
    {
        if (string.IsNullOrWhiteSpace(caminhoInstantaneo))
        {
            throw new ArgumentException("selecione a pasta do backup a restaurar.");
        }

        var configuracaoAtual = _repositorioConfiguracaoBackup.Obter();
        var sincronizacaoAtual = _repositorioSincronizacao?.Obter();
        var copiaSeguranca = _servicoBackup.ExecutarBackup(_arquivoBanco, _diretorioDocumentos, _diretorioSeguranca);

        _servicoBackup.RestaurarBackup(caminhoInstantaneo, _arquivoBanco, _diretorioDocumentos, senha);
        _prepararBanco();
        _repositorioConfiguracaoBackup.Salvar(configuracaoAtual);
        if (sincronizacaoAtual is not null)
        {
            _repositorioSincronizacao!.Salvar(sincronizacaoAtual);
        }

        var relocalizados = 0;
        var semArquivo = 0;
        foreach (var documento in _repositorioDocumento.Listar())
        {
            var encontrado = _armazenamentoDocumento.Localizar(documento.CaminhoLocal.Valor);
            if (encontrado is null)
            {
                semArquivo++;
            }
            else if (!string.Equals(encontrado, documento.CaminhoLocal.Valor, StringComparison.OrdinalIgnoreCase))
            {
                documento.RelocalizarArquivo(new Dominio.Documento.CaminhoDocumentoLocal(encontrado));
                _repositorioDocumento.Salvar(documento);
                relocalizados++;
            }
        }

        return new ResultadoRestauracao(copiaSeguranca, relocalizados, semArquivo);
    }
}
