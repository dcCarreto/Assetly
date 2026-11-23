using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;

namespace Assetly.Aplicacao.CasoDeUso;

/// <summary>Regra comum ao cadastro de fichas de veículo e imóvel: usar um bem existente do tipo certo
/// ou criar o bem junto com a ficha.</summary>
internal static class CriacaoBemComFicha
{
    internal static Bem ObterOuCriar(IRepositorioBem repositorioBem, BemId? existente, TipoBem tipo, string? nome, string nomePadrao,
        DateOnly? compradoEm, decimal? precoCompra, string? codigoMoeda)
    {
        if (existente is not null)
        {
            var bem = ApoioCasosUso.ExigirBem(repositorioBem, existente.Value);
            if (bem.Tipo != tipo)
            {
                throw new ArgumentException($"o bem \"{bem.Nome.Valor}\" não é do tipo esperado para esta ficha");
            }

            return bem;
        }

        var nomeFinal = string.IsNullOrWhiteSpace(nome) ? nomePadrao : nome;
        var novo = Bem.Criar(new NomeBem(nomeFinal), tipo);
        novo.AtualizarCompra(compradoEm, ApoioCasosUso.DinheiroOpcional(precoCompra, codigoMoeda));
        return novo;
    }
}
