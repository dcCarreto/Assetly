using Assetly.Aplicacao.CasoDeUso;
using Assetly.Aplicacao.Dados;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Saude;
using Assetly.Infraestrutura.Banco;
using Assetly.Infraestrutura.Repositorio;
using Shouldly;

namespace Assetly.Testes.Aplicacao;

public class IrpfComprovantesTests
{
    // Números gerados só para teste (dígitos verificadores válidos).
    private const string CpfValido = "529.982.247-25";
    private const string CnpjValido = "11.222.333/0001-81";

    [Theory]
    [InlineData(CpfValido, "52998224725")]
    [InlineData(CnpjValido, "11222333000181")]
    [InlineData("", "")]
    public void CpfCnpjValidoFicaSoComOsDigitos(string texto, string esperado) =>
        CpfCnpj.Normalizar(texto, "documento").ShouldBe(esperado);

    [Theory]
    [InlineData("529.982.247-26")]
    [InlineData("111.111.111-11")]
    [InlineData("11.222.333/0001-80")]
    [InlineData("12345")]
    public void CpfCnpjComDigitoErradoERecusado(string texto) =>
        Should.Throw<ArgumentException>(() => CpfCnpj.Normalizar(texto, "documento"));

    [Fact]
    public void RelatorioSomaDedutiveisPorPrestadorEPacienteEIgnoraFarmacia()
    {
        var bens = new RepositorioBemMemoria();
        var gastos = new RepositorioGastoSaudeMemoria();
        var ana = Bem.Criar(new NomeBem("Ana"), TipoBem.Pessoa);
        bens.Salvar(ana);
        void Gasto(TipoGastoSaude tipo, decimal valor, DateOnly data, string prestador = "", string documento = "") =>
            gastos.Salvar(GastoSaude.Restaurar(GastoSaudeId.Novo(), ana.Id, tipo, "gasto", data, Dinheiro.De(valor, "BRL"), prestador, documento));
        Gasto(TipoGastoSaude.Consulta, 300m, new DateOnly(2025, 3, 1), "Dra. Silva", CpfValido);
        Gasto(TipoGastoSaude.Consulta, 300m, new DateOnly(2025, 6, 1), "Dra. Silva", CpfValido);
        Gasto(TipoGastoSaude.Exame, 150m, new DateOnly(2025, 7, 1), "Laboratório", "");
        Gasto(TipoGastoSaude.Farmacia, 80m, new DateOnly(2025, 7, 2));
        Gasto(TipoGastoSaude.Consulta, 999m, new DateOnly(2024, 12, 31), "Dra. Silva", CpfValido);

        var relatorio = new GerarRelatorioIrpf(bens, gastos).Executar(2025);

        relatorio.Pagamentos.Count.ShouldBe(2);
        relatorio.Pagamentos.ShouldContain(p => p.Prestador == "Dra. Silva" && p.Quantidade == 2 && p.Total == 600m && p.Paciente == "Ana");
        relatorio.TotalDedutivel.ShouldBe(750m);
        relatorio.GastosSemDocumento.ShouldBe(1);
        relatorio.NaoDedutiveis.ShouldBe(80m);
        relatorio.ParaTexto().ShouldContain("529.982.247-25");
    }

    [Fact]
    public void RelatorioListaBensPeloValorDeAquisicaoNoFimDeCadaAno()
    {
        var bens = new RepositorioBemMemoria();
        var carro = Bem.Criar(new NomeBem("Carro"), TipoBem.Veiculo, new DateOnly(2023, 5, 10), Dinheiro.De(80_000m, "BRL"));
        carro.InformarValorAtual(Dinheiro.De(60_000m, "BRL"), new DateOnly(2025, 1, 1));
        var celular = Bem.Criar(new NomeBem("Celular"), TipoBem.Eletronico, new DateOnly(2025, 2, 1), Dinheiro.De(3_000m, "BRL"));
        var futuro = Bem.Criar(new NomeBem("Comprado depois"), TipoBem.Movel, new DateOnly(2026, 1, 5), Dinheiro.De(9_000m, "BRL"));
        var descartado = Bem.Criar(new NomeBem("Velho"), TipoBem.Eletronico, new DateOnly(2020, 1, 1), Dinheiro.De(7_000m, "BRL"));
        descartado.Descartar();
        foreach (var bem in new[] { carro, celular, futuro, descartado })
        {
            bens.Salvar(bem);
        }

        var relatorio = new GerarRelatorioIrpf(bens, new RepositorioGastoSaudeMemoria()).Executar(2025);

        relatorio.Bens.Count.ShouldBe(2);
        var itemCarro = relatorio.Bens.Single(b => b.Tipo == TipoBem.Veiculo);
        itemCarro.SituacaoAnoAnterior.ShouldBe(80_000m);
        itemCarro.SituacaoAno.ShouldBe(80_000m);
        itemCarro.Obrigatorio.ShouldBeTrue();
        var itemCelular = relatorio.Bens.Single(b => b.Tipo == TipoBem.Eletronico);
        itemCelular.SituacaoAnoAnterior.ShouldBeNull();
        itemCelular.Obrigatorio.ShouldBeFalse();
    }

    [Fact]
    public void BancoGuardaPrestadorDoGastoEVinculoDoComprovante()
    {
        var banco = new BancoDadosAssetly(new FabricaConexaoSqlite(Path.Combine(Path.GetTempPath(), $"assetly-{Guid.NewGuid()}", "assetly.sqlite3")));
        banco.Inicializar();
        var bens = new RepositorioBemSqlite(banco.FabricaConexao);
        var carro = Bem.Criar(new NomeBem("Carro"), TipoBem.Veiculo);
        bens.Salvar(carro);

        var gastos = new RepositorioGastoSaudeSqlite(banco.FabricaConexao);
        var gasto = new RegistrarGastoSaude(bens, gastos).Executar(new RegistrarGastoSaudeComando(carro.Id, TipoGastoSaude.Consulta, "Consulta",
            new DateOnly(2025, 1, 2), 200m, "BRL", "Dr. Souza", CpfValido));
        var lido = gastos.BuscarPorId(gasto.Id)!;
        lido.Prestador.ShouldBe("Dr. Souza");
        lido.DocumentoPrestador.ShouldBe("52998224725");

        var documentos = new RepositorioDocumentoSqlite(banco.FabricaConexao);
        var vinculo = new VinculoDocumento(TipoVinculoDocumento.Manutencao, Guid.NewGuid());
        var documento = new RegistrarDocumento(bens, documentos).Executar(new RegistrarDocumentoComando(carro.Id, TipoDocumento.Recibo, "Comprovante",
            @"C:\recibo.pdf", new DateOnly(2025, 1, 2), Vinculo: vinculo));
        documentos.BuscarPorId(documento.Id)!.Vinculo.ShouldBe(vinculo);
    }
}
