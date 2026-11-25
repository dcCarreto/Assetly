using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;
using Assetly.Dominio.Bem;

namespace Assetly.Aplicacao.CasoDeUso;

/// <summary>Exclui o bem e, em cascata (chaves estrangeiras do banco), todas as garantias,
/// manutenções, documentos, alertas, convênios e gastos ligados a ele. Com a <see cref="LixeiraBens"/>, tudo vai antes
/// para a lixeira (e os arquivos dos documentos ficam guardados); sem ela, as cópias privadas dos arquivos dos
/// documentos excluídos também são removidas na hora.</summary>
public sealed class ExcluirBem(IRepositorioBem repositorioBem, IRepositorioDocumento repositorioDocumento, IArmazenamentoDocumento armazenamentoDocumento,
    LixeiraBens? lixeira = null)
{
    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem), "repositório de bens é obrigatório");
    private readonly IRepositorioDocumento _repositorioDocumento = repositorioDocumento ?? throw new ArgumentNullException(nameof(repositorioDocumento), "repositório de documentos é obrigatório");
    private readonly IArmazenamentoDocumento _armazenamentoDocumento = armazenamentoDocumento ?? throw new ArgumentNullException(nameof(armazenamentoDocumento), "armazenamento de documentos é obrigatório");

    public void Executar(BemId id)
    {
        var bem = ApoioCasosUso.ExigirBem(_repositorioBem, id);
        var documentos = _repositorioDocumento.ListarPorBem(id);
        lixeira?.Guardar(bem, documentos, DateTimeOffset.Now);

        // Um único DELETE: o banco apaga em cascata (de forma atômica) tudo o que é ligado ao bem.
        _repositorioBem.Excluir(id);

        foreach (var documento in documentos)
        {
            _repositorioDocumento.Excluir(documento.Id); // já apagado pela cascata; mantém repositórios sem chave estrangeira coerentes
            if (lixeira is null)
            {
                ApoioCasosUso.RemoverArquivoSemReferencia(_repositorioDocumento, _armazenamentoDocumento, documento.CaminhoLocal);
            }
        }
    }
}
