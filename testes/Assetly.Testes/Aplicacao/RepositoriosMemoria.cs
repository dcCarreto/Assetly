using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Despesa;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Imovel;
using Assetly.Dominio.Manutencao;
using Assetly.Dominio.Saude;
using Assetly.Dominio.Veiculo;

namespace Assetly.Testes.Aplicacao;

internal sealed class RepositorioBemMemoria : IRepositorioBem
{
    private readonly Dictionary<BemId, Bem> _dados = new();

    public Bem? BuscarPorId(BemId id) => _dados.GetValueOrDefault(id);

    public IReadOnlyList<Bem> Listar() => _dados.Values.ToList();

    public void Salvar(Bem bem) => _dados[bem.Id] = bem;

    public void Excluir(BemId id) => _dados.Remove(id);
}

internal sealed class RepositorioGarantiaMemoria : IRepositorioGarantia
{
    private readonly Dictionary<GarantiaId, Garantia> _dados = new();

    public Garantia? BuscarPorId(GarantiaId id) => _dados.GetValueOrDefault(id);

    public IReadOnlyList<Garantia> Listar() => _dados.Values.ToList();

    public IReadOnlyList<Garantia> ListarPorBem(BemId bemId) =>
        _dados.Values.Where(g => g.BemId.Equals(bemId)).ToList();

    public void Salvar(Garantia garantia) => _dados[garantia.Id] = garantia;

    public void Excluir(GarantiaId id) => _dados.Remove(id);
}

internal sealed class RepositorioManutencaoMemoria : IRepositorioManutencao
{
    private readonly Dictionary<ManutencaoId, Manutencao> _dados = new();

    public Manutencao? BuscarPorId(ManutencaoId id) => _dados.GetValueOrDefault(id);

    public IReadOnlyList<Manutencao> Listar() => _dados.Values.ToList();

    public IReadOnlyList<Manutencao> ListarPorBem(BemId bemId) =>
        _dados.Values.Where(m => m.BemId.Equals(bemId)).ToList();

    public void Salvar(Manutencao manutencao) => _dados[manutencao.Id] = manutencao;

    public void Excluir(ManutencaoId id) => _dados.Remove(id);
}

internal sealed class RepositorioDocumentoMemoria : IRepositorioDocumento
{
    private readonly Dictionary<DocumentoId, Documento> _dados = new();

    public Documento? BuscarPorId(DocumentoId id) => _dados.GetValueOrDefault(id);

    public IReadOnlyList<Documento> Listar() => _dados.Values.ToList();

    public IReadOnlyList<Documento> ListarPorBem(BemId bemId) =>
        _dados.Values.Where(d => d.BemId.Equals(bemId)).ToList();

    public void Salvar(Documento documento) => _dados[documento.Id] = documento;

    public void Excluir(DocumentoId id) => _dados.Remove(id);
}

internal sealed class RepositorioAlertaMemoria : IRepositorioAlerta
{
    private readonly Dictionary<AlertaId, Alerta> _dados = new();

    public Alerta? BuscarPorId(AlertaId id) => _dados.GetValueOrDefault(id);

    public IReadOnlyList<Alerta> Listar() => _dados.Values.ToList();

    public IReadOnlyList<Alerta> ListarPorBem(BemId bemId) =>
        _dados.Values.Where(a => a.BemId.Equals(bemId)).ToList();

    public void Salvar(Alerta alerta) => _dados[alerta.Id] = alerta;

    public void Excluir(AlertaId id) => _dados.Remove(id);
}

internal sealed class RepositorioSaudeMemoria : IRepositorioSaude
{
    private readonly Dictionary<SaudeId, Saude> _dados = new();

    public Saude? BuscarPorId(SaudeId id) => _dados.GetValueOrDefault(id);

    public IReadOnlyList<Saude> Listar() => _dados.Values.ToList();

    public IReadOnlyList<Saude> ListarPorBem(BemId bemId) =>
        _dados.Values.Where(s => s.BemId.Equals(bemId)).ToList();

    public void Salvar(Saude saude) => _dados[saude.Id] = saude;

    public void Excluir(SaudeId id) => _dados.Remove(id);
}

internal sealed class RepositorioGastoSaudeMemoria : IRepositorioGastoSaude
{
    private readonly Dictionary<GastoSaudeId, GastoSaude> _dados = new();

    public GastoSaude? BuscarPorId(GastoSaudeId id) => _dados.GetValueOrDefault(id);

    public IReadOnlyList<GastoSaude> Listar() => _dados.Values.ToList();

    public IReadOnlyList<GastoSaude> ListarPorBem(BemId bemId) =>
        _dados.Values.Where(g => g.BemId.Equals(bemId)).ToList();

    public void Salvar(GastoSaude gastoSaude) => _dados[gastoSaude.Id] = gastoSaude;

    public void Excluir(GastoSaudeId id) => _dados.Remove(id);
}

internal sealed class RepositorioVeiculoMemoria : IRepositorioVeiculo
{
    private readonly Dictionary<BemId, Veiculo> _dados = new();

    public Veiculo? BuscarPorBem(BemId bemId) => _dados.GetValueOrDefault(bemId);

    public IReadOnlyList<Veiculo> Listar() => _dados.Values.ToList();

    public void Salvar(Veiculo veiculo) => _dados[veiculo.BemId] = veiculo;

    public void Excluir(BemId bemId) => _dados.Remove(bemId);
}

internal sealed class RepositorioImovelMemoria : IRepositorioImovel
{
    private readonly Dictionary<BemId, Imovel> _dados = new();

    public Imovel? BuscarPorBem(BemId bemId) => _dados.GetValueOrDefault(bemId);

    public IReadOnlyList<Imovel> Listar() => _dados.Values.ToList();

    public void Salvar(Imovel imovel) => _dados[imovel.BemId] = imovel;

    public void Excluir(BemId bemId) => _dados.Remove(bemId);
}

internal sealed class RepositorioAbastecimentoMemoria : IRepositorioAbastecimento
{
    private readonly Dictionary<AbastecimentoId, Abastecimento> _dados = new();

    public Abastecimento? BuscarPorId(AbastecimentoId id) => _dados.GetValueOrDefault(id);

    public IReadOnlyList<Abastecimento> Listar() => _dados.Values.ToList();

    public IReadOnlyList<Abastecimento> ListarPorBem(BemId bemId) => _dados.Values.Where(a => a.BemId.Equals(bemId)).ToList();

    public void Salvar(Abastecimento abastecimento) => _dados[abastecimento.Id] = abastecimento;

    public void Excluir(AbastecimentoId id) => _dados.Remove(id);
}

internal sealed class RepositorioDespesaMemoria : IRepositorioDespesa
{
    private readonly Dictionary<DespesaId, Despesa> _dados = new();

    public Despesa? BuscarPorId(DespesaId id) => _dados.GetValueOrDefault(id);

    public IReadOnlyList<Despesa> Listar() => _dados.Values.OrderBy(d => d.Vencimento).ToList();

    public IReadOnlyList<Despesa> ListarPorBem(BemId bemId) => _dados.Values.Where(d => d.BemId.Equals(bemId)).OrderBy(d => d.Vencimento).ToList();

    public void Salvar(Despesa despesa) => _dados[despesa.Id] = despesa;

    public void Excluir(DespesaId id) => _dados.Remove(id);
}
