using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed class AlterarStatusBem(IRepositorioBem repositorioBem)
{
    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem), "repositório de bens é obrigatório");

    public Bem Executar(AlterarStatusBemComando comando)
    {
        ArgumentNullException.ThrowIfNull(comando);
        var bem = ApoioCasosUso.ExigirBem(_repositorioBem, comando.Id);

        switch (comando.Status)
        {
            case StatusBem.Ativo:
                bem.Ativar();
                break;
            case StatusBem.EmManutencao:
                bem.MarcarEmManutencao();
                break;
            case StatusBem.Descartado:
                bem.Descartar();
                break;
            case StatusBem.Arquivado:
                bem.Arquivar();
                break;
        }

        _repositorioBem.Salvar(bem);
        return bem;
    }
}
