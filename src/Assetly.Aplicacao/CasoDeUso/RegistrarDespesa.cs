using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Despesa;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class RegistrarDespesa(IRepositorioBem repositorioBem, IRepositorioDespesa repositorioDespesa)
{
    public const int MaximoRepeticoes = 120;

    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem));
    private readonly IRepositorioDespesa _repositorioDespesa = repositorioDespesa ?? throw new ArgumentNullException(nameof(repositorioDespesa));

    public IReadOnlyList<Despesa> Executar(RegistrarDespesaComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ApoioCasosUso.ExigirBem(_repositorioBem, comando.BemId);
        if (comando.Repeticoes is < 1 or > MaximoRepeticoes)
        {
            throw new ArgumentException($"número de parcelas/meses deve ser de 1 a {MaximoRepeticoes}");
        }

        var valor = Dinheiro.De(comando.Valor, comando.CodigoMoeda);
        var descricaoBase = Validacao.ExigirTexto(comando.Descricao, "descrição da despesa", Despesa.TamanhoMaximoDescricao - 10);
        var despesas = Enumerable.Range(0, comando.Repeticoes)
            .Select(indice => Despesa.Registrar(
                comando.BemId,
                comando.Tipo,
                comando.Repeticoes == 1 ? descricaoBase : $"{descricaoBase} ({indice + 1}/{comando.Repeticoes})",
                comando.Vencimento.AddMonths(indice),
                valor))
            .ToList();

        _repositorioDespesa.SalvarTodos(despesas);
        return despesas;
    }
}
