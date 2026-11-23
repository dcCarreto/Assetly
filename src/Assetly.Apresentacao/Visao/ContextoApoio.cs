using Assetly.Dominio.Bem;

namespace Assetly.Apresentacao.Visao;

/// <summary>Pequenos helpers que todo módulo precisa e que dependem do <see cref="ContextoAplicacao"/>
/// — não são específicos de nenhum módulo, então não pertencem a nenhum deles.</summary>
internal static class ContextoApoio
{
    internal static DateOnly Hoje() => DateOnly.FromDateTime(DateTime.Now);

    internal static string NomeBem(ContextoAplicacao contexto, BemId bemId) =>
        contexto.RepositorioBem.BuscarPorId(bemId)?.Nome.Valor ?? "Bem não encontrado";

    /// <summary>Carrega os nomes de todos os bens de uma vez — para montar tabelas sem fazer uma
    /// consulta ao banco por linha a cada ciclo de atualização.</summary>
    internal static Func<BemId, string> NomesBens(ContextoAplicacao contexto)
    {
        var nomes = contexto.ListarBens().Executar().ToDictionary(bem => bem.Id, bem => bem.Nome.Valor);
        return bemId => nomes.TryGetValue(bemId, out var nome) ? nome : "Bem não encontrado";
    }
}
