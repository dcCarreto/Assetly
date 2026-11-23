using Assetly.Dominio.Compartilhado;

namespace Assetly.Dominio.Backup;

/// <summary>Decisão tomada ao comparar os dados deste computador com a cópia na pasta sincronizada.</summary>
public enum AcaoSincronizacao
{
    /// <summary>Nada mudou dos dois lados desde a última sincronização.</summary>
    NadaAFazer,
    /// <summary>Só este computador mudou (ou ainda não há cópia na pasta): enviar.</summary>
    Enviar,
    /// <summary>Só a cópia da pasta mudou (outro computador): trazer.</summary>
    Receber,
    /// <summary>Os dois lados mudaram: o usuário escolhe qual versão fica.</summary>
    Conflito
}

/// <summary>
/// Sincronização opcional entre computadores por meio de uma pasta já sincronizada pelo usuário
/// (OneDrive, Google Drive, Dropbox...). O Assetly mantém nessa pasta uma cópia completa dos dados e
/// compara impressões digitais (hash do conteúdo) para saber de que lado houve mudança. Pensada para
/// uma pessoa usando computadores diferentes em momentos diferentes — não para edição simultânea.
/// </summary>
public sealed class ConfiguracaoSincronizacao
{
    public string? Pasta { get; private set; }
    public bool Ativa { get; private set; }

    /// <summary>Impressão digital dos dados na última sincronização bem-sucedida (igual dos dois lados naquele momento).</summary>
    public string? UltimaImpressao { get; private set; }

    public DateTimeOffset? UltimaSincronizacaoEm { get; private set; }

    private ConfiguracaoSincronizacao(string? pasta, bool ativa, string? ultimaImpressao, DateTimeOffset? ultimaSincronizacaoEm)
    {
        Pasta = pasta;
        Ativa = ativa && !string.IsNullOrWhiteSpace(pasta);
        UltimaImpressao = ultimaImpressao;
        UltimaSincronizacaoEm = ultimaSincronizacaoEm;
    }

    public static ConfiguracaoSincronizacao Padrao() => new(null, false, null, null);

    public static ConfiguracaoSincronizacao Restaurar(string? pasta, bool ativa, string? ultimaImpressao, DateTimeOffset? ultimaSincronizacaoEm) =>
        new(pasta, ativa, ultimaImpressao, ultimaSincronizacaoEm);

    public void Ativar(string pasta)
    {
        var nova = Validacao.ExigirTexto(pasta, "pasta de sincronização", 500);
        if (!string.Equals(nova, Pasta, StringComparison.OrdinalIgnoreCase))
        {
            // Pasta diferente: a próxima sincronização compara do zero.
            UltimaImpressao = null;
            UltimaSincronizacaoEm = null;
        }

        Pasta = nova;
        Ativa = true;
    }

    public void Desativar() => Ativa = false;

    public void RegistrarSincronizacao(string impressao, DateTimeOffset momento)
    {
        UltimaImpressao = Validacao.ExigirTexto(impressao, "impressão digital", 200);
        UltimaSincronizacaoEm = momento;
    }

    /// <summary>Compara a impressão digital local e a da pasta com a da última sincronização.</summary>
    public AcaoSincronizacao Decidir(string impressaoLocal, string? impressaoRemota)
    {
        if (impressaoRemota is null)
        {
            return AcaoSincronizacao.Enviar;
        }

        if (impressaoLocal == impressaoRemota)
        {
            return AcaoSincronizacao.NadaAFazer;
        }

        var localMudou = impressaoLocal != UltimaImpressao;
        var remotoMudou = impressaoRemota != UltimaImpressao;
        return (localMudou, remotoMudou) switch
        {
            (true, false) => AcaoSincronizacao.Enviar,
            (false, true) => AcaoSincronizacao.Receber,
            _ => AcaoSincronizacao.Conflito
        };
    }
}
