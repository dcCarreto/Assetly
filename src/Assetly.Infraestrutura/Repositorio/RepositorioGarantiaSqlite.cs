using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Garantia;
using Assetly.Infraestrutura.Banco;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioGarantiaSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioGarantia
{
    private const string Ordem = "ORDER BY termina_em, fornecedor";
    private readonly ExecutorSqlite _executor = new(fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao), "fábrica de conexão é obrigatória"));

    public Garantia? BuscarPorId(GarantiaId id) =>
        _executor.Consultar("SELECT * FROM garantias WHERE id = $id", MapeadorGarantia.Mapear, "Não foi possível buscar garantia",
            ("$id", id.Valor.ToString())).FirstOrDefault();

    public IReadOnlyList<Garantia> Listar() =>
        _executor.Consultar($"SELECT * FROM garantias {Ordem}", MapeadorGarantia.Mapear, "Não foi possível listar garantias");

    public IReadOnlyList<Garantia> ListarPorBem(BemId bemId) =>
        _executor.Consultar($"SELECT * FROM garantias WHERE bem_id = $bemId {Ordem}", MapeadorGarantia.Mapear, "Não foi possível listar garantias",
            ("$bemId", bemId.Valor.ToString()));

    public void Salvar(Garantia garantia)
    {
        const string sql = """
            INSERT INTO garantias (id, bem_id, tipo, fornecedor, inicia_em, termina_em, contato_suporte)
            VALUES ($id, $bemId, $tipo, $fornecedor, $iniciaEm, $terminaEm, $contatoSuporte)
            ON CONFLICT(id) DO UPDATE SET
                bem_id = excluded.bem_id,
                tipo = excluded.tipo,
                fornecedor = excluded.fornecedor,
                inicia_em = excluded.inicia_em,
                termina_em = excluded.termina_em,
                contato_suporte = excluded.contato_suporte
            """;
        _executor.Executar(sql, "Não foi possível salvar garantia",
            ("$id", garantia.Id.Valor.ToString()),
            ("$bemId", garantia.BemId.Valor.ToString()),
            ("$tipo", ConversorSqlite.EnumParaTexto(garantia.Tipo)),
            ("$fornecedor", garantia.Fornecedor),
            ("$iniciaEm", ConversorSqlite.Texto(garantia.Periodo.IniciaEm)!),
            ("$terminaEm", ConversorSqlite.Texto(garantia.Periodo.TerminaEm)!),
            ("$contatoSuporte", garantia.ContatoSuporte));
    }

    public void Excluir(GarantiaId id) =>
        _executor.Executar("DELETE FROM garantias WHERE id = $id", "Não foi possível excluir garantia", ("$id", id.Valor.ToString()));
}
