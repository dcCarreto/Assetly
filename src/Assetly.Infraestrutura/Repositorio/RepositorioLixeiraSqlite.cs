using Assetly.Aplicacao.Repositorio;
using Assetly.Infraestrutura.Banco;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioLixeiraSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioLixeira
{
    private readonly ExecutorSqlite _executor = new(fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao)));

    public IReadOnlyList<ItemLixeira> Listar() =>
        _executor.Consultar("SELECT * FROM lixeira ORDER BY excluido_em DESC", leitor => new ItemLixeira(
                Guid.Parse(leitor.GetString(leitor.GetOrdinal("id"))),
                leitor.GetString(leitor.GetOrdinal("descricao")),
                ConversorSqlite.DataHora(leitor, "excluido_em")!.Value,
                leitor.GetString(leitor.GetOrdinal("conteudo")),
                ConversorSqlite.ListaDeTexto(leitor.GetString(leitor.GetOrdinal("arquivos")))),
            "Não foi possível listar a lixeira");

    public void Salvar(ItemLixeira item) =>
        _executor.Executar("""
            INSERT INTO lixeira (id, descricao, excluido_em, conteudo, arquivos)
            VALUES ($id, $descricao, $excluidoEm, $conteudo, $arquivos)
            ON CONFLICT(id) DO UPDATE SET descricao = excluded.descricao, excluido_em = excluded.excluido_em,
                conteudo = excluded.conteudo, arquivos = excluded.arquivos
            """, "Não foi possível guardar o item na lixeira",
            ("$id", item.Id.ToString()),
            ("$descricao", item.Descricao),
            ("$excluidoEm", ConversorSqlite.Texto(item.ExcluidoEm)!),
            ("$conteudo", item.Conteudo),
            ("$arquivos", ConversorSqlite.ListaParaTexto(item.Arquivos)));

    public void Excluir(Guid id) =>
        _executor.Executar("DELETE FROM lixeira WHERE id = $id", "Não foi possível remover o item da lixeira", ("$id", id.ToString()));
}
