using Assetly.Aplicacao.Servico;

namespace Assetly.Apresentacao.Visao;

/// <summary>Substituto usado quando o serviço de atualização não pôde ser criado: o programa funciona
/// normalmente, só sem atualização automática.</summary>
internal sealed class ServicoAtualizacaoIndisponivel : IServicoAtualizacao
{
    public bool Disponivel => false;

    public string VersaoAtual => "desconhecida";

    public Task<string?> ProcurarAsync(CancellationToken cancelamento = default) => Task.FromResult<string?>(null);

    public Task BaixarAsync(Action<int>? progresso, CancellationToken cancelamento = default) => Task.CompletedTask;

    public void IniciarInstalacao(bool reabrirDepois) => throw new InvalidOperationException("atualização indisponível.");
}
