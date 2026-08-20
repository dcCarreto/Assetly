using System.Globalization;
using System.Text.RegularExpressions;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Manutencao;
using Assetly.Dominio.Saude;

namespace Assetly.Apresentacao.Visao;

/// <summary>Formata datas, dinheiro e rótulos de enum para exibição em tela — sempre em pt-BR, nunca a cultura do SO.</summary>
internal static partial class Formatador
{
    /// <summary>Texto usado quando um valor está ausente (custo não lançado, data não concluída, campo opcional vazio).</summary>
    private const string ValorAusente = "—";

    private static readonly CultureInfo CulturaMonetaria = CultureInfo.GetCultureInfo("pt-BR");

    internal static string Data(DateOnly? data) =>
        data is null ? ValorAusente : data.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    internal static string Dinheiro(Dinheiro? dinheiro)
    {
        if (dinheiro is null)
        {
            return ValorAusente;
        }

        return dinheiro.CodigoMoeda == "BRL"
            ? dinheiro.Valor.ToString("C2", CulturaMonetaria)
            : $"{dinheiro.CodigoMoeda} {dinheiro.Valor.ToString("N2", CulturaMonetaria)}";
    }

    internal static string TextoOuTraco(string? valor) => string.IsNullOrWhiteSpace(valor) ? ValorAusente : valor;

    /// <summary>Rótulo legível e acentuado para exibição em célula/badge. Mapeamento explícito
    /// por enum, já que acentuação não pode ser derivada do nome do valor em C#.</summary>
    internal static string TextoEnum<TEnum>(TEnum valor) where TEnum : struct, Enum => (object)valor switch
    {
        TipoBem v => RotuloTipoBem(v),
        StatusBem v => RotuloStatusBem(v),
        TipoGarantia v => RotuloTipoGarantia(v),
        StatusGarantia v => RotuloStatusGarantia(v),
        TipoManutencao v => RotuloTipoManutencao(v),
        StatusManutencao v => RotuloStatusManutencao(v),
        TipoAlerta v => RotuloTipoAlerta(v),
        SeveridadeAlerta v => RotuloSeveridadeAlerta(v),
        StatusAlerta v => RotuloStatusAlerta(v),
        TipoDocumento v => RotuloTipoDocumento(v),
        StatusDocumento v => RotuloStatusDocumento(v),
        TipoSaude v => RotuloTipoSaude(v),
        StatusSaude v => RotuloStatusSaude(v),
        TipoGastoSaude v => RotuloTipoGastoSaude(v),
        _ => RegexSeparadorMaiuscula().Replace(valor.ToString(), " $1").ToLowerInvariant()
    };

    internal static string ClasseEtiqueta<TEnum>(TEnum valor) where TEnum : struct, Enum =>
        "etiqueta-" + RegexSeparadorMaiuscula().Replace(valor.ToString(), "-$1").ToLowerInvariant();

    private static string RotuloTipoBem(TipoBem valor) => valor switch
    {
        TipoBem.Eletronico => "Eletrônico",
        TipoBem.Eletrodomestico => "Eletrodoméstico",
        TipoBem.Veiculo => "Veículo",
        TipoBem.Ferramenta => "Ferramenta",
        TipoBem.Movel => "Móvel",
        TipoBem.Imovel => "Imóvel",
        _ => "Outro"
    };

    private static string RotuloStatusBem(StatusBem valor) => valor switch
    {
        StatusBem.Ativo => "Ativo",
        StatusBem.EmManutencao => "Em manutenção",
        StatusBem.Descartado => "Descartado",
        _ => "Arquivado"
    };

    private static string RotuloTipoGarantia(TipoGarantia valor) => valor switch
    {
        TipoGarantia.Fabricante => "Fabricante",
        TipoGarantia.Estendida => "Estendida",
        TipoGarantia.Loja => "Loja",
        TipoGarantia.Servico => "Serviço",
        _ => "Outra"
    };

    private static string RotuloStatusGarantia(StatusGarantia valor) => valor switch
    {
        StatusGarantia.NaoIniciada => "Não iniciada",
        StatusGarantia.Ativa => "Ativa",
        StatusGarantia.PertoDoVencimento => "Perto do vencimento",
        _ => "Vencida"
    };

    private static string RotuloTipoManutencao(TipoManutencao valor) => valor switch
    {
        TipoManutencao.Preventiva => "Preventiva",
        TipoManutencao.Corretiva => "Corretiva",
        TipoManutencao.Inspecao => "Inspeção",
        TipoManutencao.Limpeza => "Limpeza",
        _ => "Outra"
    };

    private static string RotuloStatusManutencao(StatusManutencao valor) => valor switch
    {
        StatusManutencao.Agendada => "Agendada",
        StatusManutencao.Concluida => "Concluída",
        StatusManutencao.Atrasada => "Atrasada",
        _ => "Cancelada"
    };

    private static string RotuloTipoAlerta(TipoAlerta valor) => valor switch
    {
        TipoAlerta.GarantiaVencendo => "Garantia vencendo",
        TipoAlerta.GarantiaVencida => "Garantia vencida",
        TipoAlerta.ManutencaoProxima => "Manutenção próxima",
        TipoAlerta.ManutencaoAtrasada => "Manutenção atrasada",
        _ => "Documento ausente"
    };

    private static string RotuloSeveridadeAlerta(SeveridadeAlerta valor) => valor switch
    {
        SeveridadeAlerta.Informativo => "Informativo",
        SeveridadeAlerta.Aviso => "Aviso",
        _ => "Crítico"
    };

    private static string RotuloStatusAlerta(StatusAlerta valor) => valor switch
    {
        StatusAlerta.Aberto => "Aberto",
        StatusAlerta.Ciente => "Ciente",
        _ => "Resolvido"
    };

    private static string RotuloTipoDocumento(TipoDocumento valor) => valor switch
    {
        TipoDocumento.NotaFiscal => "Nota fiscal",
        TipoDocumento.Recibo => "Recibo",
        TipoDocumento.CertificadoGarantia => "Certificado de garantia",
        TipoDocumento.Manual => "Manual",
        TipoDocumento.Foto => "Foto",
        _ => "Outro"
    };

    private static string RotuloStatusDocumento(StatusDocumento valor) => valor switch
    {
        StatusDocumento.Disponivel => "Disponível",
        StatusDocumento.Ausente => "Ausente",
        _ => "Arquivado"
    };

    private static string RotuloTipoSaude(TipoSaude valor) => valor switch
    {
        TipoSaude.Convenio => "Convênio",
        TipoSaude.Ortodontia => "Ortodontia",
        TipoSaude.Farmacia => "Farmácia",
        TipoSaude.Emergencia => "Emergência",
        _ => "Outra"
    };

    private static string RotuloStatusSaude(StatusSaude valor) => valor switch
    {
        StatusSaude.NaoIniciada => "Não iniciada",
        StatusSaude.Ativa => "Ativa",
        StatusSaude.PertoDoVencimento => "Perto do vencimento",
        StatusSaude.Vencida => "Vencida",
        _ => "Cancelada"
    };

    private static string RotuloTipoGastoSaude(TipoGastoSaude valor) => valor switch
    {
        TipoGastoSaude.Farmacia => "Farmácia",
        TipoGastoSaude.Consulta => "Consulta",
        TipoGastoSaude.Exame => "Exame",
        TipoGastoSaude.Procedimento => "Procedimento",
        _ => "Outro"
    };

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex RegexSeparadorMaiuscula();
}
