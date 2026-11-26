using Assetly.Aplicacao.CasoDeUso;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Despesa;
using Assetly.Dominio.Garantia;
using Assetly.Infraestrutura.Seguranca;
using Shouldly;

namespace Assetly.Testes.Aplicacao;

public sealed class BuscaNotificacaoPinTests : IDisposable
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Now);
    private readonly string _raiz = Path.Combine(Path.GetTempPath(), $"assetly-pin-{Guid.NewGuid()}");

    [Fact]
    public void BuscaAchaEmQualquerCadastroSemAcentoNemMaiuscula()
    {
        var fonte = NovosRecursosCasosUsoTests.FonteMemoria();
        var geladeira = Bem.Criar(new NomeBem("Geladeira Frost Free"), TipoBem.Eletrodomestico);
        geladeira.AtualizarInventario(new InventarioBem("Brastemp", null, "BRM-778", null, "Cozinha"));
        fonte.Bens.Salvar(geladeira);
        fonte.Garantias.Salvar(Garantia.Criar(geladeira.Id, TipoGarantia.Estendida, "Seguradora Ômega", Hoje, Hoje.AddYears(1)));
        fonte.Despesas.Salvar(Despesa.Registrar(geladeira.Id, TipoDespesa.Outra, "Conserto do motor", Hoje, Dinheiro.De(300m, "BRL")));

        var indice = new BuscaGeral(fonte).Indexar();

        indice.Buscar("GELADEIRA cozinha").Single().Area.ShouldBe(AreaBusca.Bem);
        indice.Buscar("brm-778").Single().Chave.ShouldBe(geladeira.Id);
        indice.Buscar("omega").Single().Area.ShouldBe(AreaBusca.Garantia);
        indice.Buscar("motor").Single().Area.ShouldBe(AreaBusca.Despesa);
        indice.Buscar("geladeira").Count.ShouldBe(3);
        indice.Buscar("   ").ShouldBeEmpty();
        indice.Buscar("inexistente").ShouldBeEmpty();
    }

    [Fact]
    public void NotificaCadaAlertaAbertoUmaVezSoEIgnoraOsJaVistos()
    {
        var bem = BemId.Novo();
        var critico = Alerta.Criar(bem, TipoAlerta.GarantiaVencida, SeveridadeAlerta.Critico, "garantia venceu", Hoje, Hoje);
        var aviso = Alerta.Criar(bem, TipoAlerta.ContaVencendo, SeveridadeAlerta.Aviso, "conta vence amanhã", Hoje, Hoje.AddDays(1));
        var informativo = Alerta.Criar(bem, TipoAlerta.ManutencaoProxima, SeveridadeAlerta.Informativo, "revisão em breve", Hoje, null);
        var ciente = Alerta.Criar(bem, TipoAlerta.SeguroVencendo, SeveridadeAlerta.Critico, "seguro", Hoje, null);
        ciente.TomarCiencia();
        var avisados = new HashSet<AlertaId>();

        var primeira = NotificacaoAlertas.Novos([critico, aviso, informativo, ciente], avisados, _ => "Carro");

        primeira.ShouldNotBeNull();
        primeira.Critico.ShouldBeTrue();
        primeira.Titulo.ShouldContain("2 alertas");
        primeira.Texto.ShouldContain("garantia venceu");
        NotificacaoAlertas.Novos([critico, aviso, informativo, ciente], avisados, _ => "Carro").ShouldBeNull();

        var novo = Alerta.Criar(bem, TipoAlerta.LicenciamentoVencendo, SeveridadeAlerta.Aviso, "licenciamento", Hoje, null);
        var segunda = NotificacaoAlertas.Novos([critico, novo], avisados, _ => "Carro")!;
        segunda.Titulo.ShouldBe("Assetly — Carro");
        segunda.Texto.ShouldBe("licenciamento");
    }

    [Fact]
    public void PinConfereSoComOValorCertoEPodeSerRemovido()
    {
        var bloqueio = new BloqueioPin(Path.Combine(_raiz, "bloqueio.json"));
        bloqueio.Ativo.ShouldBeFalse();
        bloqueio.Conferir("qualquer").ShouldBeTrue();
        Should.Throw<ArgumentException>(() => bloqueio.Definir("123", "123"));
        Should.Throw<ArgumentException>(() => bloqueio.Definir("1234", "4321"));

        bloqueio.Definir("1234", "1234");

        bloqueio.Ativo.ShouldBeTrue();
        bloqueio.Conferir("1234").ShouldBeTrue();
        bloqueio.Conferir("1235").ShouldBeFalse();
        bloqueio.Conferir("").ShouldBeFalse();
        File.ReadAllText(bloqueio.Arquivo).ShouldNotContain("1234");

        File.WriteAllText(bloqueio.Arquivo, "corrompido");
        bloqueio.Conferir("1234").ShouldBeFalse();

        bloqueio.Remover();
        bloqueio.Ativo.ShouldBeFalse();
    }

    public void Dispose()
    {
        if (Directory.Exists(_raiz))
        {
            Directory.Delete(_raiz, recursive: true);
        }
    }
}
