using Assetly.Infraestrutura.Armazenamento;

namespace Assetly.Infraestrutura.Banco;

public sealed class BancoDadosAssetly
{
    private readonly MigradorBancoDados _migradorBancoDados;

    public FabricaConexaoSqlite FabricaConexao { get; }

    public BancoDadosAssetly(FabricaConexaoSqlite fabricaConexao)
    {
        FabricaConexao = fabricaConexao;
        _migradorBancoDados = new MigradorBancoDados(fabricaConexao);
    }

    public static BancoDadosAssetly LocalPadrao() =>
        new(new FabricaConexaoSqlite(CaminhosArmazenamentoLocal.ArquivoBanco()));

    public static BancoDadosAssetly LocalTeste() =>
        new(new FabricaConexaoSqlite(CaminhosArmazenamentoLocal.ArquivoBancoTeste()));

    public void Inicializar() => _migradorBancoDados.Migrar();
}
