using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Despesa;
using Assetly.Dominio.Imovel;
using Assetly.Dominio.Veiculo;
using Shouldly;

namespace Assetly.Testes.Dominio;

public class VeiculoImovelTests
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Now);

    internal static DadosVeiculo Ficha(string? placa = "abc-1234", int km = 50000, string? renavam = null, string? chassi = null,
        int? anoFabricacao = 2020, int? anoModelo = 2021) =>
        new(placa, "Toyota", "Corolla", anoFabricacao, anoModelo, "Prata", renavam, chassi, TipoCombustivel.Flex, km,
            null, null, null, null, null, null);

    [Theory]
    [InlineData("abc-1234", "ABC1234", "ABC-1234")]
    [InlineData("bra2e19", "BRA2E19", "BRA2E19")]
    [InlineData(" BRA 2E19 ", "BRA2E19", "BRA2E19")]
    public void DeveNormalizarPlacasNosPadroesAntigoEMercosul(string digitada, string armazenada, string exibida)
    {
        var veiculo = Veiculo.Criar(BemId.Novo(), Ficha(placa: digitada));

        veiculo.Placa.ShouldBe(armazenada);
        veiculo.PlacaFormatada.ShouldBe(exibida);
    }

    [Theory]
    [InlineData("AB1234")]
    [InlineData("1234ABC")]
    [InlineData("ABCD123")]
    public void DeveRejeitarPlacaInvalida(string placa)
    {
        Should.Throw<ArgumentException>(() => Veiculo.Criar(BemId.Novo(), Ficha(placa: placa))).Message.ShouldContain("placa");
    }

    [Fact]
    public void DeveValidarRenavamChassiEAnos()
    {
        Should.Throw<ArgumentException>(() => Veiculo.Criar(BemId.Novo(), Ficha(renavam: "123")));
        Should.Throw<ArgumentException>(() => Veiculo.Criar(BemId.Novo(), Ficha(chassi: "9BWZZZ377VT00425I")));
        Should.Throw<ArgumentException>(() => Veiculo.Criar(BemId.Novo(), Ficha(anoFabricacao: 2020, anoModelo: 2023)));
        Should.Throw<ArgumentException>(() => Veiculo.Criar(BemId.Novo(), Ficha(km: -1)));

        var valido = Veiculo.Criar(BemId.Novo(), Ficha(renavam: "0012.345.678-9", chassi: "9bwzzz377vt004251"));
        valido.Renavam.ShouldBe("00123456789");
        valido.Chassi.ShouldBe("9BWZZZ377VT004251");
    }

    [Fact]
    public void QuilometragemSoAvancaNoRegistroDiarioMasPodeSerCorrigidaNaFicha()
    {
        var veiculo = Veiculo.Criar(BemId.Novo(), Ficha(km: 50000));

        veiculo.RegistrarQuilometragem(51000);
        Should.Throw<ArgumentException>(() => veiculo.RegistrarQuilometragem(50500));
        veiculo.AvancarQuilometragem(50800).ShouldBeFalse();
        veiculo.Atualizar(Ficha(km: 50500));

        veiculo.Quilometragem.ShouldBe(50500);
    }

    [Fact]
    public void DeveCalcularConsumoDeTanqueCheioATanqueCheio()
    {
        var bem = BemId.Novo();
        Abastecimento Abastecer(int km, decimal litros, bool cheio, int diasAtras) =>
            Abastecimento.Registrar(bem, Hoje.AddDays(-diasAtras), km, litros, Dinheiro.De(litros * 6m, "BRL"), TipoCombustivel.Gasolina, cheio, null);

        var abastecimentos = new[]
        {
            Abastecer(10000, 40, cheio: true, diasAtras: 30),   // referência
            Abastecer(10200, 15, cheio: false, diasAtras: 20),  // parcial: entra na soma do próximo trecho
            Abastecer(10500, 25, cheio: true, diasAtras: 10),   // 500 km / 40 L = 12,5
            Abastecer(11000, 50, cheio: true, diasAtras: 1)     // 500 km / 50 L = 10
        };

        var trechos = CalculoConsumo.Trechos(abastecimentos);

        trechos.Select(t => t.KmPorLitro).ShouldBe([12.5m, 10m]);
        CalculoConsumo.Medio(abastecimentos).ShouldBe(Math.Round(1000m / 90m, 2));
        CalculoConsumo.Medio([abastecimentos[0]]).ShouldBeNull();
        abastecimentos[0].PrecoPorLitro.ShouldBe(6m);
    }

    [Fact]
    public void AbastecimentoDeveRejeitarValoresInvalidos()
    {
        Should.Throw<ArgumentException>(() => Abastecimento.Registrar(BemId.Novo(), Hoje, 100, 0, Dinheiro.De(10, "BRL"), TipoCombustivel.Etanol, true, null));
        Should.Throw<ArgumentException>(() => Abastecimento.Registrar(BemId.Novo(), Hoje.AddDays(2), 100, 10, Dinheiro.De(10, "BRL"), TipoCombustivel.Etanol, true, null));
    }

    [Fact]
    public void DespesaDeveCalcularStatusEControlarPagamento()
    {
        var despesa = Despesa.Registrar(BemId.Novo(), TipoDespesa.Ipva, "IPVA", Hoje.AddDays(5), Dinheiro.De(1500, "BRL"));

        despesa.StatusEm(Hoje).ShouldBe(StatusDespesa.AVencer);
        despesa.StatusEm(Hoje.AddDays(-10)).ShouldBe(StatusDespesa.Pendente);
        despesa.StatusEm(Hoje.AddDays(6)).ShouldBe(StatusDespesa.Vencida);

        despesa.Pagar(Hoje);
        despesa.StatusEm(Hoje.AddDays(30)).ShouldBe(StatusDespesa.Paga);
        Should.Throw<InvalidOperationException>(() => despesa.Pagar(Hoje));

        despesa.DesfazerPagamento();
        despesa.PagaEm.ShouldBeNull();
        Should.Throw<InvalidOperationException>(() => despesa.DesfazerPagamento());
    }

    internal static DadosImovel FichaImovel(string? uf = "sp", string? cep = "01310-100", decimal? area = 72.5m,
        SituacaoImovel situacao = SituacaoImovel.MoradiaPropria, DateOnly? fimContrato = null, DateOnly? vencimentoSeguro = null) =>
        new(TipoImovel.Apartamento, situacao, "Av. Paulista", "1000", "apto 52", "Bela Vista", "São Paulo", uf, cep, area,
            2, 1, 1, "12.345", "000.111.222-3", Dinheiro.De(850, "BRL"), null, null, vencimentoSeguro,
            situacao == SituacaoImovel.AlugadoParaTerceiros ? "Inquilino Demo" : null,
            situacao == SituacaoImovel.AlugadoParaTerceiros ? Dinheiro.De(3200, "BRL") : null, fimContrato);

    [Fact]
    public void ImovelDeveNormalizarEnderecoEValidarCampos()
    {
        var imovel = Imovel.Criar(BemId.Novo(), FichaImovel());

        imovel.Uf.ShouldBe("SP");
        imovel.Cep.ShouldBe("01310100");
        imovel.CepFormatado.ShouldBe("01310-100");
        imovel.EnderecoCompleto.ShouldBe("Av. Paulista, 1000, apto 52 — Bela Vista, São Paulo/SP, CEP 01310-100");

        Should.Throw<ArgumentException>(() => Imovel.Criar(BemId.Novo(), FichaImovel(uf: "XX")));
        Should.Throw<ArgumentException>(() => Imovel.Criar(BemId.Novo(), FichaImovel(cep: "1234")));
        Should.Throw<ArgumentException>(() => Imovel.Criar(BemId.Novo(), FichaImovel(area: 0)));
    }
}
