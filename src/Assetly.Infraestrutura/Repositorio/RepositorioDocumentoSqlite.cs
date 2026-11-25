using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;
using Assetly.Infraestrutura.Banco;

namespace Assetly.Infraestrutura.Repositorio;

public sealed class RepositorioDocumentoSqlite(FabricaConexaoSqlite fabricaConexao) : IRepositorioDocumento
{
    private const string Ordem = "ORDER BY registrado_em DESC, nome";
    private readonly ExecutorSqlite _executor = new(fabricaConexao ?? throw new ArgumentNullException(nameof(fabricaConexao), "fábrica de conexão é obrigatória"));

    public Documento? BuscarPorId(DocumentoId id) =>
        _executor.Consultar("SELECT * FROM documentos WHERE id = $id", MapeadorDocumento.Mapear, "Não foi possível buscar documento",
            ("$id", id.Valor.ToString())).FirstOrDefault();

    public IReadOnlyList<Documento> Listar() =>
        _executor.Consultar($"SELECT * FROM documentos {Ordem}", MapeadorDocumento.Mapear, "Não foi possível listar documentos");

    public IReadOnlyList<Documento> ListarPorBem(BemId bemId) =>
        _executor.Consultar($"SELECT * FROM documentos WHERE bem_id = $bemId {Ordem}", MapeadorDocumento.Mapear, "Não foi possível listar documentos",
            ("$bemId", bemId.Valor.ToString()));

    public void Salvar(Documento documento)
    {
        const string sql = """
            INSERT INTO documentos (id, bem_id, tipo, nome, caminho_local, registrado_em, status, chave_acesso, url_consulta)
            VALUES ($id, $bemId, $tipo, $nome, $caminhoLocal, $registradoEm, $status, $chaveAcesso, $urlConsulta)
            ON CONFLICT(id) DO UPDATE SET
                bem_id = excluded.bem_id,
                tipo = excluded.tipo,
                nome = excluded.nome,
                caminho_local = excluded.caminho_local,
                registrado_em = excluded.registrado_em,
                status = excluded.status,
                chave_acesso = excluded.chave_acesso,
                url_consulta = excluded.url_consulta
            """;
        _executor.Executar(sql, "Não foi possível salvar documento",
            ("$id", documento.Id.Valor.ToString()),
            ("$bemId", documento.BemId.Valor.ToString()),
            ("$tipo", ConversorSqlite.EnumParaTexto(documento.Tipo)),
            ("$nome", documento.Nome.Valor),
            ("$caminhoLocal", documento.CaminhoLocal.Valor),
            ("$registradoEm", ConversorSqlite.Texto(documento.RegistradoEm)!),
            ("$status", ConversorSqlite.EnumParaTexto(documento.Status)),
            ("$chaveAcesso", documento.ChaveAcesso),
            ("$urlConsulta", documento.UrlConsulta));
    }

    public void Excluir(DocumentoId id) =>
        _executor.Executar("DELETE FROM documentos WHERE id = $id", "Não foi possível excluir documento", ("$id", id.Valor.ToString()));
}
