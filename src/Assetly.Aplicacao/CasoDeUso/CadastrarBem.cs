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

        var bem = Bem.Criar(nome, comando.Tipo);
        bem.AtualizarCompra(comando.CompradoEm, ApoioCasosUso.DinheiroOpcional(comando.PrecoCompra, comando.CodigoMoeda));
        bem.AtualizarObservacoes(comando.Observacoes);
        bem.DefinirResponsavel(ApoioCasosUso.ExigirResponsavel(_repositorioBem, comando.Responsavel, bem.Id));
        _repositorioBem.Salvar(bem);
        return bem;
    }
}
