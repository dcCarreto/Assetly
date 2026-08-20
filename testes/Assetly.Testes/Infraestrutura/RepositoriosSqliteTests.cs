using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Manutencao;
using Assetly.Dominio.Saude;
using Assetly.Infraestrutura.Banco;
using Assetly.Infraestrutura.Repositorio;
using Shouldly;

namespace Assetly.Testes.Infraestrutura;

public class RepositoriosSqliteTests
{
    [Fact]
    public void DeveMigrarBancoDeFormaIdempotente()
    {
        var banco = BancoTemporario();

        banco.Inicializar();
        banco.Inicializar();

        File.Exists(banco.FabricaConexao.ArquivoBanco).ShouldBeTrue();
        using var conexao = banco.FabricaConexao.Abrir();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT COUNT(*) AS total FROM schema_migrations";
        var total = (long)comando.ExecuteScalar()!;
        total.ShouldBe(2L);
    }

    [Fact]
    public void DeveSalvarERecuperarBens()
    {
        var banco = BancoTemporario();
        banco.Inicializar();
        var repositorio = new RepositorioBemSqlite(banco.FabricaConexao);
        var bem = Bem.Criar(
            new NomeBem("Notebook"),
            TipoBem.Eletronico,
            new DateOnly(2026, 1, 10),
            Dinheiro.De(2500.00m, "BRL"));

        bem.AtualizarObservacoes("Uso de trabalho");
        bem.MarcarEmManutencao();
        repositorio.Salvar(bem);

        var recuperado = repositorio.BuscarPorId(bem.Id) ?? throw new InvalidOperationException("bem não encontrado");

        recuperado.Nome.Valor.ShouldBe("Notebook");
        recuperado.Status.ShouldBe(StatusBem.EmManutencao);
        recuperado.PrecoCompra.ShouldNotBeNull();
        recuperado.PrecoCompra!.Valor.ShouldBe(2500.00m);
        repositorio.Listar().Select(item => item.Id).ShouldBe([bem.Id]);
    }

    [Fact]
    public void DeveSalvarEListarDadosRelacionadosAoBem()
    {
        var banco = BancoTemporario();
        banco.Inicializar();
        var repositorioBem = new RepositorioBemSqlite(banco.FabricaConexao);
        var repositorioGarantia = new RepositorioGarantiaSqlite(banco.FabricaConexao);
        var repositorioManutencao = new RepositorioManutencaoSqlite(banco.FabricaConexao);
        var repositorioDocumento = new RepositorioDocumentoSqlite(banco.FabricaConexao);
        var repositorioAlerta = new RepositorioAlertaSqlite(banco.FabricaConexao);
        var repositorioSaude = new RepositorioSaudeSqlite(banco.FabricaConexao);
        var repositorioGastoSaude = new RepositorioGastoSaudeSqlite(banco.FabricaConexao);
        var bem = Bem.Criar(new NomeBem("Impressora"), TipoBem.Eletronico);

        repositorioBem.Salvar(bem);

        var garantia = Garantia.Criar(
            bem.Id, TipoGarantia.Fabricante, "Fabricante", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        garantia.AtualizarContatoSuporte("0800");
        repositorioGarantia.Salvar(garantia);

        var manutencao = Manutencao.Agendar(bem.Id, TipoManutencao.Preventiva, "Limpeza", new DateOnly(2026, 8, 1));
        manutencao.Concluir(new DateOnly(2026, 8, 1), Dinheiro.De(120.00m, "BRL"));
        repositorioManutencao.Salvar(manutencao);

        var documento = Documento.Registrar(
            bem.Id, TipoDocumento.NotaFiscal, new NomeDocumento("Nota fiscal"),
            CaminhoDocumentoLocal.De("documentos/nota.pdf"), new DateOnly(2026, 1, 2));
        documento.MarcarAusente();
        repositorioDocumento.Salvar(documento);

        var alerta = Alerta.Criar(
            bem.Id, TipoAlerta.GarantiaVencendo, SeveridadeAlerta.Aviso, "Garantia vence em breve",
            new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31));
        alerta.TomarCiencia();
        repositorioAlerta.Salvar(alerta);

        var saude = Saude.Criar(
            bem.Id, TipoSaude.Convenio, "Amil", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), "0800-123456");
        saude.Cancelar(new DateOnly(2026, 6, 1));
        repositorioSaude.Salvar(saude);

        var gastoSaude = GastoSaude.Registrar(
            bem.Id, TipoGastoSaude.Farmacia, "Antibiótico", new DateOnly(2026, 3, 10), Dinheiro.De(89.90m, "BRL"));
        repositorioGastoSaude.Salvar(gastoSaude);

        repositorioGarantia.ListarPorBem(bem.Id).Single().StatusEm(new DateOnly(2026, 12, 15)).ShouldBe(StatusGarantia.PertoDoVencimento);
        repositorioManutencao.ListarPorBem(bem.Id).Single().Status.ShouldBe(StatusManutencao.Concluida);
        repositorioDocumento.ListarPorBem(bem.Id).Single().Status.ShouldBe(StatusDocumento.Ausente);
        repositorioAlerta.ListarPorBem(bem.Id).Single().Status.ShouldBe(StatusAlerta.Ciente);
        repositorioSaude.ListarPorBem(bem.Id).Single().StatusEm(new DateOnly(2026, 7, 1)).ShouldBe(StatusSaude.Cancelada);
        repositorioGastoSaude.ListarPorBem(bem.Id).Single().Valor.Valor.ShouldBe(89.90m);
    }

    private static BancoDadosAssetly BancoTemporario()
    {
        var arquivo = Path.Combine(Path.GetTempPath(), $"assetly-{Guid.NewGuid()}", "assetly.sqlite3");
        return new BancoDadosAssetly(new FabricaConexaoSqlite(arquivo));
    }
}
