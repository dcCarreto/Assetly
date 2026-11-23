using System.Globalization;
using System.Text.RegularExpressions;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Backup;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Manutencao;
using Assetly.Dominio.Preferencias;
using Assetly.Dominio.Despesa;
using Assetly.Dominio.Imovel;
using Assetly.Dominio.Saude;
using Assetly.Dominio.Veiculo;

namespace Assetly.Apresentacao.Visao;

/// <summary>Formata datas, dinheiro e rótulos de enum para exibição em tela — sempre em pt-BR, nunca a cultura do SO.</summary>
internal static partial class Formatador
{
    /// <summary>Texto usado quando um valor está ausente (custo não lançado, data não concluída, campo opcional vazio).</summary>
    private const string ValorAusente = "—";

    private static readonly CultureInfo CulturaMonetaria = CultureInfo.GetCultureInfo("pt-BR");

    internal static string Data(DateOnly? data) =>
        data is null ? ValorAusente : data.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    internal static string DataHora(DateTime momento) => momento.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

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

    /// <summary>Soma valores agrupando por moeda — somar BRL com USD num total único em reais seria
    /// enganoso. Ex.: "R$ 1.200,00" ou "R$ 1.200,00 + USD 300,00".</summary>
    internal static string Total(IEnumerable<Dinheiro?> valores)
    {
        var porMoeda = valores
            .Where(valor => valor is not null)
            .GroupBy(valor => valor!.CodigoMoeda)
            .OrderBy(grupo => grupo.Key == "BRL" ? 0 : 1)
            .ThenBy(grupo => grupo.Key, StringComparer.Ordinal)
            .Select(grupo => Dinheiro(Assetly.Dominio.Compartilhado.Dinheiro.De(grupo.Sum(valor => valor!.Valor), grupo.Key)))
            .ToList();
        return porMoeda.Count == 0 ? 0m.ToString("C2", CulturaMonetaria) : string.Join(" + ", porMoeda);
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
        FrequenciaBackup v => RotuloFrequenciaBackup(v),
        TemaVisual v => RotuloTemaVisual(v),
        CorDestaque v => RotuloCorDestaque(v),
        DensidadeTabela v => RotuloDensidadeTabela(v),
        IntervaloAtualizacao v => RotuloIntervaloAtualizacao(v),
        TipoCombustivel v => RotuloCombustivel(v),
        TipoDespesa v => RotuloTipoDespesa(v),
        StatusDespesa v => RotuloStatusDespesa(v),
        TipoImovel v => RotuloTipoImovel(v),
        SituacaoImovel v => RotuloSituacaoImovel(v),
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
        TipoBem.Pessoa => "Pessoa (titular)",
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
        TipoAlerta.ConvenioVencendo => "Convênio vencendo",
        TipoAlerta.ConvenioVencido => "Convênio vencido",
        TipoAlerta.LicenciamentoVencendo => "Licenciamento vencendo",
        TipoAlerta.LicenciamentoVencido => "Licenciamento vencido",
        TipoAlerta.SeguroVencendo => "Seguro vencendo",
        TipoAlerta.SeguroVencido => "Seguro vencido",
        TipoAlerta.RevisaoVeiculo => "Revisão do veículo",
        TipoAlerta.ContaVencendo => "Conta a vencer",
        TipoAlerta.ContaVencida => "Conta vencida",
        TipoAlerta.ContratoAluguel => "Contrato de aluguel",
        _ => "Nota fiscal ausente"
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
        TipoDocumento.Escritura => "Escritura",
        TipoDocumento.Contrato => "Contrato",
        TipoDocumento.DocumentoVeiculo => "Documento do veículo (CRLV)",
        TipoDocumento.Apolice => "Apólice de seguro",
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

    private static string RotuloFrequenciaBackup(FrequenciaBackup valor) => valor switch
    {
        FrequenciaBackup.Diaria => "Diária",
        FrequenciaBackup.Semanal => "Semanal",
        _ => "Mensal"
    };

    private static string RotuloTemaVisual(TemaVisual valor) => valor switch
    {
        TemaVisual.Claro => "Claro",
        _ => "Escuro"
    };

    private static string RotuloCorDestaque(CorDestaque valor) => valor switch
    {
        CorDestaque.Violeta => "Violeta",
        CorDestaque.Verde => "Verde",
        CorDestaque.Ambar => "Âmbar",
        _ => "Ciano"
    };

    private static string RotuloDensidadeTabela(DensidadeTabela valor) => valor switch
    {
        DensidadeTabela.Compacta => "Compacta",
        _ => "Confortável"
    };

    private static string RotuloIntervaloAtualizacao(IntervaloAtualizacao valor) => valor switch
    {
        IntervaloAtualizacao.Segundos10 => "10 segundos",
        IntervaloAtualizacao.Segundos30 => "30 segundos",
        IntervaloAtualizacao.Segundos60 => "60 segundos",
        _ => "5 segundos"
    };

    private static string RotuloCombustivel(TipoCombustivel valor) => valor switch
    {
        TipoCombustivel.Gasolina => "Gasolina",
        TipoCombustivel.Etanol => "Etanol",
        TipoCombustivel.Flex => "Flex",
        TipoCombustivel.Diesel => "Diesel",
        TipoCombustivel.Gnv => "GNV",
        TipoCombustivel.Eletrico => "Elétrico",
        TipoCombustivel.Hibrido => "Híbrido",
        _ => "Outro"
    };

    private static string RotuloTipoDespesa(TipoDespesa valor) => valor switch
    {
        TipoDespesa.Ipva => "IPVA",
        TipoDespesa.Licenciamento => "Licenciamento",
        TipoDespesa.SeguroVeiculo => "Seguro do veículo",
        TipoDespesa.Multa => "Multa",
        TipoDespesa.Pedagio => "Pedágio",
        TipoDespesa.Estacionamento => "Estacionamento",
        TipoDespesa.Lavagem => "Lavagem",
        TipoDespesa.Iptu => "IPTU",
        TipoDespesa.Condominio => "Condomínio",
        TipoDespesa.Agua => "Água",
        TipoDespesa.Energia => "Energia",
        TipoDespesa.Gas => "Gás",
        TipoDespesa.Internet => "Internet / TV",
        TipoDespesa.SeguroImovel => "Seguro do imóvel",
        TipoDespesa.Aluguel => "Aluguel",
        TipoDespesa.Reforma => "Reforma",
        TipoDespesa.Financiamento => "Financiamento",
        _ => "Outra"
    };

    private static string RotuloStatusDespesa(StatusDespesa valor) => valor switch
    {
        StatusDespesa.Pendente => "Em aberto",
        StatusDespesa.AVencer => "A vencer",
        StatusDespesa.Vencida => "Vencida",
        _ => "Paga"
    };

    private static string RotuloTipoImovel(TipoImovel valor) => valor switch
    {
        TipoImovel.Casa => "Casa",
        TipoImovel.Apartamento => "Apartamento",
        TipoImovel.Terreno => "Terreno",
        TipoImovel.SalaComercial => "Sala comercial",
        TipoImovel.Galpao => "Galpão",
        TipoImovel.Rural => "Rural",
        _ => "Outro"
    };

    private static string RotuloSituacaoImovel(SituacaoImovel valor) => valor switch
    {
        SituacaoImovel.MoradiaPropria => "Moradia própria",
        SituacaoImovel.AlugadoParaTerceiros => "Alugado",
        SituacaoImovel.Desocupado => "Desocupado",
        SituacaoImovel.Veraneio => "Veraneio",
        SituacaoImovel.UsoComercial => "Uso comercial",
        _ => "Em construção"
    };

    internal static string Km(int? quilometragem) =>
        quilometragem is null ? ValorAusente : $"{quilometragem.Value.ToString("N0", CulturaMonetaria)} km";

    internal static string Numero(decimal? valor, int casas = 2) =>
        valor is null ? ValorAusente : valor.Value.ToString("N" + casas, CulturaMonetaria);

    internal static string Inteiro(int? valor) => valor?.ToString(CulturaMonetaria) ?? ValorAusente;

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex RegexSeparadorMaiuscula();
}
