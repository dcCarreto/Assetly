using Assetly.Aplicacao.Excecao;
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
/// de backup atual preservada (destino/agenda são do computador, não dos dados), caminhos de
/// documentos corrigidos quando o backup veio de outra pasta ou perfil de usuário e arquivos que nenhum
/// documento restaurado usa apagados da pasta privada.
/// <para>A cópia de segurança usa a senha dos backups, quando ligada, e só as últimas
/// <see cref="CopiasSegurancaRetidas"/> são mantidas (cada sincronização recebida também gera uma). Se a troca
/// falhar no meio, o estado anterior é devolvido automaticamente a partir dessa cópia.</para>
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
    IRepositorioConfiguracaoSincronizacao? repositorioSincronizacao = null,
    IProtecaoSenha? protecaoSenha = null)
{
    public const int CopiasSegurancaRetidas = 5;

    private readonly IProtecaoSenha? _protecaoSenha = protecaoSenha;
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
        var senhaCopia = SenhaDaCopiaSeguranca(configuracaoAtual);
        var copiaSeguranca = _servicoBackup.ExecutarBackup(_arquivoBanco, _diretorioDocumentos, _diretorioSeguranca, senhaCopia);
        _servicoBackup.ManterUltimos(_diretorioSeguranca, CopiasSegurancaRetidas);

        try
        {
            _servicoBackup.RestaurarBackup(caminhoInstantaneo, _arquivoBanco, _diretorioDocumentos, senha);
        }
        catch (RestauracaoIncompletaExcecao falha)
        {
            Desfazer(copiaSeguranca, senhaCopia, falha);
        }

        try
        {
            _prepararBanco();
        }
        catch (Exception falha) when (falha is not OutOfMemoryException)
        {
            Desfazer(copiaSeguranca, senhaCopia, falha);
        }

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

        RemoverArquivosSemDocumento();
        return new ResultadoRestauracao(copiaSeguranca, relocalizados, semArquivo);
    }

    /// <summary>Senha dos backups para cifrar a cópia de segurança. Se ela não puder ser lida (outro usuário do
    /// Windows), a cópia sai sem senha em vez de impedir a restauração — fica na pasta de dados, ao lado dos
    /// próprios dados, que também não são cifrados.</summary>
    private string? SenhaDaCopiaSeguranca(Dominio.Backup.ConfiguracaoBackup configuracao)
    {
        try
        {
            return SenhaBackup.Obter(configuracao, _protecaoSenha);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>Devolve o estado de antes da restauração e sempre lança: a restauração não aconteceu.</summary>
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private void Desfazer(string copiaSeguranca, string? senhaCopia, Exception falha)
    {
        try
        {
            _servicoBackup.RestaurarBackup(copiaSeguranca, _arquivoBanco, _diretorioDocumentos, senhaCopia);
            _prepararBanco();
            RemoverArquivosSemDocumento();
        }
        catch (Exception falhaAoDesfazer) when (falhaAoDesfazer is not OutOfMemoryException)
        {
            throw new InvalidOperationException(
                $"a restauração falhou ({falha.Message}) e não foi possível voltar sozinho ao estado anterior. " +
                $"Restaure manualmente a cópia de segurança: {copiaSeguranca}", falhaAoDesfazer);
        }

        throw new InvalidOperationException($"a restauração falhou e seus dados voltaram ao estado anterior: {falha.Message}", falha);
    }

    /// <summary>A restauração junta os arquivos do backup aos de hoje: os que nenhum documento usa sobram para sempre
    /// (e entrariam em todo backup e sincronização).</summary>
    private void RemoverArquivosSemDocumento() =>
        _armazenamentoDocumento.RemoverNaoReferenciados(_repositorioDocumento.Listar().Select(d => d.CaminhoLocal.Valor).ToList());
}
