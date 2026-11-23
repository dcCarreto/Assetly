namespace Assetly.Aplicacao.Servico;

/// <summary>Atualização do próprio programa (não dos dados): procurar versão nova publicada, baixar o
/// instalador e executá-lo. Só funciona na cópia instalada pelo instalador; rodando pela Visual Studio
/// ou pelo executável avulso, <see cref="Disponivel"/> é falso e nada acontece.</summary>
public interface IServicoAtualizacao
{
    bool Disponivel { get; }

    /// <summary>Versão em execução, para exibição (ex.: "23/11/2026").</summary>
    string VersaoAtual { get; }

    /// <summary>Versão nova encontrada (para exibição), ou nulo se já está na mais recente.</summary>
    Task<string?> ProcurarAsync(CancellationToken cancelamento = default);

    /// <summary>Baixa e confere (SHA-256) o instalador da versão encontrada; progresso de 0 a 100.</summary>
    Task BaixarAsync(Action<int>? progresso, CancellationToken cancelamento = default);

    /// <summary>Inicia o instalador baixado em modo silencioso. Quem chama deve fechar o programa logo em
    /// seguida — o instalador espera o Assetly fechar. Com <paramref name="reabrirDepois"/>, ele abre o
    /// Assetly de novo ao terminar.</summary>
    void IniciarInstalacao(bool reabrirDepois);
}
