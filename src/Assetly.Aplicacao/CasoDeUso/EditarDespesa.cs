using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Despesa;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class EditarDespesa(IRepositorioDespesa repositorioDespesa)
{
    private readonly IRepositorioDespesa _repositorioDespesa = repositorioDespesa ?? throw new ArgumentNullException(nameof(repositorioDespesa));

    public Despesa Executar(EditarDespesaComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        var despesa = ApoioCasosUso.ExigirDespesa(_repositorioDespesa, comando.Id);
        despesa.Atualizar(comando.Tipo, comando.Descricao, comando.Vencimento, Dinheiro.De(comando.Valor, comando.CodigoMoeda));
        _repositorioDespesa.Salvar(despesa);
        return despesa;
    }
}
