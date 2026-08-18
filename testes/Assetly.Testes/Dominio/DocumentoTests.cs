using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;
using Shouldly;

namespace Assetly.Testes.Dominio;

public class DocumentoTests
{
    [Fact]
    public void DeveRegistrarDocumentoENormalizarCaminho()
    {
        var documento = Documento.Registrar(
            BemId.Novo(),
            TipoDocumento.NotaFiscal,
            new NomeDocumento("  Nota fiscal 2026  "),
            CaminhoDocumentoLocal.De("documentos/../documentos/nota-fiscal.pdf"),
            new DateOnly(2026, 2, 1));

        documento.Nome.Valor.ShouldBe("Nota fiscal 2026");
        documento.CaminhoLocal.Valor.ShouldBe(Path.Combine("documentos", "nota-fiscal.pdf"));
        documento.Status.ShouldBe(StatusDocumento.Disponivel);
    }

    [Fact]
    public void DeveMarcarDocumentoComoAusente()
    {
        var documento = Documento.Registrar(
            BemId.Novo(),
            TipoDocumento.Manual,
            new NomeDocumento("Manual"),
            CaminhoDocumentoLocal.De("documentos/manual.pdf"),
            new DateOnly(2026, 2, 1));

        documento.MarcarAusente();

        documento.Status.ShouldBe(StatusDocumento.Ausente);
    }

    [Fact]
    public void DeveRejeitarNomeDeDocumentoEmBranco()
    {
        var excecao = Should.Throw<ArgumentException>(() => new NomeDocumento(" "));
        excecao.Message.ShouldContain("nome do documento");
    }
}
