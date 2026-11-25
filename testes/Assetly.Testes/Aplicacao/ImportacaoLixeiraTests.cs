using Assetly.Aplicacao.CasoDeUso;
using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Despesa;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Saude;
using Assetly.Dominio.Veiculo;
using Assetly.Infraestrutura.Armazenamento;
using Assetly.Infraestrutura.Banco;
using Assetly.Infraestrutura.Repositorio;
using Assetly.Testes.Dominio;
using Microsoft.Data.Sqlite;
using Shouldly;

namespace Assetly.Testes.Aplicacao;

public sealed class ImportacaoLixeiraTests : IDisposable
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Now);
    private readonly string _raiz = Path.Combine(Path.GetTempPath(), $"assetly-importacao-{Guid.NewGuid()}");

    [Fact]
    public void JsonExportadoVoltaIgualEImportarDeNovoNaoDuplica()
    {
        var origem = NovosRecursosCasosUsoTests.FonteMemoria();
        Popular(origem);
        var json = new ExportarDados(origem).Executar();

        var destino = NovosRecursosCasosUsoTests.FonteMemoria();
        var importar = new ImportarDados(destino, new ArmazenamentoQueAchaTudo());
        var resultado = importar.Executar(json);
        importar.Executar(json);

        resultado.Bens.ShouldBe(2);
        resultado.Ignorados.ShouldBe(0);
        new ExportarDados(destino).ImpressaoDigital().ShouldBe(new ExportarDados(origem).ImpressaoDigital());
        destino.Despesas.Listar().Single().Recorrente.ShouldBeTrue();
        destino.Bens.Listar().Single(b => b.Tipo == TipoBem.Eletronico).ResponsavelId.ShouldNotBeNull();
    }

    [Fact]
    public void JsonComRegistroInvalidoNaoImportaNada()
    {
        var destino = NovosRecursosCasosUsoTests.FonteMemoria();
        var json = """{"aplicativo":"Assetly","dados":{"bens":[{"id":"11111111-1111-1111-1111-111111111111","nome":"Ok","tipo":"Outro","status":"Ativo"},{"id":"22222222-2222-2222-2222-222222222222","nome":"","tipo":"Outro","status":"Ativo"}]}}""";

        Should.Throw<ArgumentException>(() => new ImportarDados(destino, new ArmazenamentoQueAchaTudo()).Executar(json)).Message.ShouldContain("registro 2");
        destino.Bens.Listar().ShouldBeEmpty();
        Should.Throw<ArgumentException>(() => new ImportarDados(destino, new ArmazenamentoQueAchaTudo()).Executar("[1,2]"));
    }

    [Fact]
    public void CsvCadastraBensReconhecendoAsColunasEPulaRepetidos()
    {
        var bens = new RepositorioBemMemoria();
        var csv = "Nome;Tipo;Data de compra;Preço;Número de série;Localização;Observações\n" +
                  "Geladeira;Eletrodoméstico;10/02/2024;\"3.499,90\";SN-1;Cozinha;\"inox; 2 portas\"\n" +
                  "Furadeira;Ferramenta;;;;;\n";

        var primeira = new ImportarBensCsv(bens).Executar(csv);
        var segunda = new ImportarBensCsv(bens).Executar(csv);

        primeira.Criados.ShouldBe(2);
        segunda.Criados.ShouldBe(0);
        segunda.JaCadastrados.ShouldBe(2);
        var geladeira = bens.Listar().Single(b => b.Nome.Valor == "Geladeira");
        geladeira.Tipo.ShouldBe(TipoBem.Eletrodomestico);
        geladeira.CompradoEm.ShouldBe(new DateOnly(2024, 2, 10));
        geladeira.PrecoCompra.ShouldBe(Dinheiro.De(3_499.90m, "BRL"));
        geladeira.Inventario.NumeroSerie.ShouldBe("SN-1");
        geladeira.Inventario.Localizacao.ShouldBe("Cozinha");
        geladeira.Observacoes.ShouldBe("inox; 2 portas");
    }

    [Fact]
    public void CsvComLinhaInvalidaNaoImportaNada()
    {
        var bens = new RepositorioBemMemoria();
        var csv = "nome,tipo,compra\nTV,Eletrônico,01/01/2020\nNave,Espaçonave,\nRádio,Eletrônico,99/99/2020\n";

        var erro = Should.Throw<ArgumentException>(() => new ImportarBensCsv(bens).Executar(csv));

        erro.Message.ShouldContain("linha 3");
        erro.Message.ShouldContain("linha 4");
        bens.Listar().ShouldBeEmpty();
        Should.Throw<ArgumentException>(() => new ImportarBensCsv(bens).Executar("tipo;preço\nOutro;10"));
    }

    [Fact]
    public void BemExcluidoVaiParaALixeiraEVoltaComTudoEOsArquivos()
    {
        var (fonte, lixeira, armazenamento) = Sqlite();
        var bem = Bem.Criar(new NomeBem("Notebook"), TipoBem.Eletronico);
        fonte.Bens.Salvar(bem);
        fonte.Garantias.Salvar(Garantia.Criar(bem.Id, TipoGarantia.Fabricante, "Fabricante", Hoje.AddYears(-1), Hoje.AddYears(1)));
        var original = Path.Combine(_raiz, "nota.pdf");
        Directory.CreateDirectory(_raiz);
        File.WriteAllText(original, "nota");
        var documento = new RegistrarDocumento(fonte.Bens, fonte.Documentos, armazenamento)
            .Executar(new RegistrarDocumentoComando(bem.Id, TipoDocumento.NotaFiscal, "Nota", original, Hoje));
        var excluir = new ExcluirBem(fonte.Bens, fonte.Documentos, armazenamento, lixeira);

        excluir.Executar(bem.Id);

        fonte.Bens.Listar().ShouldBeEmpty();
        fonte.Garantias.Listar().ShouldBeEmpty();
        File.Exists(documento.CaminhoLocal.Valor).ShouldBeTrue();
        var item = lixeira.Listar().Single();
        item.Descricao.ShouldBe("Notebook");

        lixeira.Restaurar(item.Id);

        fonte.Bens.Listar().Single().Id.ShouldBe(bem.Id);
        fonte.Garantias.Listar().Count.ShouldBe(1);
        fonte.Documentos.Listar().Single().Status.ShouldBe(StatusDocumento.Disponivel);
        lixeira.Listar().ShouldBeEmpty();

        excluir.Executar(bem.Id);
        lixeira.ExcluirDefinitivamente(lixeira.Listar().Single().Id);
        File.Exists(documento.CaminhoLocal.Valor).ShouldBeFalse();
    }

    [Fact]
    public void LixeiraApagaSozinhaOsItensVencidos()
    {
        var (fonte, lixeira, armazenamento) = Sqlite();
        var bem = Bem.Criar(new NomeBem("Cadeira"), TipoBem.Movel);
        fonte.Bens.Salvar(bem);
        new ExcluirBem(fonte.Bens, fonte.Documentos, armazenamento, lixeira).Executar(bem.Id);

        lixeira.EsvaziarVencidos(Hoje.AddDays(LixeiraBens.DiasRetencao - 1)).ShouldBe(0);
        lixeira.EsvaziarVencidos(Hoje.AddDays(LixeiraBens.DiasRetencao)).ShouldBe(1);
        lixeira.Listar().ShouldBeEmpty();
    }

    private static void Popular(FonteDados fonte)
    {
        var ana = Bem.Criar(new NomeBem("Ana"), TipoBem.Pessoa);
        var carro = Bem.Criar(new NomeBem("Notebook"), TipoBem.Eletronico, Hoje.AddYears(-1), Dinheiro.De(5_000m, "BRL"));
        carro.DefinirResponsavel(ana.Id);
        carro.AtualizarInventario(new InventarioBem("Marca", "Modelo", "SN", "Loja", "Sala"));
        fonte.Bens.Salvar(ana);
        fonte.Bens.Salvar(carro);
        fonte.Garantias.Salvar(Garantia.Criar(carro.Id, TipoGarantia.Fabricante, "Fabricante", Hoje.AddYears(-1), Hoje.AddYears(1)));
        fonte.Documentos.Salvar(Documento.Registrar(carro.Id, TipoDocumento.NotaFiscal, new NomeDocumento("Nota"), new CaminhoDocumentoLocal(@"C:\docs\nota.pdf"), Hoje));
        fonte.GastosSaude.Salvar(GastoSaude.Restaurar(GastoSaudeId.Novo(), ana.Id, TipoGastoSaude.Consulta, "Consulta", Hoje, Dinheiro.De(200m, "BRL"), "Dra.", "529.982.247-25"));
        fonte.Veiculos.Salvar(Veiculo.Criar(carro.Id, VeiculoImovelTests.Ficha()));
        fonte.Abastecimentos.Salvar(Abastecimento.Registrar(carro.Id, Hoje, 50_500, 40m, Dinheiro.De(240m, "BRL"), TipoCombustivel.Etanol, true, "Posto"));
        var conta = Despesa.Registrar(carro.Id, TipoDespesa.Internet, "Internet", Hoje, Dinheiro.De(100m, "BRL"));
        conta.DefinirRecorrencia(true);
        fonte.Despesas.Salvar(conta);
    }

    private (FonteDados Fonte, LixeiraBens Lixeira, IArmazenamentoDocumento Armazenamento) Sqlite()
    {
        var banco = new BancoDadosAssetly(new FabricaConexaoSqlite(Path.Combine(_raiz, "banco", "assetly.sqlite3")));
        banco.Inicializar();
        var f = banco.FabricaConexao;
        var fonte = new FonteDados(new RepositorioBemSqlite(f), new RepositorioGarantiaSqlite(f), new RepositorioManutencaoSqlite(f), new RepositorioDocumentoSqlite(f),
            new RepositorioAlertaSqlite(f), new RepositorioSaudeSqlite(f), new RepositorioGastoSaudeSqlite(f), new RepositorioVeiculoSqlite(f),
            new RepositorioAbastecimentoSqlite(f), new RepositorioImovelSqlite(f), new RepositorioDespesaSqlite(f));
        var armazenamento = new ArmazenamentoDocumentoLocal(Path.Combine(_raiz, "documentos"));
        var lixeira = new LixeiraBens(new RepositorioLixeiraSqlite(f), new ExportarDados(fonte), new ImportarDados(fonte, armazenamento), fonte.Documentos, armazenamento);
        return (fonte, lixeira, armazenamento);
    }

    private sealed class ArmazenamentoQueAchaTudo : IArmazenamentoDocumento
    {
        public string Armazenar(string arquivoOrigem) => arquivoOrigem;

        public string? Localizar(string caminhoRegistrado) => caminhoRegistrado;
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_raiz))
        {
            Directory.Delete(_raiz, recursive: true);
        }
    }
}
