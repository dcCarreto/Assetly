using Assetly.Infraestrutura.Armazenamento;
using Shouldly;

namespace Assetly.Testes.Infraestrutura;

public class ArmazenamentoDocumentoLocalTests : IDisposable
{
    private readonly string _temporario = Path.Combine(Path.GetTempPath(), $"assetly-teste-{Guid.NewGuid()}");

    public ArmazenamentoDocumentoLocalTests()
    {
        Directory.CreateDirectory(_temporario);
    }

    [Fact]
    public void DeveCopiarDocumentoParaDiretorioLocalComNomeSeguro()
    {
        var origem = Path.Combine(_temporario, "Nota Fiscal Ágil.pdf");
        File.WriteAllText(origem, "conteúdo");
        var diretorioDocumentos = Path.Combine(_temporario, "assetly", "documentos");
        var armazenamento = new ArmazenamentoDocumentoLocal(diretorioDocumentos);

        var destino = armazenamento.Armazenar(origem);

        destino.ShouldStartWith(Path.GetFullPath(diretorioDocumentos));
        Path.GetFileName(destino).ShouldEndWith("-nota-fiscal-agil.pdf");
        File.ReadAllText(destino).ShouldBe("conteúdo");
    }

    [Fact]
    public void DeveManterCaminhoQuandoArquivoJaEstaNoDiretorioLocal()
    {
        var diretorioDocumentos = Path.Combine(_temporario, "documentos");
        Directory.CreateDirectory(diretorioDocumentos);
        var documentoInterno = Path.Combine(diretorioDocumentos, "documento.pdf");
        File.WriteAllText(documentoInterno, "conteúdo");
        var armazenamento = new ArmazenamentoDocumentoLocal(diretorioDocumentos);

        var destino = armazenamento.Armazenar(documentoInterno);

        destino.ShouldBe(Path.GetFullPath(documentoInterno));
    }

    [Fact]
    public void DeveFalharQuandoArquivoOrigemNaoExiste()
    {
        var armazenamento = new ArmazenamentoDocumentoLocal(Path.Combine(_temporario, "documentos"));

        var excecao = Should.Throw<ArgumentException>(
            () => armazenamento.Armazenar(Path.Combine(_temporario, "ausente.pdf")));
        excecao.Message.ShouldContain("arquivo do documento não encontrado");
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporario))
        {
            Directory.Delete(_temporario, recursive: true);
        }
    }
}
