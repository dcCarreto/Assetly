using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Infraestrutura.Banco;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioBemSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioBem
{
    private readonly ExecutorSqlite _executor = new(fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao), "fábrica de conexão é obrigatória"));

    public Bem? BuscarPorId(BemId id) =>
        _executor.Consultar("SELECT * FROM bens WHERE id = $id", MapeadorBem.Mapear, "Não foi possível buscar bem",
            ("$id", id.Valor.ToString())).FirstOrDefault();

    public IReadOnlyList<Bem> Listar() =>
        _executor.Consultar("SELECT * FROM bens ORDER BY nome", MapeadorBem.Mapear, "Não foi possível listar bens");

    public void Salvar(Bem bem)
    {
        const string sql = """
            INSERT INTO bens (id, nome, tipo, status, comprado_em, preco_compra, moeda, observacoes, responsavel_id)
            VALUES ($id, $nome, $tipo, $status, $compradoEm, $precoCompra, $moeda, $observacoes, $responsavelId)
            ON CONFLICT(id) DO UPDATE SET
                nome = excluded.nome,
                tipo = excluded.tipo,
                status = excluded.status,
                comprado_em = excluded.comprado_em,
                preco_compra = excluded.preco_compra,
                moeda = excluded.moeda,
                observacoes = excluded.observacoes,
                responsavel_id = excluded.responsavel_id
            """;
        var preco = bem.PrecoCompra;
        _executor.Executar(sql, "Não foi possível salvar bem",
            ("$id", bem.Id.Valor.ToString()),
            ("$nome", bem.Nome.Valor),
            ("$tipo", ConversorSqlite.EnumParaTexto(bem.Tipo)),
            ("$status", ConversorSqlite.EnumParaTexto(bem.Status)),
            ("$compradoEm", ConversorSqlite.Nulo(ConversorSqlite.Texto(bem.CompradoEm))),
            ("$precoCompra", ConversorSqlite.Nulo(ConversorSqlite.Valor(preco))),
            ("$moeda", ConversorSqlite.Nulo(ConversorSqlite.Moeda(preco))),
            ("$observacoes", bem.Observacoes),
            ("$responsavelId", ConversorSqlite.Nulo(bem.ResponsavelId?.Valor.ToString())));
    }

    public void Excluir(BemId id) =>
        _executor.Executar("DELETE FROM bens WHERE id = $id", "Não foi possível excluir bem", ("$id", id.Valor.ToString()));
}
