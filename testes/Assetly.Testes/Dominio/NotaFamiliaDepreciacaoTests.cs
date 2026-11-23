using Assetly.Dominio.Backup;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Documento;
using Shouldly;

namespace Assetly.Testes.Dominio;

public class NotaFamiliaDepreciacaoTests
{
    /// <summary>Chave de exemplo com dígito verificador calculado: PR, nov/2026, CNPJ 12.345.678/0001-95, NFC-e série 1 nº 12345.</summary>
    internal static string ChaveValida()
    {
        const string base43 = "4126111234567800019565001000012345112345678";
        return base43 + ChaveAcessoNfe.DigitoVerificador(base43);
    }

    [Fact]
    public void ChaveDeAcessoValidaDeveExporSeusCampos()
    {
        var chave = ChaveAcessoNfe.Criar(ChaveValida());

        chave.Uf.ShouldBe("PR");
        chave.MesEmissao.ShouldBe(new DateOnly(2026, 11, 1));
        chave.CnpjFormatado.ShouldBe("12.345.678/0001-95");
        chave.Modelo.ShouldBe("NFC-e");
        chave.Serie.ShouldBe(1);
        chave.Numero.ShouldBe(12345);
        chave.Formatada.Split(' ').Length.ShouldBe(11);
    }

    [Fact]
    public void ChaveDeveAceitarSeparadoresERecusarDigitoErrado()
    {
        var valida = ChaveValida();
        ChaveAcessoNfe.TentarCriar(ChaveAcessoNfe.Criar(valida).Formatada).ShouldNotBeNull();

        var ultimo = valida[^1] - '0';
        var errada = valida[..43] + ((ultimo + 1) % 10);
        ChaveAcessoNfe.TentarCriar(errada).ShouldBeNull();
        ChaveAcessoNfe.TentarCriar("123").ShouldBeNull();
        ChaveAcessoNfe.TentarCriar("99" + valida[2..]).ShouldBeNull();
        Should.Throw<ArgumentException>(() => ChaveAcessoNfe.Criar(errada));
    }

    [Fact]
    public void DocumentoDeveVincularNotaEletronicaComLinkSeguro()
    {
        var documento = Documento.Registrar(BemId.Novo(), TipoDocumento.NotaFiscal, new NomeDocumento("Nota"), new CaminhoDocumentoLocal(@"C:\nota.pdf"), new DateOnly(2026, 11, 1));

        documento.VincularNotaEletronica(ChaveAcessoNfe.Criar(ChaveValida()), "https://sefaz.exemplo/nfce?p=1");

        documento.ChaveAcesso.ShouldBe(ChaveValida());
        documento.UrlConsulta.ShouldBe("https://sefaz.exemplo/nfce?p=1");
        Should.Throw<ArgumentException>(() => documento.VincularNotaEletronica(null, "javascript:alert(1)"));
    }

    [Fact]
    public void BemNaoPodeSerResponsavelPorSiMesmo()
    {
        var bem = Bem.Criar(new NomeBem("Notebook"), TipoBem.Eletronico);
        var pessoa = Bem.Criar(new NomeBem("Ana"), TipoBem.Pessoa);

        bem.DefinirResponsavel(pessoa.Id);
        bem.ResponsavelId.ShouldBe(pessoa.Id);
        bem.DefinirResponsavel(null);
        bem.ResponsavelId.ShouldBeNull();
        Should.Throw<ArgumentException>(() => bem.DefinirResponsavel(bem.Id));
    }

    [Fact]
    public void EletronicoDeveDepreciarVintePorCentoAoAnoAteOResidual()
    {
        var compra = new DateOnly(2020, 1, 1);
        var bem = Bem.Criar(new NomeBem("Notebook"), TipoBem.Eletronico, compra, Dinheiro.De(10_000m, "BRL"));

        Depreciacao.ValorEstimado(bem, compra)!.Valor.ShouldBe(10_000m);
        Depreciacao.ValorEstimado(bem, compra.AddYears(2))!.Valor.ShouldBeInRange(5_990m, 6_010m);
        Depreciacao.ValorEstimado(bem, compra.AddYears(10))!.Valor.ShouldBe(1_000m);
        Depreciacao.ValorEstimado(bem, compra.AddDays(-1)).ShouldBeNull();
    }

    [Fact]
    public void ImovelSemDataEPessoaTemRegrasProprias()
    {
        var imovel = Bem.Criar(new NomeBem("Casa"), TipoBem.Imovel, new DateOnly(2010, 5, 1), Dinheiro.De(300_000m, "BRL"));
        var semData = Bem.Criar(new NomeBem("Sofá"), TipoBem.Movel);
        semData.AtualizarCompra(null, Dinheiro.De(2_000m, "BRL"));
        var pessoa = Bem.Criar(new NomeBem("Ana"), TipoBem.Pessoa);

        Depreciacao.ValorEstimado(imovel, new DateOnly(2026, 1, 1))!.Valor.ShouldBe(300_000m);
        Depreciacao.ValorEstimado(semData, new DateOnly(2026, 1, 1))!.Valor.ShouldBe(2_000m);
        Depreciacao.ValorEstimado(pessoa, new DateOnly(2026, 1, 1)).ShouldBeNull();
    }

    [Theory]
    [InlineData("A", "A", "A", AcaoSincronizacao.NadaAFazer)]
    [InlineData("A", "B", "A", AcaoSincronizacao.Enviar)]
    [InlineData("A", "A", "B", AcaoSincronizacao.Receber)]
    [InlineData("A", "B", "C", AcaoSincronizacao.Conflito)]
    [InlineData(null, "B", "C", AcaoSincronizacao.Conflito)]
    public void SincronizacaoDeveDecidirPelaUltimaImpressaoConhecida(string? ultima, string local, string remota, AcaoSincronizacao esperada)
    {
        var configuracao = ConfiguracaoSincronizacao.Restaurar(@"C:\Nuvem", true, ultima, null);

        configuracao.Decidir(local, remota).ShouldBe(esperada);
        configuracao.Decidir(local, null).ShouldBe(AcaoSincronizacao.Enviar);
    }

    [Fact]
    public void TrocarDePastaDeSincronizacaoEsqueceAUltimaImpressao()
    {
        var configuracao = ConfiguracaoSincronizacao.Padrao();
        configuracao.Ativar(@"C:\Nuvem\A");
        configuracao.RegistrarSincronizacao("X", DateTimeOffset.Now);

        configuracao.Ativar(@"C:\Nuvem\B");

        configuracao.UltimaImpressao.ShouldBeNull();
        configuracao.Ativa.ShouldBeTrue();
    }
}
