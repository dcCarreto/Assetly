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

        // Campos vazios limpam os valores: antes, apagar a data/preço no formulário era ignorado.
        bem.AtualizarCompra(comando.CompradoEm, ApoioCasosUso.DinheiroOpcional(comando.PrecoCompra, comando.CodigoMoeda));
        bem.AtualizarObservacoes(comando.Observacoes);
        bem.DefinirResponsavel(ApoioCasosUso.ExigirResponsavel(_repositorioBem, comando.Responsavel, bem.Id));
        _repositorioBem.Salvar(bem);
        return bem;
    }
}
