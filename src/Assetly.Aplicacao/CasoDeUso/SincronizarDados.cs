using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;
using Assetly.Dominio.Backup;

namespace Assetly.Aplicacao.CasoDeUso;

/// <summary>Resultado de uma sincronização.</summary>
public sealed record ResultadoSincronizacao(AcaoSincronizacao Acao, bool Executada, MetadadosSincronizacao? Remoto);

/// <summary>Escolha do usuário quando os dois lados mudaram.</summary>
public enum EscolhaConflito
{
    ManterDesteComputador,
    UsarDaPasta,
    Cancelar
}

/// <summary>
/// Sincroniza os dados com a cópia na pasta sincronizada (ver <see cref="ConfiguracaoSincronizacao"/>):
/// envia quando só este computador mudou, recebe (restaurando, com cópia de segurança) quando só a
/// pasta mudou e pergunta ao usuário quando os dois mudaram. Usa a senha dos backups, se configurada.
/// </summary>
public sealed class SincronizarDados(
    IRepositorioConfiguracaoSincronizacao repositorioSincronizacao,
    IRepositorioConfiguracaoBackup repositorioBackup,
    ExportarDados exportarDados,
    ISincronizacaoPasta pasta,
    RestaurarBackup restaurarBackup,
    IProtecaoSenha? protecaoSenha,
    string arquivoBanco,
    string diretorioDocumentos,
    string nomeDispositivo)
{
    private readonly IRepositorioConfiguracaoSincronizacao _repositorioSincronizacao = repositorioSincronizacao ?? throw new ArgumentNullException(nameof(repositorioSincronizacao));
    private readonly IRepositorioConfiguracaoBackup _repositorioBackup = repositorioBackup ?? throw new ArgumentNullException(nameof(repositorioBackup));
    private readonly ExportarDados _exportarDados = exportarDados ?? throw new ArgumentNullException(nameof(exportarDados));
    private readonly ISincronizacaoPasta _pasta = pasta ?? throw new ArgumentNullException(nameof(pasta));
    private readonly RestaurarBackup _restaurarBackup = restaurarBackup ?? throw new ArgumentNullException(nameof(restaurarBackup));

    /// <param name="resolverConflito">Chamado só quando os dois lados mudaram.</param>
    public ResultadoSincronizacao Executar(Func<MetadadosSincronizacao, EscolhaConflito> resolverConflito)
    {
        ArgumentNullException.ThrowIfNull(resolverConflito);
        var configuracao = _repositorioSincronizacao.Obter();
        if (!configuracao.Ativa || string.IsNullOrWhiteSpace(configuracao.Pasta))
        {
            throw new InvalidOperationException("a sincronização está desligada. Escolha a pasta sincronizada no módulo Backup.");
        }

        if (!Directory.Exists(configuracao.Pasta))
        {
            throw new InvalidOperationException($"a pasta de sincronização não está acessível: {configuracao.Pasta}");
        }

        var senha = SenhaBackup.Obter(_repositorioBackup.Obter(), protecaoSenha);
        var impressaoLocal = _exportarDados.ImpressaoDigital();
        var remoto = _pasta.LerMetadados(configuracao.Pasta);
        var acao = configuracao.Decidir(impressaoLocal, remoto?.Impressao);
        if (acao == AcaoSincronizacao.Conflito && configuracao.UltimaImpressao is null && _exportarDados.SemDados())
        {
            // Computador novo (nada cadastrado, nunca sincronizado): simplesmente recebe os dados da pasta.
            acao = AcaoSincronizacao.Receber;
        }


        if (acao == AcaoSincronizacao.Conflito)
        {
            acao = resolverConflito(remoto!) switch
            {
                EscolhaConflito.ManterDesteComputador => AcaoSincronizacao.Enviar,
                EscolhaConflito.UsarDaPasta => AcaoSincronizacao.Receber,
                _ => AcaoSincronizacao.Conflito
            };
            if (acao == AcaoSincronizacao.Conflito)
            {
                return new ResultadoSincronizacao(AcaoSincronizacao.Conflito, false, remoto);
            }
        }

        switch (acao)
        {
            case AcaoSincronizacao.Enviar:
                _pasta.Enviar(configuracao.Pasta, arquivoBanco, diretorioDocumentos, senha,
                    new MetadadosSincronizacao(impressaoLocal, nomeDispositivo, DateTimeOffset.Now, senha is not null));
                configuracao.RegistrarSincronizacao(impressaoLocal, DateTimeOffset.Now);
                break;

            case AcaoSincronizacao.Receber:
                if (remoto!.Criptografado && senha is null)
                {
                    throw new InvalidOperationException(
                        "a cópia na pasta sincronizada é protegida por senha. Ative a proteção por senha dos backups (módulo Backup) com a mesma senha usada no outro computador.");
                }

                _restaurarBackup.Executar(_pasta.CaminhoCopia(configuracao.Pasta), remoto.Criptografado ? senha : null);
                configuracao = _repositorioSincronizacao.Obter();
                configuracao.RegistrarSincronizacao(_exportarDados.ImpressaoDigital(), DateTimeOffset.Now);
                break;

            default:
                configuracao.RegistrarSincronizacao(impressaoLocal, DateTimeOffset.Now);
                break;
        }

        _repositorioSincronizacao.Salvar(configuracao);
        return new ResultadoSincronizacao(acao, acao != AcaoSincronizacao.NadaAFazer, remoto);
    }
}
