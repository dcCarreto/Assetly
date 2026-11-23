using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Veiculo;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class CadastrarVeiculo(IRepositorioBem repositorioBem, IRepositorioVeiculo repositorioVeiculo)
{
    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem));
    private readonly IRepositorioVeiculo _repositorioVeiculo = repositorioVeiculo ?? throw new ArgumentNullException(nameof(repositorioVeiculo));

    public Veiculo Executar(CadastrarVeiculoComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        ArgumentNullException.ThrowIfNull(comando.Dados);
        if (comando.BemExistente is not null && _repositorioVeiculo.BuscarPorBem(comando.BemExistente.Value) is not null)
        {
            throw new InvalidOperationException("este bem já tem uma ficha de veículo");
        }

        var nomePadrao = string.Join(' ', new[] { comando.Dados.Marca, comando.Dados.Modelo }.Where(p => !string.IsNullOrWhiteSpace(p)));
        var bem = CriacaoBemComFicha.ObterOuCriar(_repositorioBem, comando.BemExistente, TipoBem.Veiculo, comando.Nome,
            string.IsNullOrWhiteSpace(nomePadrao) ? "Veículo" : nomePadrao, comando.CompradoEm, comando.PrecoCompra, comando.CodigoMoeda);

        // Valida a ficha antes de gravar o bem: ficha inválida não deixa um bem "órfão" para trás.
        var veiculo = Veiculo.Criar(bem.Id, comando.Dados);
        if (comando.BemExistente is null)
        {
            _repositorioBem.Salvar(bem);
        }

        _repositorioVeiculo.Salvar(veiculo);
        return veiculo;
    }
}
