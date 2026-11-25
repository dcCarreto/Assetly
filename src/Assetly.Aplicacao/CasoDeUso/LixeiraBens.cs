using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;

namespace Assetly.Aplicacao.CasoDeUso;

/// <summary>
/// Lixeira dos bens excluídos: a exclusão guarda um retrato do bem e de tudo o que era ligado a ele (garantias,
/// manutenções, documentos, fichas, despesas...) e mantém os arquivos dos documentos. Por <see cref="DiasRetencao"/> dias
/// o bem pode voltar inteiro; depois disso (ou ao excluir de vez) o retrato e os arquivos são apagados.
/// </summary>
public sealed class LixeiraBens(
    IRepositorioLixeira repositorioLixeira,
    ExportarDados exportarDados,
    ImportarDados importarDados,
    IRepositorioDocumento repositorioDocumento,
    IArmazenamentoDocumento armazenamentoDocumento)
{
    public const int DiasRetencao = 30;

    private readonly IRepositorioLixeira _repositorioLixeira = repositorioLixeira ?? throw new ArgumentNullException(nameof(repositorioLixeira));
    private readonly ExportarDados _exportarDados = exportarDados ?? throw new ArgumentNullException(nameof(exportarDados));
    private readonly ImportarDados _importarDados = importarDados ?? throw new ArgumentNullException(nameof(importarDados));
    private readonly IRepositorioDocumento _repositorioDocumento = repositorioDocumento ?? throw new ArgumentNullException(nameof(repositorioDocumento));
    private readonly IArmazenamentoDocumento _armazenamento = armazenamentoDocumento ?? throw new ArgumentNullException(nameof(armazenamentoDocumento));

    public static DateOnly ExpiraEm(ItemLixeira item) => DateOnly.FromDateTime(item.ExcluidoEm.LocalDateTime).AddDays(DiasRetencao);

    public IReadOnlyList<ItemLixeira> Listar() => _repositorioLixeira.Listar();

    /// <summary>Chamado por <see cref="ExcluirBem"/> antes de apagar o bem.</summary>
    internal void Guardar(Bem bem, IReadOnlyList<Documento> documentos, DateTimeOffset agora) =>
        _repositorioLixeira.Salvar(new ItemLixeira(Guid.NewGuid(), bem.Nome.Valor, agora, _exportarDados.ExecutarDoBem(bem.Id),
            documentos.Select(d => d.CaminhoLocal.Valor).ToList()));

    /// <summary>Devolve o bem e tudo o que era dele. Registros ligados a outro bem que não existe mais ficam de fora.</summary>
    public ResultadoImportacao Restaurar(Guid id)
    {
        var item = Exigir(id);
        var resultado = _importarDados.Executar(item.Conteudo);
        _repositorioLixeira.Excluir(id);
        return resultado;
    }

    public void ExcluirDefinitivamente(Guid id) => Apagar(Exigir(id));

    /// <summary>Apaga os itens com mais de <see cref="DiasRetencao"/> dias. Devolve quantos saíram.</summary>
    public int EsvaziarVencidos(DateOnly hoje)
    {
        var vencidos = _repositorioLixeira.Listar().Where(item => ExpiraEm(item) <= hoje).ToList();
        vencidos.ForEach(Apagar);
        return vencidos.Count;
    }

    /// <summary>Caminhos de arquivo que a lixeira ainda precisa (a limpeza da pasta de documentos não pode apagá-los).</summary>
    public IReadOnlyList<string> ArquivosGuardados() => _repositorioLixeira.Listar().SelectMany(item => item.Arquivos).ToList();

    private ItemLixeira Exigir(Guid id) =>
        _repositorioLixeira.Listar().FirstOrDefault(item => item.Id == id)
        ?? throw new Excecao.RecursoNaoEncontradoExcecao("item não encontrado na lixeira (talvez já tenha sido restaurado ou apagado).");

    private void Apagar(ItemLixeira item)
    {
        _repositorioLixeira.Excluir(item.Id);
        foreach (var arquivo in item.Arquivos)
        {
            ApoioCasosUso.RemoverArquivoSemReferencia(_repositorioDocumento, _armazenamento, new CaminhoDocumentoLocal(arquivo));
        }
    }
}
