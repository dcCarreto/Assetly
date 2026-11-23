using Assetly.Dominio.Bem;
using Assetly.Dominio.Despesa;
using Assetly.Dominio.Imovel;
using Assetly.Dominio.Veiculo;

namespace Assetly.Aplicacao.Dados;

/// <summary>Cadastra a ficha de um veículo. Com <paramref name="BemExistente"/>, vincula a um bem já
/// cadastrado (do tipo veículo); sem ele, cria o bem junto, com nome e dados de compra.</summary>
public sealed record CadastrarVeiculoComando(
    BemId? BemExistente,
    string? Nome,
    DateOnly? CompradoEm,
    decimal? PrecoCompra,
    string? CodigoMoeda,
    DadosVeiculo Dados);

public sealed record EditarVeiculoComando(BemId BemId, DadosVeiculo Dados);

public sealed record RegistrarAbastecimentoComando(
    BemId BemId,
    DateOnly Data,
    int Quilometragem,
    decimal Litros,
    decimal Valor,
    string CodigoMoeda,
    TipoCombustivel Combustivel,
    bool TanqueCheio,
    string? Posto);

public sealed record EditarAbastecimentoComando(
    AbastecimentoId Id,
    DateOnly Data,
    int Quilometragem,
    decimal Litros,
    decimal Valor,
    string CodigoMoeda,
    TipoCombustivel Combustivel,
    bool TanqueCheio,
    string? Posto);

/// <summary>Cadastra a ficha de um imóvel (mesma regra de vínculo/criação do bem do veículo).</summary>
public sealed record CadastrarImovelComando(
    BemId? BemExistente,
    string? Nome,
    DateOnly? CompradoEm,
    decimal? PrecoCompra,
    string? CodigoMoeda,
    DadosImovel Dados);

public sealed record EditarImovelComando(BemId BemId, DadosImovel Dados);

/// <summary><paramref name="Repeticoes"/> &gt; 1 gera uma despesa por mês (parcelas de IPVA/IPTU,
/// condomínio...), com "(1/N)" no fim da descrição.</summary>
public sealed record RegistrarDespesaComando(
    BemId BemId,
    TipoDespesa Tipo,
    string? Descricao,
    DateOnly Vencimento,
    decimal Valor,
    string CodigoMoeda,
    int Repeticoes = 1);

public sealed record EditarDespesaComando(
    DespesaId Id,
    TipoDespesa Tipo,
    string? Descricao,
    DateOnly Vencimento,
    decimal Valor,
    string CodigoMoeda);
