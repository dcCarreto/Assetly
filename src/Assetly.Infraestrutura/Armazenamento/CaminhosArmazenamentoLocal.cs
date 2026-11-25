namespace Assetly.Infraestrutura.Armazenamento;

public static class CaminhosArmazenamentoLocal
{
    private const string DiretorioAplicacao = ".assetly";
    private const string DiretorioAplicacaoTeste = ".assetly-teste";
    private const string DiretorioBancoNome = "banco";
    private const string DiretorioDocumentosNome = "documentos";
    private const string ArquivoBancoNome = "assetly.sqlite3";
    private const string ArquivoBancoTesteNome = "assetly-teste.sqlite3";

    public static string DiretorioBase() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), DiretorioAplicacao);

    public static string DiretorioBaseTeste() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), DiretorioAplicacaoTeste);

    public static string DiretorioBanco() => Path.Combine(DiretorioBase(), DiretorioBancoNome);

    public static string DiretorioBancoTeste() => Path.Combine(DiretorioBaseTeste(), DiretorioBancoNome);

    public static string ArquivoBanco() => Path.Combine(DiretorioBanco(), ArquivoBancoNome);

    public static string ArquivoBancoTeste() => Path.Combine(DiretorioBancoTeste(), ArquivoBancoTesteNome);

    public static string DiretorioDocumentos() => Path.Combine(DiretorioBase(), DiretorioDocumentosNome);

    public static string DiretorioDocumentosTeste() => Path.Combine(DiretorioBaseTeste(), DiretorioDocumentosNome);

    /// <summary>Cópias automáticas do estado anterior, feitas antes de cada restauração de backup.</summary>
    public static string DiretorioSeguranca() => Path.Combine(DiretorioBase(), "seguranca");

    public static string DiretorioSegurancaTeste() => Path.Combine(DiretorioBaseTeste(), "seguranca");

    /// <summary>Registro de erros inesperados (um arquivo por dia) do ambiente de produção.</summary>
    public static string DiretorioLogs() => Path.Combine(DiretorioBase(), "logs");

    /// <summary>Registro de erros do ambiente de teste, separado do de produção.</summary>
    public static string DiretorioLogsTeste() => Path.Combine(DiretorioBaseTeste(), "logs");
}
