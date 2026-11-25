using System.Globalization;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Manutencao;
using Assetly.Dominio.Saude;

namespace Assetly.Aplicacao.CasoDeUso;

/// <summary>
/// Motor de alertas automáticos. A cada execução:
/// <list type="number">
/// <item>calcula quais alertas a situação atual exige (garantias, manutenções, nota fiscal e convênios
/// de bens ativos ou em manutenção) e cria os que ainda não existem — sem duplicar, inclusive quando o
/// usuário já resolveu um alerta igual;</item>
/// <item>resolve sozinho os alertas automáticos ainda abertos cuja condição deixou de existir (garantia
/// renovada, manutenção concluída, nota fiscal anexada, bem arquivado...). Alertas manuais nunca são
/// tocados.</item>
/// </list>
/// </summary>
public sealed class GerarAlertasAutomaticos
{
    private const int DiasManutencaoProxima = 7;

    /// <summary>Por quantos dias um alerta automático resolvido (e sem situação atual) fica no histórico.</summary>
    public const int DiasRetencaoResolvidos = 90;
    private const int TamanhoDescricaoAlerta = 120;
    private const string FormatoData = "dd/MM/yyyy";

    private readonly IRepositorioBem _repositorioBem;
    private readonly IRepositorioGarantia _repositorioGarantia;
    private readonly IRepositorioManutencao _repositorioManutencao;
    private readonly IRepositorioDocumento _repositorioDocumento;
    private readonly IRepositorioAlerta _repositorioAlerta;
    private readonly IRepositorioSaude? _repositorioSaude;
    private readonly IRepositorioVeiculo? _repositorioVeiculo;
    private readonly IRepositorioImovel? _repositorioImovel;
    private readonly IRepositorioDespesa? _repositorioDespesa;

    public GerarAlertasAutomaticos(
        IRepositorioBem repositorioBem,
        IRepositorioGarantia repositorioGarantia,
        IRepositorioManutencao repositorioManutencao,
        IRepositorioDocumento repositorioDocumento,
        IRepositorioAlerta repositorioAlerta)
        : this(repositorioBem, repositorioGarantia, repositorioManutencao, repositorioDocumento, repositorioAlerta, null)
    {
    }

