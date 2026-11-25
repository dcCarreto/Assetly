namespace Assetly.Aplicacao.Repositorio;

/// <summary>Bem excluído guardado na lixeira: o retrato dele e de tudo o que era ligado a ele (no formato da exportação)
/// e os arquivos dos documentos, que só são apagados quando o item sai da lixeira de vez.</summary>
public sealed record ItemLixeira(Guid Id, string Descricao, DateTimeOffset ExcluidoEm, string Conteudo, IReadOnlyList<string> Arquivos);

public interface IRepositorioLixeira
{
    IReadOnlyList<ItemLixeira> Listar();

    void Salvar(ItemLixeira item);

    void Excluir(Guid id);
}
