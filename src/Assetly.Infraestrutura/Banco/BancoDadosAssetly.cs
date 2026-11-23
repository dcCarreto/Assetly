using Assetly.Infraestrutura.Armazenamento;

namespace Assetly.Infraestrutura.Banco;

public sealed class BancoDadosAssetly
{
    private readonly MigradorBancoDados _migradorBancoDados;

    public FabricaConexaoSqlite FabricaConexao { get; }

    /// <param name="diretorioCopiaSeguranca">Pasta para a cópia automática do banco antes de migrações (ver
    /// <see cref="MigradorBancoDados"/>); nulo desliga.</param>
    public BancoDadosAssetly(FabricaConexaoSqlite fabricaConexao, string? diretorioCopiaSeguranca = null)
    {
        FabricaConexao = fabricaConexao;
        _migradorBancoDados = new MigradorBancoDados(fabricaConexao, diretorioCopiaSeguranca);
    }

    public static BancoDadosAssetly LocalPadrao() =>
        new(new FabricaConexaoSqlite(CaminhosArmazenamentoLocal.ArquivoBanco()), CaminhosArmazenamentoLocal.DiretorioSeguranca());

    public static BancoDadosAssetly LocalTeste() =>
        new(new FabricaConexaoSqlite(CaminhosArmazenamentoLocal.ArquivoBancoTeste()), CaminhosArmazenamentoLocal.DiretorioSegurancaTeste());

    public void Inicializar() => _migradorBancoDados.Migrar();
}
