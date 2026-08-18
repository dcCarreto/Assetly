using System.Globalization;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Manutencao;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class GerarAlertasAutomaticos(
    IRepositorioBem repositorioBem,
    IRepositorioGarantia repositorioGarantia,
    IRepositorioManutencao repositorioManutencao,
    IRepositorioDocumento repositorioDocumento,
    IRepositorioAlerta repositorioAlerta)
{
    private const int DiasManutencaoProxima = 7;
    private const int TamanhoDescricaoAlerta = 120;
    private const string FormatoData = "dd/MM/yyyy";

    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem), "repositório de bens é obrigatório");
    private readonly IRepositorioGarantia _repositorioGarantia = repositorioGarantia ?? throw new ArgumentNullException(nameof(repositorioGarantia), "repositório de garantias é obrigatório");
    private readonly IRepositorioManutencao _repositorioManutencao = repositorioManutencao ?? throw new ArgumentNullException(nameof(repositorioManutencao), "repositório de manutenções é obrigatório");
    private readonly IRepositorioDocumento _repositorioDocumento = repositorioDocumento ?? throw new ArgumentNullException(nameof(repositorioDocumento), "repositório de documentos é obrigatório");
    private readonly IRepositorioAlerta _repositorioAlerta = repositorioAlerta ?? throw new ArgumentNullException(nameof(repositorioAlerta), "repositório de alertas é obrigatório");

    public IReadOnlyList<Alerta> Executar(DateOnly dataReferencia)
    {
        var gerados = new List<Alerta>();

        foreach (var garantia in _repositorioGarantia.Listar())
        {
            GerarAlertaGarantia(garantia, dataReferencia, gerados);
        }

        foreach (var manutencao in _repositorioManutencao.Listar())
        {
            GerarAlertaManutencao(manutencao, dataReferencia, gerados);
        }

        foreach (var bem in _repositorioBem.Listar().Where(DeveVerificarNotaFiscal))
        {
            GerarAlertaDocumentoAusente(bem, dataReferencia, gerados);
        }

        return gerados;
    }

    private void GerarAlertaGarantia(Garantia garantia, DateOnly dataReferencia, List<Alerta> gerados)
    {
        var status = garantia.StatusEm(dataReferencia);
        if (status == StatusGarantia.Vencida)
        {
            RegistrarSeNovo(
                garantia.BemId,
                TipoAlerta.GarantiaVencida,
                SeveridadeAlerta.Critico,
                $"Garantia vencida em {FormatarData(garantia.Periodo.TerminaEm)}.",
                dataReferencia,
                garantia.Periodo.TerminaEm,
                gerados);
        }
        else if (status == StatusGarantia.PertoDoVencimento)
        {
            RegistrarSeNovo(
                garantia.BemId,
                TipoAlerta.GarantiaVencendo,
                SeveridadeAlerta.Aviso,
                $"Garantia vence em {FormatarData(garantia.Periodo.TerminaEm)}.",
                dataReferencia,
                garantia.Periodo.TerminaEm,
                gerados);
        }
    }

    private void GerarAlertaManutencao(Manutencao manutencao, DateOnly dataReferencia, List<Alerta> gerados)
    {
        var status = manutencao.StatusEm(dataReferencia);
        if (status == StatusManutencao.Atrasada)
        {
            RegistrarSeNovo(
                manutencao.BemId,
                TipoAlerta.ManutencaoAtrasada,
                SeveridadeAlerta.Critico,
                $"Manutenção atrasada desde {FormatarData(manutencao.AgendadaPara)}: {DescricaoAlerta(manutencao)}.",
                dataReferencia,
                manutencao.AgendadaPara,
                gerados);
        }
        else if (status == StatusManutencao.Agendada && manutencao.AgendadaPara <= dataReferencia.AddDays(DiasManutencaoProxima))
        {
            RegistrarSeNovo(
                manutencao.BemId,
                TipoAlerta.ManutencaoProxima,
                SeveridadeAlerta.Informativo,
                $"Manutenção próxima em {FormatarData(manutencao.AgendadaPara)}: {DescricaoAlerta(manutencao)}.",
                dataReferencia,
                manutencao.AgendadaPara,
                gerados);
        }
    }

    private void GerarAlertaDocumentoAusente(Bem bem, DateOnly dataReferencia, List<Alerta> gerados)
    {
        var possuiNotaFiscal = _repositorioDocumento.ListarPorBem(bem.Id)
            .Any(documento => documento.Tipo == TipoDocumento.NotaFiscal && documento.Status == StatusDocumento.Disponivel);

        if (!possuiNotaFiscal)
        {
            RegistrarSeNovo(
                bem.Id,
                TipoAlerta.DocumentoAusente,
                SeveridadeAlerta.Aviso,
                "Nota fiscal ausente.",
                dataReferencia,
                null,
                gerados);
        }
    }

    private static bool DeveVerificarNotaFiscal(Bem bem) =>
        bem.Status is StatusBem.Ativo or StatusBem.EmManutencao;

    private static string DescricaoAlerta(Manutencao manutencao)
    {
        var descricao = manutencao.Descricao;
        return descricao.Length <= TamanhoDescricaoAlerta
            ? descricao
            : descricao[..(TamanhoDescricaoAlerta - 3)] + "...";
    }

    private void RegistrarSeNovo(
        BemId bemId,
        TipoAlerta tipo,
        SeveridadeAlerta severidade,
        string mensagem,
        DateOnly criadoEm,
        DateOnly? prazoEm,
        List<Alerta> gerados)
    {
        if (AlertaJaRegistrado(bemId, tipo, mensagem))
        {
            return;
        }

        var alerta = Alerta.Criar(bemId, tipo, severidade, mensagem, criadoEm, prazoEm);
        _repositorioAlerta.Salvar(alerta);
        gerados.Add(alerta);
    }

    private bool AlertaJaRegistrado(BemId bemId, TipoAlerta tipo, string mensagem) =>
        _repositorioAlerta.ListarPorBem(bemId).Any(alerta => alerta.Tipo == tipo && alerta.Mensagem == mensagem);

    private static string FormatarData(DateOnly data) => data.ToString(FormatoData, CultureInfo.InvariantCulture);
}
