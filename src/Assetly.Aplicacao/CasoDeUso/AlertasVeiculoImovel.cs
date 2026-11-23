using System.Globalization;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Despesa;
using Assetly.Dominio.Imovel;
using Assetly.Dominio.Veiculo;

namespace Assetly.Aplicacao.CasoDeUso;

internal delegate void ExigirAlerta(BemId bemId, TipoAlerta tipo, SeveridadeAlerta severidade, string mensagem, DateOnly? prazoEm);

/// <summary>
/// Regras de alerta de veículos, imóveis e contas, usadas por <see cref="GerarAlertasAutomaticos"/>.
/// As mensagens são estáveis (não mudam a cada km rodado ou a cada dia) para que a deduplicação e a
/// resolução automática funcionem: um alerta só muda quando a situação muda de fato.
/// </summary>
internal static class AlertasVeiculoImovel
{
    private const string FormatoData = "dd/MM/yyyy";
    private const int TamanhoDescricao = 100;

    internal static void GerarParaVeiculo(Veiculo veiculo, DateOnly hoje, ExigirAlerta exigir)
    {
        Prazo(veiculo.BemId, veiculo.VencimentoLicenciamento, hoje, Veiculo.DiasAvisoVencimento, exigir,
            TipoAlerta.LicenciamentoVencendo, data => $"Licenciamento vence em {Data(data)}.",
            TipoAlerta.LicenciamentoVencido, data => $"Licenciamento vencido em {Data(data)}.");

        Prazo(veiculo.BemId, veiculo.VencimentoSeguro, hoje, Veiculo.DiasAvisoVencimento, exigir,
            TipoAlerta.SeguroVencendo, data => $"Seguro do veículo vence em {Data(data)}.",
            TipoAlerta.SeguroVencido, data => $"Seguro do veículo vencido em {Data(data)}.");

        if (veiculo.ProximaRevisaoKm is { } kmRevisao)
        {
            var km = kmRevisao.ToString("N0", CultureInfo.GetCultureInfo("pt-BR"));
            if (veiculo.Quilometragem >= kmRevisao)
            {
                exigir(veiculo.BemId, TipoAlerta.RevisaoVeiculo, SeveridadeAlerta.Critico, $"Revisão atrasada: prevista aos {km} km.", null);
            }
            else if (veiculo.Quilometragem >= kmRevisao - Veiculo.KmAvisoRevisao)
            {
                exigir(veiculo.BemId, TipoAlerta.RevisaoVeiculo, SeveridadeAlerta.Aviso, $"Revisão próxima: prevista aos {km} km.", null);
            }
        }

        if (veiculo.ProximaRevisaoEm is { } dataRevisao)
        {
            if (dataRevisao < hoje)
            {
                exigir(veiculo.BemId, TipoAlerta.RevisaoVeiculo, SeveridadeAlerta.Critico, $"Revisão atrasada desde {Data(dataRevisao)}.", dataRevisao);
            }
            else if (dataRevisao <= hoje.AddDays(Veiculo.DiasAvisoRevisao))
            {
                exigir(veiculo.BemId, TipoAlerta.RevisaoVeiculo, SeveridadeAlerta.Aviso, $"Revisão prevista para {Data(dataRevisao)}.", dataRevisao);
            }
        }
    }

    internal static void GerarParaImovel(Imovel imovel, DateOnly hoje, ExigirAlerta exigir)
    {
        Prazo(imovel.BemId, imovel.VencimentoSeguro, hoje, Imovel.DiasAvisoSeguro, exigir,
            TipoAlerta.SeguroVencendo, data => $"Seguro do imóvel vence em {Data(data)}.",
            TipoAlerta.SeguroVencido, data => $"Seguro do imóvel vencido em {Data(data)}.");

        if (imovel.Alugado && imovel.FimContratoAluguel is { } fimContrato)
        {
            if (fimContrato < hoje)
            {
                exigir(imovel.BemId, TipoAlerta.ContratoAluguel, SeveridadeAlerta.Critico, $"Contrato de aluguel terminou em {Data(fimContrato)}.", fimContrato);
            }
            else if (fimContrato <= hoje.AddDays(Imovel.DiasAvisoContrato))
            {
                exigir(imovel.BemId, TipoAlerta.ContratoAluguel, SeveridadeAlerta.Aviso, $"Contrato de aluguel termina em {Data(fimContrato)}.", fimContrato);
            }
        }
    }

    internal static void GerarParaDespesa(Despesa despesa, DateOnly hoje, ExigirAlerta exigir)
    {
        var descricao = despesa.Descricao.Length <= TamanhoDescricao ? despesa.Descricao : despesa.Descricao[..(TamanhoDescricao - 3)] + "...";
        switch (despesa.StatusEm(hoje))
        {
            case StatusDespesa.Vencida:
                exigir(despesa.BemId, TipoAlerta.ContaVencida, SeveridadeAlerta.Critico, $"Conta vencida em {Data(despesa.Vencimento)}: {descricao}.", despesa.Vencimento);
                break;
            case StatusDespesa.AVencer:
                exigir(despesa.BemId, TipoAlerta.ContaVencendo, SeveridadeAlerta.Aviso, $"Conta vence em {Data(despesa.Vencimento)}: {descricao}.", despesa.Vencimento);
                break;
        }
    }

    private static void Prazo(BemId bemId, DateOnly? vencimento, DateOnly hoje, int diasAviso, ExigirAlerta exigir,
        TipoAlerta tipoVencendo, Func<DateOnly, string> mensagemVencendo, TipoAlerta tipoVencido, Func<DateOnly, string> mensagemVencido)
    {
        if (vencimento is not { } data)
        {
            return;
        }

        if (data < hoje)
        {
            exigir(bemId, tipoVencido, SeveridadeAlerta.Critico, mensagemVencido(data), data);
        }
        else if (data <= hoje.AddDays(diasAviso))
        {
            exigir(bemId, tipoVencendo, SeveridadeAlerta.Aviso, mensagemVencendo(data), data);
        }
    }

    private static string Data(DateOnly data) => data.ToString(FormatoData, CultureInfo.InvariantCulture);
}
