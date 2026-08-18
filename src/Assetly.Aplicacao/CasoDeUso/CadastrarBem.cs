using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class CadastrarBem(IRepositorioBem repositorioBem)
{
    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem), "repositório de bens é obrigatório");

    public Bem Executar(CadastrarBemComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        var nome = new NomeBem(comando.Nome);

        var bem = comando.CompradoEm is null
            ? Bem.Criar(nome, comando.Tipo)
            : Bem.Criar(nome, comando.Tipo, comando.CompradoEm.Value, ApoioCasosUso.DinheiroOpcional(comando.PrecoCompra, comando.CodigoMoeda));

        if (comando.CompradoEm is null && comando.PrecoCompra is not null)
        {
            throw new ArgumentException("data de compra é obrigatória quando preço de compra é informado");
        }

        bem.AtualizarObservacoes(comando.Observacoes);
        _repositorioBem.Salvar(bem);
        return bem;
    }
}
