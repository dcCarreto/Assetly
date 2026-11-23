using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;

namespace Assetly.Dominio.Imovel;

/// <summary>Dados da ficha de um imóvel, usados para criar e atualizar <see cref="Imovel"/>.</summary>
public sealed record DadosImovel(
    TipoImovel Tipo,
    SituacaoImovel Situacao,
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? Cidade,
    string? Uf,
    string? Cep,
    decimal? AreaM2,
    int? Quartos,
    int? Banheiros,
    int? Vagas,
    string? Matricula,
    string? InscricaoIptu,
    Dinheiro? ValorCondominio,
    string? Seguradora,
    string? Apolice,
    DateOnly? VencimentoSeguro,
    string? Inquilino,
    Dinheiro? ValorAluguel,
    DateOnly? FimContratoAluguel);

/// <summary>
/// Ficha de um imóvel — complemento 1:1 de um <see cref="Bem.Bem"/> do tipo imóvel: endereço, características,
/// registro (matrícula e inscrição de IPTU), condomínio, seguro residencial e, quando alugado para
/// terceiros, inquilino, valor e fim do contrato.
/// </summary>
public sealed class Imovel
{
    public const int DiasAvisoSeguro = 30;
    public const int DiasAvisoContrato = 60;

    private static readonly HashSet<string> UfsValidas =
    [
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI",
        "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"
    ];

    public BemId BemId { get; }
    public TipoImovel Tipo { get; private set; }
    public SituacaoImovel Situacao { get; private set; }
    public string Logradouro { get; private set; } = string.Empty;
    public string Numero { get; private set; } = string.Empty;
    public string Complemento { get; private set; } = string.Empty;
    public string Bairro { get; private set; } = string.Empty;
    public string Cidade { get; private set; } = string.Empty;
    public string Uf { get; private set; } = string.Empty;
    public string Cep { get; private set; } = string.Empty;
    public decimal? AreaM2 { get; private set; }
    public int? Quartos { get; private set; }
    public int? Banheiros { get; private set; }
    public int? Vagas { get; private set; }
    public string Matricula { get; private set; } = string.Empty;
    public string InscricaoIptu { get; private set; } = string.Empty;
    public Dinheiro? ValorCondominio { get; private set; }
    public string Seguradora { get; private set; } = string.Empty;
    public string Apolice { get; private set; } = string.Empty;
    public DateOnly? VencimentoSeguro { get; private set; }
    public string Inquilino { get; private set; } = string.Empty;
    public Dinheiro? ValorAluguel { get; private set; }
    public DateOnly? FimContratoAluguel { get; private set; }

    private Imovel(BemId bemId, DadosImovel dados)
    {
        BemId = bemId;
        Aplicar(dados);
    }

    public static Imovel Criar(BemId bemId, DadosImovel dados) => new(bemId, dados ?? throw new ArgumentNullException(nameof(dados)));

    public static Imovel Restaurar(BemId bemId, DadosImovel dados) => new(bemId, dados);

    public void Atualizar(DadosImovel dados) => Aplicar(dados ?? throw new ArgumentNullException(nameof(dados)));

    public bool Alugado => Situacao == SituacaoImovel.AlugadoParaTerceiros;

    /// <summary>CEP no formato 00000-000 (vazio se não informado).</summary>
    public string CepFormatado => Cep.Length == 8 ? $"{Cep[..5]}-{Cep[5..]}" : Cep;

    /// <summary>Endereço em uma linha: "Rua X, 100, apto 12 — Bairro, Cidade/UF, CEP 00000-000".</summary>
    public string EnderecoCompleto
    {
        get
        {
            var rua = string.Join(", ", new[] { Logradouro, Numero, Complemento }.Where(p => p.Length > 0));
            var cidade = Cidade.Length > 0 && Uf.Length > 0 ? $"{Cidade}/{Uf}" : Cidade + Uf;
            var resto = string.Join(", ", new[] { Bairro, cidade, Cep.Length > 0 ? $"CEP {CepFormatado}" : string.Empty }.Where(p => p.Length > 0));
            return string.Join(" — ", new[] { rua, resto }.Where(p => p.Length > 0));
        }
    }

    public DadosImovel Dados() => new(Tipo, Situacao, Logradouro, Numero, Complemento, Bairro, Cidade, Uf, Cep, AreaM2, Quartos,
        Banheiros, Vagas, Matricula, InscricaoIptu, ValorCondominio, Seguradora, Apolice, VencimentoSeguro, Inquilino, ValorAluguel, FimContratoAluguel);

    private void Aplicar(DadosImovel dados)
    {
        var uf = (dados.Uf ?? string.Empty).Trim().ToUpperInvariant();
        if (uf.Length > 0 && !UfsValidas.Contains(uf))
        {
            throw new ArgumentException("UF inválida. Use a sigla do estado (ex.: SP, RJ, MG)");
        }

        var cep = new string((dados.Cep ?? string.Empty).Where(char.IsDigit).ToArray());
        if (cep.Length > 0 && cep.Length != 8)
        {
            throw new ArgumentException("CEP deve ter 8 dígitos (ex.: 01310-100)");
        }

        if (dados.AreaM2 is <= 0)
        {
            throw new ArgumentException("área deve ser maior que zero");
        }

        foreach (var (valor, campo) in new[] { (dados.Quartos, "quartos"), (dados.Banheiros, "banheiros"), (dados.Vagas, "vagas") })
        {
            if (valor is < 0 or > 999)
            {
                throw new ArgumentException($"número de {campo} inválido");
            }
        }

        Tipo = dados.Tipo;
        Situacao = dados.Situacao;
        Logradouro = Validacao.TextoOpcional(dados.Logradouro, "logradouro", 160);
        Numero = Validacao.TextoOpcional(dados.Numero, "número", 20);
        Complemento = Validacao.TextoOpcional(dados.Complemento, "complemento", 80);
        Bairro = Validacao.TextoOpcional(dados.Bairro, "bairro", 80);
        Cidade = Validacao.TextoOpcional(dados.Cidade, "cidade", 80);
        Uf = uf;
        Cep = cep;
        AreaM2 = dados.AreaM2 is null ? null : Math.Round(dados.AreaM2.Value, 2, MidpointRounding.AwayFromZero);
        Quartos = dados.Quartos;
        Banheiros = dados.Banheiros;
        Vagas = dados.Vagas;
        Matricula = Validacao.TextoOpcional(dados.Matricula, "matrícula", 60);
        InscricaoIptu = Validacao.TextoOpcional(dados.InscricaoIptu, "inscrição do IPTU", 60);
        ValorCondominio = dados.ValorCondominio;
        Seguradora = Validacao.TextoOpcional(dados.Seguradora, "seguradora", 120);
        Apolice = Validacao.TextoOpcional(dados.Apolice, "apólice", 60);
        VencimentoSeguro = dados.VencimentoSeguro;
        Inquilino = Validacao.TextoOpcional(dados.Inquilino, "inquilino", 120);
        ValorAluguel = dados.ValorAluguel;
        FimContratoAluguel = dados.FimContratoAluguel;
    }
}
