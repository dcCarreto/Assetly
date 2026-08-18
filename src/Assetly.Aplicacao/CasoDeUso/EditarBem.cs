using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class EditarBem(IRepositorioBem repositorioBem)
{
    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem), "repositório de bens é obrigatório");

    public Bem Executar(EditarBemComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        var bem = ApoioCasosUso.ExigirBem(_repositorioBem, comando.Id);

        bem.Renomear(new NomeBem(comando.Nome));
        bem.AlterarTipo(comando.Tipo);

        if (comando.CompradoEm is null && comando.PrecoCompra is not null)
        {
            throw new ArgumentException("data de compra é obrigatória quando preço de compra é informado");
        }

        if (comando.CompradoEm is not null)
        {
            bem.AtualizarCompra(comando.CompradoEm.Value, ApoioCasosUso.DinheiroOpcional(comando.PrecoCompra, comando.CodigoMoeda));
        }

        bem.AtualizarObservacoes(comando.Observacoes);
        _repositorioBem.Salvar(bem);
        return bem;
    }
}