    public GerarAlertasAutomaticos(
        IRepositorioBem repositorioBem,
        IRepositorioGarantia repositorioGarantia,
        IRepositorioManutencao repositorioManutencao,
        IRepositorioDocumento repositorioDocumento,
        IRepositorioAlerta repositorioAlerta,
        IRepositorioSaude? repositorioSaude,
        IRepositorioVeiculo? repositorioVeiculo = null,
        IRepositorioImovel? repositorioImovel = null,
        IRepositorioDespesa? repositorioDespesa = null)
    {
        _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem), "repositório de bens é obrigatório");
        _repositorioGarantia = repositorioGarantia ?? throw new ArgumentNullException(nameof(repositorioGarantia), "repositório de garantias é obrigatório");
        _repositorioManutencao = repositorioManutencao ?? throw new ArgumentNullException(nameof(repositorioManutencao), "repositório de manutenções é obrigatório");
        _repositorioDocumento = repositorioDocumento ?? throw new ArgumentNullException(nameof(repositorioDocumento), "repositório de documentos é obrigatório");
        _repositorioAlerta = repositorioAlerta ?? throw new ArgumentNullException(nameof(repositorioAlerta), "repositório de alertas é obrigatório");
        _repositorioSaude = repositorioSaude;
        _repositorioVeiculo = repositorioVeiculo;
        _repositorioImovel = repositorioImovel;
        _repositorioDespesa = repositorioDespesa;
    }

    public IReadOnlyList<Alerta> Executar(DateOnly dataReferencia)
    {
        var execucao = new Execucao(_repositorioAlerta, dataReferencia);
        var monitorados = _repositorioBem.Listar().Where(DeveMonitorar).ToList();
        var bensMonitorados = monitorados.Select(bem => bem.Id).ToHashSet();
        var bensQueExigemNotaFiscal = monitorados.Where(ExigeNotaFiscal).Select(bem => bem.Id).ToList();

        var garantias = _repositorioGarantia.Listar().Where(g => bensMonitorados.Contains(g.BemId)).ToList();
        foreach (var garantia in garantias)
        {
            var outras = garantias.Where(g => g.BemId.Equals(garantia.BemId) && !g.Id.Equals(garantia.Id))
                .Select(g => (g.Periodo.IniciaEm, g.Periodo.TerminaEm));
            if (!CobertoPorOutroPeriodo(garantia.Periodo.TerminaEm, dataReferencia, outras))
            {
                GerarAlertaGarantia(garantia, dataReferencia, execucao);
            }
        }

        foreach (var manutencao in _repositorioManutencao.Listar().Where(m => bensMonitorados.Contains(m.BemId)))
        {
            GerarAlertaManutencao(manutencao, dataReferencia, execucao);
        }

        var bensComNotaFiscal = _repositorioDocumento.Listar()
            .Where(documento => documento.Tipo == TipoDocumento.NotaFiscal && documento.Status == StatusDocumento.Disponivel)
            .Select(documento => documento.BemId)
            .ToHashSet();
        foreach (var bemId in bensQueExigemNotaFiscal.Where(id => !bensComNotaFiscal.Contains(id)))
        {
            execucao.Exigir(bemId, TipoAlerta.DocumentoAusente, SeveridadeAlerta.Aviso, "Nota fiscal ausente.", null);
        }

        if (_repositorioSaude is not null)
        {
            var convenios = _repositorioSaude.Listar().Where(s => bensMonitorados.Contains(s.BemId)).ToList();
            foreach (var saude in convenios)
            {
                var renovacoes = convenios.Where(s => s.BemId.Equals(saude.BemId) && s.Tipo == saude.Tipo && !s.Id.Equals(saude.Id)
                        && s.StatusEm(dataReferencia) != StatusSaude.Cancelada)
                    .Select(s => (s.Periodo.IniciaEm, s.Periodo.TerminaEm));
                if (!CobertoPorOutroPeriodo(saude.Periodo.TerminaEm, dataReferencia, renovacoes))
                {
                    GerarAlertaSaude(saude, dataReferencia, execucao);
                }
            }
        }

        if (_repositorioVeiculo is not null)
        {
            foreach (var veiculo in _repositorioVeiculo.Listar().Where(v => bensMonitorados.Contains(v.BemId)))
            {
                AlertasVeiculoImovel.GerarParaVeiculo(veiculo, dataReferencia, execucao.Exigir);
            }
        }

        if (_repositorioImovel is not null)
        {
            foreach (var imovel in _repositorioImovel.Listar().Where(i => bensMonitorados.Contains(i.BemId)))
            {
                AlertasVeiculoImovel.GerarParaImovel(imovel, dataReferencia, execucao.Exigir);
            }
        }

        // Contas continuam devidas mesmo depois que o bem é vendido, arquivado ou descartado.
        if (_repositorioDespesa is not null)
        {
            foreach (var despesa in _repositorioDespesa.Listar())
            {
                AlertasVeiculoImovel.GerarParaDespesa(despesa, dataReferencia, execucao.Exigir);
            }
        }

        execucao.ResolverAlertasSemCondicao();
        execucao.Gravar();
        return execucao.Gerados;
    }

    /// <summary>Verdadeiro quando outro registro do mesmo bem (garantia estendida, convênio renovado) continua
    /// a cobertura depois de <paramref name="termino"/>: ele termina depois e já começou — ou começa até o
    /// dia seguinte ao término, sem deixar o bem descoberto. Só o último período da sequência gera alerta.</summary>
    private static bool CobertoPorOutroPeriodo(DateOnly termino, DateOnly dataReferencia, IEnumerable<(DateOnly Inicio, DateOnly Fim)> outros)
    {
        var limiteInicio = termino.AddDays(1) > dataReferencia ? termino.AddDays(1) : dataReferencia;
        return outros.Any(outro => outro.Fim > termino && outro.Inicio <= limiteInicio);
    }

    private static void GerarAlertaGarantia(Garantia garantia, DateOnly dataReferencia, Execucao execucao)
    {
        var status = garantia.StatusEm(dataReferencia);
        var termino = garantia.Periodo.TerminaEm;
        if (status == StatusGarantia.Vencida)
        {
            // Garantia vencida não exige ação imediata (o bem continua funcionando): aviso, não crítico.
            execucao.Exigir(garantia.BemId, TipoAlerta.GarantiaVencida, SeveridadeAlerta.Aviso,
                $"Garantia vencida em {FormatarData(termino)}.", termino, garantia.Id.Valor);
        }
        else if (status == StatusGarantia.PertoDoVencimento)
        {
            execucao.Exigir(garantia.BemId, TipoAlerta.GarantiaVencendo, SeveridadeAlerta.Aviso,
                $"Garantia vence em {FormatarData(termino)}.", termino, garantia.Id.Valor);
        }
    }

    private static void GerarAlertaManutencao(Manutencao manutencao, DateOnly dataReferencia, Execucao execucao)
    {
        var status = manutencao.StatusEm(dataReferencia);
        if (status == StatusManutencao.Atrasada)
        {
            execucao.Exigir(manutencao.BemId, TipoAlerta.ManutencaoAtrasada, SeveridadeAlerta.Critico,
                $"Manutenção atrasada desde {FormatarData(manutencao.AgendadaPara)}: {DescricaoAlerta(manutencao)}.", manutencao.AgendadaPara, manutencao.Id.Valor);
        }
        else if (status == StatusManutencao.Agendada && manutencao.AgendadaPara <= dataReferencia.AddDays(DiasManutencaoProxima))
        {
            execucao.Exigir(manutencao.BemId, TipoAlerta.ManutencaoProxima, SeveridadeAlerta.Informativo,
                $"Manutenção próxima em {FormatarData(manutencao.AgendadaPara)}: {DescricaoAlerta(manutencao)}.", manutencao.AgendadaPara, manutencao.Id.Valor);
        }
    }

    private static void GerarAlertaSaude(Saude saude, DateOnly dataReferencia, Execucao execucao)
    {
        var status = saude.StatusEm(dataReferencia);
        var termino = saude.Periodo.TerminaEm;
        var nome = string.IsNullOrWhiteSpace(saude.Fornecedor) ? "Convênio" : $"Convênio {saude.Fornecedor}";
        if (status == StatusSaude.Vencida)
        {
            execucao.Exigir(saude.BemId, TipoAlerta.ConvenioVencido, SeveridadeAlerta.Critico,
                $"{nome} vencido em {FormatarData(termino)}.", termino, saude.Id.Valor);
        }
        else if (status == StatusSaude.PertoDoVencimento)
        {
            execucao.Exigir(saude.BemId, TipoAlerta.ConvenioVencendo, SeveridadeAlerta.Aviso,
                $"{nome} vence em {FormatarData(termino)}.", termino, saude.Id.Valor);
        }
    }

    /// <summary>Pessoa (titular de convênio ou de gastos de saúde) não tem nota fiscal de compra; imóvel tem escritura, não nota fiscal.</summary>
    private static bool ExigeNotaFiscal(Bem bem) => bem.Tipo is not (TipoBem.Pessoa or TipoBem.Imovel);

    private static bool DeveMonitorar(Bem bem) =>
        bem.Status is StatusBem.Ativo or StatusBem.EmManutencao;

    private static string DescricaoAlerta(Manutencao manutencao)
    {
        var descricao = manutencao.Descricao;
        return descricao.Length <= TamanhoDescricaoAlerta
            ? descricao
            : descricao[..(TamanhoDescricaoAlerta - 3)] + "...";
    }

    private static string FormatarData(DateOnly data) => data.ToString(FormatoData, CultureInfo.InvariantCulture);

    /// <summary>Estado de uma execução: alertas existentes indexados, chaves exigidas pela situação
    /// atual, alertas criados nesta rodada e o que falta gravar (tudo numa transação, em <see cref="Gravar"/>).</summary>
    private sealed class Execucao
    {
        private readonly IRepositorioAlerta _repositorioAlerta;
        private readonly DateOnly _dataReferencia;
        private readonly IReadOnlyList<Alerta> _existentes;
        private readonly HashSet<(BemId, TipoAlerta, string, Guid?)> _chavesExistentes;
        private readonly HashSet<(BemId, TipoAlerta, string, Guid?)> _chavesExigidas = [];
        private readonly List<Alerta> _aGravar = [];

        internal List<Alerta> Gerados { get; } = [];

        internal Execucao(IRepositorioAlerta repositorioAlerta, DateOnly dataReferencia)
        {
            _repositorioAlerta = repositorioAlerta;
            _dataReferencia = dataReferencia;
            _existentes = repositorioAlerta.Listar();
            _chavesExistentes = _existentes.Select(Chave).ToHashSet();
        }

        internal void Exigir(BemId bemId, TipoAlerta tipo, SeveridadeAlerta severidade, string mensagem, DateOnly? prazoEm, Guid? origemId = null)
        {
            var chave = (bemId, tipo, mensagem, origemId);
            // Alerta gravado antes da V009 (sem origem) continua valendo para a mesma situação: não duplica.
            var legada = (bemId, tipo, mensagem, (Guid?)null);
            _chavesExigidas.Add(chave);
            _chavesExigidas.Add(legada);
            if (_chavesExistentes.Contains(legada) || !_chavesExistentes.Add(chave))
            {
                return;
            }

            var alerta = Alerta.CriarAutomatico(bemId, tipo, severidade, mensagem, _dataReferencia, prazoEm, origemId);
            _aGravar.Add(alerta);
            Gerados.Add(alerta);
        }

        internal void ResolverAlertasSemCondicao()
        {
            foreach (var alerta in _existentes.Where(a => a.Automatico && a.EstaAcionavel() && !_chavesExigidas.Contains(Chave(a))))
            {
                alerta.Resolver();
                _aGravar.Add(alerta);
            }
        }

        internal void Gravar()
        {
            if (_aGravar.Count > 0)
            {
                _repositorioAlerta.SalvarTodos(_aGravar);
            }

            var obsoletos = Obsoletos().Select(a => a.Id).ToList();
            if (obsoletos.Count > 0)
            {
                _repositorioAlerta.ExcluirTodos(obsoletos);
            }
        }

        /// <summary>Alertas automáticos já resolvidos antes desta rodada, cuja situação não existe mais e cujo prazo
        /// (ou criação) passou há mais de <see cref="DiasRetencaoResolvidos"/> dias: sem isso a tabela só cresce
        /// (cada conta mensal deixa dois alertas por mês). Os que ainda correspondem a uma situação atual ficam —
        /// são eles que impedem um alerta já resolvido pelo usuário de voltar. Alertas manuais nunca são apagados.</summary>
        private IEnumerable<Alerta> Obsoletos()
        {
            var limite = _dataReferencia.AddDays(-DiasRetencaoResolvidos);
            return _existentes.Where(a => a.Automatico
                && a.Status == StatusAlerta.Resolvido
                && !_aGravar.Contains(a)
                && !_chavesExigidas.Contains(Chave(a))
                && (a.PrazoEm ?? a.CriadoEm) < limite);
        }

        private static (BemId, TipoAlerta, string, Guid?) Chave(Alerta alerta) => (alerta.BemId, alerta.Tipo, alerta.Mensagem, alerta.OrigemId);
    }
}
