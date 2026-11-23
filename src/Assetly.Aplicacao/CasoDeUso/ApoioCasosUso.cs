using Assetly.Aplicacao.Excecao;
using Assetly.Aplicacao.Repositorio;
using Assetly.Aplicacao.Servico;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Manutencao;
using Assetly.Dominio.Saude;

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

    internal static Saude ExigirSaude(IRepositorioSaude repositorio, SaudeId id) =>
        repositorio.BuscarPorId(id) ?? throw new RecursoNaoEncontradoExcecao($"Registro de saúde não encontrado: {id.Valor}");

    internal static GastoSaude ExigirGastoSaude(IRepositorioGastoSaude repositorio, GastoSaudeId id) =>
        repositorio.BuscarPorId(id) ?? throw new RecursoNaoEncontradoExcecao($"Gasto de saúde não encontrado: {id.Valor}");

    internal static Dominio.Veiculo.Veiculo ExigirVeiculo(IRepositorioVeiculo repositorio, BemId bemId) =>
        repositorio.BuscarPorBem(bemId) ?? throw new RecursoNaoEncontradoExcecao("Ficha de veículo não encontrada para este bem");

    internal static Dominio.Veiculo.Abastecimento ExigirAbastecimento(IRepositorioAbastecimento repositorio, Dominio.Veiculo.AbastecimentoId id) =>
        repositorio.BuscarPorId(id) ?? throw new RecursoNaoEncontradoExcecao($"Abastecimento não encontrado: {id.Valor}");

    internal static Dominio.Imovel.Imovel ExigirImovel(IRepositorioImovel repositorio, BemId bemId) =>
        repositorio.BuscarPorBem(bemId) ?? throw new RecursoNaoEncontradoExcecao("Ficha de imóvel não encontrada para este bem");

    internal static Dominio.Despesa.Despesa ExigirDespesa(IRepositorioDespesa repositorio, Dominio.Despesa.DespesaId id) =>
        repositorio.BuscarPorId(id) ?? throw new RecursoNaoEncontradoExcecao($"Despesa não encontrada: {id.Valor}");

    /// <summary>Valida o responsável (modo família): precisa existir e ser do tipo Pessoa.</summary>
    internal static BemId? ExigirResponsavel(IRepositorioBem repositorio, BemId? responsavel, BemId bemAtual)
    {
        if (responsavel is null)
        {
            return null;
        }

        if (responsavel.Value.Equals(bemAtual))
        {
            throw new ArgumentException("um bem não pode ser responsável por ele mesmo");
        }

        var pessoa = ExigirBem(repositorio, responsavel.Value);
        if (pessoa.Tipo != TipoBem.Pessoa)
        {
            throw new ArgumentException($"\"{pessoa.Nome.Valor}\" não é uma pessoa. Cadastre o responsável com o tipo \"Pessoa (titular)\".");
        }

        return responsavel;
    }

    internal static void RemoverArquivoSemReferencia(IRepositorioDocumento repositorio, IArmazenamentoDocumento armazenamento, CaminhoDocumentoLocal caminho)
    {
        var aindaReferenciado = repositorio.Listar().Any(documento =>
            string.Equals(documento.CaminhoLocal.Valor, caminho.Valor, StringComparison.OrdinalIgnoreCase));
        if (!aindaReferenciado)
        {
            armazenamento.Remover(caminho.Valor);
        }
    }

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
