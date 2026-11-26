using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed record AvisoNotificacao(string Titulo, string Texto, bool Critico);

/// <summary>Quais alertas viram notificação do Windows: abertos (o usuário ainda não tomou ciência), de severidade aviso
/// ou crítica, e só uma vez cada — <paramref name="jaAvisados"/> guarda os já notificados nesta sessão.</summary>
public static class NotificacaoAlertas
{
    public static AvisoNotificacao? Novos(IEnumerable<Alerta> alertas, ISet<AlertaId> jaAvisados, Func<BemId, string> nomeBem)
    {
        ArgumentNullException.ThrowIfNull(alertas);
        ArgumentNullException.ThrowIfNull(jaAvisados);
        var novos = alertas
            .Where(a => a.Status == StatusAlerta.Aberto && a.Severidade is SeveridadeAlerta.Aviso or SeveridadeAlerta.Critico && !jaAvisados.Contains(a.Id))
            .OrderByDescending(a => a.Severidade)
            .ThenBy(a => a.PrazoEm ?? DateOnly.MaxValue)
            .ToList();
        novos.ForEach(a => jaAvisados.Add(a.Id));
        if (novos.Count == 0)
        {
            return null;
        }

        var critico = novos[0].Severidade == SeveridadeAlerta.Critico;
        if (novos.Count == 1)
        {
            return new AvisoNotificacao($"Assetly — {nomeBem(novos[0].BemId)}", novos[0].Mensagem, critico);
        }

        var primeiros = novos.Take(3).Select(a => $"• {nomeBem(a.BemId)}: {a.Mensagem}");
        return new AvisoNotificacao($"Assetly — {novos.Count} alertas precisam de atenção",
            string.Join("\n", primeiros) + (novos.Count > 3 ? $"\n… e mais {novos.Count - 3}" : string.Empty), critico);
    }
}
