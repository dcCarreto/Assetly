namespace Assetly.Infraestrutura.Banco;

public class ExcecaoBancoDados(string mensagem, Exception causa) : Exception(mensagem, causa);
