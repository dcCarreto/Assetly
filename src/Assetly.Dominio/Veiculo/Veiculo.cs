using System.Text.RegularExpressions;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;

namespace Assetly.Dominio.Veiculo;

/// <summary>Dados da ficha de um veículo, usados para criar e atualizar <see cref="Veiculo"/>.</summary>
public sealed record DadosVeiculo(
    string? Placa,
    string? Marca,
    string? Modelo,
    int? AnoFabricacao,
    int? AnoModelo,
    string? Cor,
    string? Renavam,
    string? Chassi,
    TipoCombustivel Combustivel,
    int Quilometragem,
    int? ProximaRevisaoKm,
    DateOnly? ProximaRevisaoEm,
    DateOnly? VencimentoLicenciamento,
    string? Seguradora,
    string? Apolice,
    DateOnly? VencimentoSeguro);

/// <summary>
/// Ficha de um automóvel (ou moto, caminhão...). É um complemento 1:1 de um <see cref="Bem.Bem"/> do tipo
/// veículo: nome, status, compra e documentos continuam no bem; aqui ficam identificação (placa,
/// RENAVAM, chassi), quilometragem, plano de revisão, licenciamento e seguro.
/// </summary>
public sealed partial class Veiculo
{
    public const int DiasAvisoVencimento = 30;
    public const int KmAvisoRevisao = 1000;
    public const int DiasAvisoRevisao = 15;

    public BemId BemId { get; }
    public string Placa { get; private set; } = string.Empty;
    public string Marca { get; private set; } = string.Empty;
    public string Modelo { get; private set; } = string.Empty;
    public int? AnoFabricacao { get; private set; }
    public int? AnoModelo { get; private set; }
    public string Cor { get; private set; } = string.Empty;
    public string Renavam { get; private set; } = string.Empty;
    public string Chassi { get; private set; } = string.Empty;
    public TipoCombustivel Combustivel { get; private set; }
    public int Quilometragem { get; private set; }
    public int? ProximaRevisaoKm { get; private set; }
    public DateOnly? ProximaRevisaoEm { get; private set; }
    public DateOnly? VencimentoLicenciamento { get; private set; }
    public string Seguradora { get; private set; } = string.Empty;
    public string Apolice { get; private set; } = string.Empty;
    public DateOnly? VencimentoSeguro { get; private set; }

    private Veiculo(BemId bemId, DadosVeiculo dados)
    {
        BemId = bemId;
        Aplicar(dados);
    }

    public static Veiculo Criar(BemId bemId, DadosVeiculo dados) => new(bemId, dados ?? throw new ArgumentNullException(nameof(dados)));

    public static Veiculo Restaurar(BemId bemId, DadosVeiculo dados) => new(bemId, dados);

    /// <summary>Placa no formato de exibição: "ABC-1234" (padrão antigo) ou "ABC1D23" (Mercosul).</summary>
    public string PlacaFormatada => Placa.Length == 7 && Placa[3..].All(char.IsDigit) ? $"{Placa[..3]}-{Placa[3..]}" : Placa;

    /// <summary>"Marca Modelo" (ou o que estiver preenchido).</summary>
    public string Descricao => string.Join(' ', new[] { Marca, Modelo }.Where(parte => parte.Length > 0));

    public DadosVeiculo Dados() => new(Placa, Marca, Modelo, AnoFabricacao, AnoModelo, Cor, Renavam, Chassi, Combustivel,
        Quilometragem, ProximaRevisaoKm, ProximaRevisaoEm, VencimentoLicenciamento, Seguradora, Apolice, VencimentoSeguro);

    /// <summary>Atualiza a ficha inteira. A quilometragem pode ser corrigida para baixo aqui (correção de
    /// digitação); o registro do dia a dia usa <see cref="RegistrarQuilometragem"/>, que só aceita avanço.</summary>
    public void Atualizar(DadosVeiculo dados) => Aplicar(dados ?? throw new ArgumentNullException(nameof(dados)));

