namespace Assetly.Aplicacao.Repositorio;

/// <summary>Todos os repositórios de dados do usuário juntos — para operações que percorrem a base
/// inteira (exportação, impressão digital da sincronização).</summary>
public sealed record FonteDados(
    IRepositorioBem Bens,
    IRepositorioGarantia Garantias,
    IRepositorioManutencao Manutencoes,
    IRepositorioDocumento Documentos,
    IRepositorioAlerta Alertas,
    IRepositorioSaude Convenios,
    IRepositorioGastoSaude GastosSaude,
    IRepositorioVeiculo Veiculos,
    IRepositorioAbastecimento Abastecimentos,
    IRepositorioImovel Imoveis,
    IRepositorioDespesa Despesas);
