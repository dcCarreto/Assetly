using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Manutencao;
using Shouldly;

namespace Assetly.Testes.Dominio;

public class ManutencaoTests
{
    [Fact]
    public void DeveMarcarManutencaoAgendadaComoAtrasadaAposDataPrevista()
    {
        var manutencao = Manutencao.Agendar(
            BemId.Novo(),
            TipoManutencao.Preventiva,
            "Troca de óleo",
            new DateOnly(2026, 3, 10));

        manutencao.StatusEm(new DateOnly(2026, 3, 11)).ShouldBe(StatusManutencao.Atrasada);
    }

    [Fact]
    public void DeveManterManutencaoConcluidaEncerrada()
    {
        var manutencao = Manutencao.Agendar(
            BemId.Novo(),
            TipoManutencao.Corretiva,
            "Trocar bateria",
            new DateOnly(2026, 3, 10));

        manutencao.Concluir(new DateOnly(2026, 3, 9), Dinheiro.De(49.90m, "USD"));

        manutencao.Status.ShouldBe(StatusManutencao.Concluida);
        manutencao.StatusEm(new DateOnly(2026, 4, 1)).ShouldBe(StatusManutencao.Concluida);
        manutencao.Custo.ShouldNotBeNull();
        manutencao.Custo!.Valor.ShouldBe(49.90m);
    }

    [Fact]
    public void DeveRejeitarDescricaoEmBranco()
    {
        var excecao = Should.Throw<ArgumentException>(() => Manutencao.Agendar(
            BemId.Novo(),
            TipoManutencao.Outra,
            " ",
            new DateOnly(2026, 3, 10)));
        excecao.Message.ShouldContain("descrição da manutenção");
    }
}
