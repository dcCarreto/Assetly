using Assetly.Aplicacao.Dados;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;

namespace Assetly.Apresentacao.Visao;

/// <summary>Comprovantes (nota, recibo, foto) anexados a uma manutenção, despesa ou abastecimento: viram documentos do
/// bem com o vínculo ao registro, aparecem também no módulo Documentos e entram no backup.</summary>
internal static class ComprovanteApoio
{
    internal const string Marca = "  📎";

    /// <summary>Ids dos registros deste tipo que têm algum comprovante (para marcar as linhas da tabela).</summary>
    internal static HashSet<Guid> ComComprovante(ContextoAplicacao contexto, TipoVinculoDocumento tipo) =>
        contexto.ListarDocumentos().Executar().Where(d => d.Vinculo?.Tipo == tipo).Select(d => d.Vinculo!.Id).ToHashSet();

    internal static void Anexar(ContextoAplicacao contexto, BemId bemId, VinculoDocumento vinculo, string descricao)
    {
        var arquivo = DialogoApoio.SelecionarArquivo("Comprovante (nota, recibo, foto)", null);
        if (arquivo is null)
        {
            return;
        }

        var nome = $"Comprovante: {descricao}";
        contexto.RegistrarDocumento().Executar(new RegistrarDocumentoComando(bemId, TipoDocumento.Recibo,
            nome.Length > NomeDocumento.TamanhoMaximo ? nome[..NomeDocumento.TamanhoMaximo] : nome, arquivo, ContextoApoio.Hoje(), Vinculo: vinculo));
        contexto.NotificarAlteracao();
    }

    /// <summary>Abre o comprovante mais recente do registro (os demais ficam no módulo Documentos).</summary>
    internal static void Abrir(ContextoAplicacao contexto, VinculoDocumento vinculo)
    {
        var documento = contexto.ListarDocumentos().Executar()
            .Where(d => vinculo.Equals(d.Vinculo))
            .OrderByDescending(d => d.RegistradoEm)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("nenhum comprovante anexado a este registro. Use \"Anexar comprovante…\".");
        ModuloDocumentos.AbrirDocumento(documento);
    }
}
