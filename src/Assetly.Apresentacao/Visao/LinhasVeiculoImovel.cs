using Assetly.Dominio.Bem;
using Assetly.Dominio.Despesa;
using Assetly.Dominio.Imovel;
using Assetly.Dominio.Veiculo;

namespace Assetly.Apresentacao.Visao;

internal sealed record VeiculoLinha(
    BemId Id, string Nome, string Placa, string MarcaModelo, string Ano, string Quilometragem, string Situacao, string SituacaoClasse,
    string LinhaClasse, string Licenciamento, string Seguro, string Revisao, string Consumo, Veiculo Origem)
{
    public int QuilometragemOrdenacao => Origem.Quilometragem;
    public DateOnly? LicenciamentoOrdenacao => Origem.VencimentoLicenciamento;
    public DateOnly? SeguroOrdenacao => Origem.VencimentoSeguro;
}

internal sealed record AbastecimentoLinha(
    Dominio.Veiculo.AbastecimentoId Id, string Veiculo, string Data, string Quilometragem, string Litros, string Valor, string PrecoLitro,
    string Combustivel, string TanqueCheio, string Consumo, string Posto, Abastecimento Origem)
{
    public DateOnly DataOrdenacao => Origem.Data;
    public int QuilometragemOrdenacao => Origem.Quilometragem;
    public decimal LitrosOrdenacao => Origem.Litros;
    public decimal ValorOrdenacao => Origem.Valor.Valor;
    public decimal PrecoLitroOrdenacao => Origem.PrecoPorLitro;
}

internal sealed record DespesaLinha(
    DespesaId Id, string Bem, string Tipo, string Descricao, string Vencimento, string Valor, string Status, string StatusClasse,
    string LinhaClasse, string PagaEm, Despesa Origem)
{
    public DateOnly VencimentoOrdenacao => Origem.Vencimento;
    public decimal ValorOrdenacao => Origem.Valor.Valor;
    public DateOnly? PagaEmOrdenacao => Origem.PagaEm;
}

internal sealed record ImovelLinha(
    BemId Id, string Nome, string Tipo, string Situacao, string SituacaoClasse, string LinhaClasse, string Endereco, string Area,
    string Condominio, string Aluguel, string Seguro, Imovel Origem)
{
    public decimal? AreaOrdenacao => Origem.AreaM2;
    public decimal? CondominioOrdenacao => Origem.ValorCondominio?.Valor;
    public decimal? AluguelOrdenacao => Origem.ValorAluguel?.Valor;
    public DateOnly? SeguroOrdenacao => Origem.VencimentoSeguro;
}
