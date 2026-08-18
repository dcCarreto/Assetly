using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;

namespace Assetly.Aplicacao.Repositorio;

public interface IRepositorioDocumento
{
    Documento? BuscarPorId(DocumentoId id);

    IReadOnlyList<Documento> Listar();

    IReadOnlyList<Documento> ListarPorBem(BemId bemId);

    void Salvar(Documento documento);
}
