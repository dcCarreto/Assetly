using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;
using Assetly.Dominio.Backup;

namespace Assetly.Aplicacao.CasoDeUso;

/// <summary>Resultado de uma sincronização automática para a interface mostrar: se dados chegaram (a tela precisa
/// recarregar), um aviso para o usuário (algo ficou pendente) ou a falha (registrada, sem interromper o uso).</summary>
public sealed record SituacaoSincronizacao(bool DadosRecebidos, string? Aviso, Exception? Falha);

/// <summary>
/// Regras da sincronização que acontece sozinha, sem o usuário pedir:
/// <list type="bullet">
/// <item>ao abrir o programa: envia ou recebe; se os dois lados mudaram, pergunta ao usuário;</item>
/// <item>durante o uso (a cada <see cref="IntervaloEnvioPadrao"/>) e ao fechar: só envia — os dados deste
/// computador nunca são substituídos com o programa em uso; um conflito ou uma mudança vinda do outro
/// computador fica pendente e vira um aviso.</item>
/// </list>
/// Não guarda estado de interface: devolve <see cref="SituacaoSincronizacao"/> e a tela decide como mostrar.
/// </summary>
public sealed class SincronizacaoAutomatica(
    Func<SincronizarDados> criarSincronizacao,
    IRepositorioConfiguracaoSincronizacao repositorioSincronizacao,
    TimeSpan? intervaloEnvio = null)
{
    public static readonly TimeSpan IntervaloEnvioPadrao = TimeSpan.FromMinutes(10);

    public const string AvisoConflito =
        "os dados mudaram aqui e no outro computador. Clique em \"Sincronizar agora\" para escolher qual versão manter.";

    public const string AvisoRecebimentoPendente =
        "há alterações feitas no outro computador. Elas serão trazidas na próxima vez que o Assetly abrir ou em \"Sincronizar agora\".";

    private readonly Func<SincronizarDados> _criarSincronizacao = criarSincronizacao ?? throw new ArgumentNullException(nameof(criarSincronizacao));
    private readonly IRepositorioConfiguracaoSincronizacao _repositorioSincronizacao = repositorioSincronizacao ?? throw new ArgumentNullException(nameof(repositorioSincronizacao));
    private readonly TimeSpan _intervaloEnvio = intervaloEnvio ?? IntervaloEnvioPadrao;
    private DateTimeOffset? _ultimaTentativa;

    public bool Ativa => _repositorioSincronizacao.Obter().Ativa;

    /// <summary>Ao abrir: pode receber; num conflito, <paramref name="perguntarConflito"/> decide.</summary>
    public SituacaoSincronizacao? AoAbrir(DateTimeOffset agora, Func<MetadadosSincronizacao, EscolhaConflito> perguntarConflito) =>
        Executar(agora, perguntarConflito, permitirReceber: true);

    /// <summary>Durante o uso: só envia, e só quando o intervalo passou desde a última tentativa (nulo se não era hora).</summary>
    public SituacaoSincronizacao? DuranteUso(DateTimeOffset agora)
    {
        _ultimaTentativa ??= agora;
        return agora - _ultimaTentativa.Value >= _intervaloEnvio ? Executar(agora, _ => EscolhaConflito.Cancelar, permitirReceber: false) : null;
    }

    /// <summary>Ao fechar: só envia.</summary>
    public SituacaoSincronizacao? AoFechar(DateTimeOffset agora) => Executar(agora, _ => EscolhaConflito.Cancelar, permitirReceber: false);

    private SituacaoSincronizacao? Executar(DateTimeOffset agora, Func<MetadadosSincronizacao, EscolhaConflito> perguntarConflito, bool permitirReceber)
    {
        if (!Ativa)
        {
            return null;
        }

        _ultimaTentativa = agora;
        try
        {
            var resultado = _criarSincronizacao().Executar(perguntarConflito, permitirReceber);
            var aviso = (resultado.Acao, resultado.Executada) switch
            {
                (AcaoSincronizacao.Conflito, false) => AvisoConflito,
                (AcaoSincronizacao.Receber, false) => AvisoRecebimentoPendente,
                _ => null
            };
            return new SituacaoSincronizacao(resultado.Acao == AcaoSincronizacao.Receber && resultado.Executada, aviso, null);
        }
        catch (Exception excecao) when (excecao is not OutOfMemoryException)
        {
            return new SituacaoSincronizacao(false, null, excecao);
        }
    }
}