    public void RegistrarQuilometragem(int quilometragem)
    {
        if (quilometragem < Quilometragem)
        {
            throw new ArgumentException($"a quilometragem informada ({quilometragem:N0} km) é menor que a atual ({Quilometragem:N0} km). Para corrigir um erro de digitação, edite a ficha do veículo.");
        }

        Quilometragem = quilometragem;
    }

    /// <summary>Avança a quilometragem se o valor for maior que o atual (ex.: km informado num abastecimento).</summary>
    public bool AvancarQuilometragem(int quilometragem)
    {
        if (quilometragem <= Quilometragem)
        {
            return false;
        }

        Quilometragem = quilometragem;
        return true;
    }

    private void Aplicar(DadosVeiculo dados)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var placa = NormalizarPlaca(dados.Placa);
        var renavam = SoDigitos(dados.Renavam);
        var chassi = (dados.Chassi ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", string.Empty);

        if (renavam.Length > 0 && renavam.Length is < 9 or > 11)
        {
            throw new ArgumentException("RENAVAM deve ter de 9 a 11 dígitos");
        }

        if (chassi.Length > 0 && !RegexChassi().IsMatch(chassi))
        {
            throw new ArgumentException("chassi deve ter 17 caracteres (letras e números, sem I, O ou Q)");
        }

        ValidarAno(dados.AnoFabricacao, "ano de fabricação", hoje.Year + 1);
        ValidarAno(dados.AnoModelo, "ano do modelo", hoje.Year + 2);
        if (dados.AnoFabricacao is not null && dados.AnoModelo is not null
            && (dados.AnoModelo < dados.AnoFabricacao || dados.AnoModelo > dados.AnoFabricacao + 1))
        {
            throw new ArgumentException("ano do modelo deve ser igual ao ano de fabricação ou o ano seguinte");
        }

        if (dados.Quilometragem < 0)
        {
            throw new ArgumentException("quilometragem não pode ser negativa");
        }

        if (dados.ProximaRevisaoKm is < 0)
        {
            throw new ArgumentException("quilometragem da próxima revisão não pode ser negativa");
        }

        Placa = placa;
        Marca = Validacao.TextoOpcional(dados.Marca, "marca", 60);
        Modelo = Validacao.TextoOpcional(dados.Modelo, "modelo", 80);
        AnoFabricacao = dados.AnoFabricacao;
        AnoModelo = dados.AnoModelo;
        Cor = Validacao.TextoOpcional(dados.Cor, "cor", 40);
        Renavam = renavam;
        Chassi = chassi;
        Combustivel = dados.Combustivel;
        Quilometragem = dados.Quilometragem;
        ProximaRevisaoKm = dados.ProximaRevisaoKm;
        ProximaRevisaoEm = dados.ProximaRevisaoEm;
        VencimentoLicenciamento = dados.VencimentoLicenciamento;
        Seguradora = Validacao.TextoOpcional(dados.Seguradora, "seguradora", 120);
        Apolice = Validacao.TextoOpcional(dados.Apolice, "apólice", 60);
        VencimentoSeguro = dados.VencimentoSeguro;
    }

    private static string NormalizarPlaca(string? placa)
    {
        var normalizada = (placa ?? string.Empty).Trim().ToUpperInvariant().Replace("-", string.Empty).Replace(" ", string.Empty);
        if (normalizada.Length > 0 && !RegexPlaca().IsMatch(normalizada))
        {
            throw new ArgumentException("placa inválida. Use o padrão antigo (ABC-1234) ou Mercosul (ABC1D23)");
        }

        return normalizada;
    }

    private static string SoDigitos(string? valor) => new((valor ?? string.Empty).Where(char.IsDigit).ToArray());

    private static void ValidarAno(int? ano, string campo, int maximo)
    {
        if (ano is not null && (ano < 1900 || ano > maximo))
        {
            throw new ArgumentException($"{campo} deve estar entre 1900 e {maximo}");
        }
    }

    [GeneratedRegex("^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$")]
    private static partial Regex RegexPlaca();

    [GeneratedRegex("^[A-HJ-NPR-Z0-9]{17}$")]
    private static partial Regex RegexChassi();
}
