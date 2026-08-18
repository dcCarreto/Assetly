using Assetly.Aplicacao.Excecao;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Manutencao;

namespace Assetly.Aplicacao.CasoDeUso;

internal static class ApoioCasosUso
{
    internal static Bem ExigirBem(IRepositorioBem repositorio, BemId id) =>
        repositorio.BuscarPorId(id) ?? throw new RecursoNaoEncontradoExcecao($"Bem não encontrado: {id.Valor}");

    internal static Garantia ExigirGarantia(IRepositorioGarantia repositorio, GarantiaId id) =>
        repositorio.BuscarPorId(id) ?? throw new RecursoNaoEncontradoExcecao($"Garantia não encontrada: {id.Valor}");

    internal static Manutencao ExigirManutencao(IRepositorioManutencao repositorio, ManutencaoId id) =>
        repositorio.BuscarPorId(id) ?? throw new RecursoNaoEncontradoExcecao($"Manutenção não encontrada: {id.Valor}");

    internal static Documento ExigirDocumento(IRepositorioDocumento repositorio, DocumentoId id) =>
        repositorio.BuscarPorId(id) ?? throw new RecursoNaoEncontradoExcecao($"Documento não encontrado: {id.Valor}");

    internal static Alerta ExigirAlerta(IRepositorioAlerta repositorio, AlertaId id) =>
        repositorio.BuscarPorId(id) ?? throw new RecursoNaoEncontradoExcecao($"Alerta não encontrado: {id.Valor}");

    internal static Dinheiro? DinheiroOpcional(decimal? valor, string? codigoMoeda)
    {
        if (valor is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(codigoMoeda))
        {
            throw new ArgumentException("código da moeda é obrigatório quando valor é informado");
        }

        return Dinheiro.De(valor.Value, codigoMoeda);
    }
}
