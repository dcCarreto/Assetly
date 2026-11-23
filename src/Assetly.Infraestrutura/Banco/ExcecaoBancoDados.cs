namespace Assetly.Infraestrutura.Banco;

public class ExcecaoBancoDados : Exception
{
    public ExcecaoBancoDados(string mensagem, Exception causa) : base(mensagem, causa)
    {
    }

    public ExcecaoBancoDados(string mensagem) : base(mensagem)
    {
    }
}
