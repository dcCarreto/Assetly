using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;

namespace Assetly.Apresentacao.Visao;

/// <summary>Em que módulo um alerta é resolvido — usado pelas linhas clicáveis do painel e pelos contadores da barra lateral.</summary>
internal static class ModuloDoAlerta
{
    internal static string Titulo(Alerta alerta, ISet<BemId> veiculos, ISet<BemId> imoveis) => alerta.Tipo switch
    {
        TipoAlerta.GarantiaVencendo or TipoAlerta.GarantiaVencida => "Garantias",
        TipoAlerta.ManutencaoProxima or TipoAlerta.ManutencaoAtrasada => "Manutenções",
        TipoAlerta.ConvenioVencendo or TipoAlerta.ConvenioVencido => "Convênios",
        TipoAlerta.DocumentoAusente => "Documentos",
        TipoAlerta.LicenciamentoVencendo or TipoAlerta.LicenciamentoVencido or TipoAlerta.RevisaoVeiculo => ModulosVeiculoImovel.TituloAutomoveis,
        TipoAlerta.ContratoAluguel => ModulosVeiculoImovel.TituloImoveis,
        TipoAlerta.SeguroVencendo or TipoAlerta.SeguroVencido or TipoAlerta.ContaVencendo or TipoAlerta.ContaVencida
            => veiculos.Contains(alerta.BemId) ? ModulosVeiculoImovel.TituloAutomoveis
            : imoveis.Contains(alerta.BemId) ? ModulosVeiculoImovel.TituloImoveis : "Alertas",
        _ => "Alertas"
    };
}
