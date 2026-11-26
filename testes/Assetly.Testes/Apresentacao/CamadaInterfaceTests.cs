using System.Windows.Controls;
using Assetly.Aplicacao.CasoDeUso;
using Assetly.Apresentacao.Visao;
using Assetly.Dominio.Compartilhado;
using Shouldly;

namespace Assetly.Testes.Apresentacao;

public class CamadaInterfaceTests
{
    private sealed record Linha(int Id, string Nome, bool Ativo);

    [Fact]
    public void FormatadorUsaOFormatoBrasileiroESeparaMoedas()
    {
        Formatador.Data(new DateOnly(2026, 3, 9)).ShouldBe("09/03/2026");
        Formatador.Data(null).ShouldBe("—");
        Formatador.Dinheiro(Dinheiro.De(1234.5m, "BRL")).ShouldBe("R$ 1.234,50");
        Formatador.Dinheiro(Dinheiro.De(99m, "USD")).ShouldBe("USD 99,00");
        Formatador.Total([Dinheiro.De(10m, "USD"), Dinheiro.De(1000m, "BRL"), null, Dinheiro.De(200m, "BRL")]).ShouldBe("R$ 1.200,00 + USD 10,00");
        Formatador.Total([]).ShouldBe("R$ 0,00");
        Formatador.TextoOuTraco("  ").ShouldBe("—");
    }

    [Theory]
    [InlineData("simples")]
    [InlineData("com; ponto e vírgula")]
    [InlineData("com \"aspas\" no meio")]
    [InlineData(" espaço nas pontas ")]
    public void CsvExportadoPelaTabelaVoltaIgualNaImportacao(string valor)
    {
        var csv = $"{ExportacaoCsv.Escapar("Nome")};{ExportacaoCsv.Escapar(valor)}\n";

        var linhas = ImportarBensCsv.LerCsv(csv);

        linhas.Single().ShouldBe(["Nome", valor]);
    }

    [Fact]
    public void FiltroDaTabelaContaFiltraBuscaELocalizaRegistroEscondido() => InterfaceWpf.Executar(() =>
    {
        var tabela = new DataGrid();
        var busca = new TextBox();
        var filtro = new FiltroTabela<Linha>(tabela, l => l.Id, l => l.Nome,
        [
            new("Todos", _ => true),
            new("Ativos", l => l.Ativo)
        ]);
        filtro.ConectarBusca(busca);
        filtro.AtualizarDados([new Linha(1, "Geladeira", true), new Linha(2, "Fogão", true), new Linha(3, "TV antiga", false)]);
        tabela.Items.Count.ShouldBe(3);

        busca.Text = "gela";
        tabela.Items.Count.ShouldBe(1);

        // A busca geral (Ctrl+K) precisa mostrar o registro mesmo com a busca da tela escondendo-o.
        filtro.Localizar(3).ShouldBeTrue();
        busca.Text.ShouldBeEmpty();
        tabela.Items.Count.ShouldBe(3);
        ((Linha)tabela.SelectedItem).Nome.ShouldBe("TV antiga");

        filtro.Localizar(99).ShouldBeFalse();
    });
}
