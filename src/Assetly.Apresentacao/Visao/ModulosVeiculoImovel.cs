using Assetly.Dominio.Despesa;

namespace Assetly.Apresentacao.Visao;

/// <summary>Montagem dos módulos "Automóveis" e "Imóveis" a partir das suas páginas.</summary>
internal static class ModulosVeiculoImovel
{
    internal const string TituloAutomoveis = "Automóveis";
    internal const string TituloImoveis = "Imóveis";

    internal static ModuloComAbas Automoveis(ContextoAplicacao contexto)
    {
        var despesas = new PaginaDespesas(contexto, "Despesas", "IPVA, licenciamento, seguro, multas, pedágios e outras contas dos veículos.",
            "Veículo", TiposDespesa.ParaVeiculo, () => contexto.ListarVeiculos().Executar().Select(v => v.BemId).ToList());
        var abastecimentos = new PaginaAbastecimentos(contexto);
        var veiculos = new PaginaVeiculos(contexto, abastecimentos, despesas);
        return new ModuloComAbas(TituloAutomoveis, "Veículos e combustível", () => veiculos.Quantidade, veiculos, abastecimentos, despesas);
    }

    internal static ModuloComAbas Imoveis(ContextoAplicacao contexto)
    {
        var contas = new PaginaDespesas(contexto, "Contas", "IPTU, condomínio, água, energia, gás, internet e outras contas dos imóveis.",
            "Imóvel", TiposDespesa.ParaImovel, () => contexto.ListarImoveis().Executar().Select(i => i.BemId).ToList(), "conta");
        var imoveis = new PaginaImoveis(contexto, contas);
        return new ModuloComAbas(TituloImoveis, "Endereços, contas e locação", () => imoveis.Quantidade, imoveis, contas);
    }
}
