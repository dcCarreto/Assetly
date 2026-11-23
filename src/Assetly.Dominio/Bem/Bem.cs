using Assetly.Dominio.Compartilhado;

namespace Assetly.Dominio.Bem;

public sealed class Bem
{
    public BemId Id { get; }
    public NomeBem Nome { get; private set; }
    public TipoBem Tipo { get; private set; }
    public StatusBem Status { get; private set; }
    public DateOnly? CompradoEm { get; private set; }
    public Dinheiro? PrecoCompra { get; private set; }
    public string Observacoes { get; private set; }

    /// <summary>Modo família: pessoa (bem do tipo <see cref="TipoBem.Pessoa"/>) responsável por este bem.
    /// Nulo = sem responsável definido.</summary>
    public BemId? ResponsavelId { get; private set; }

    private Bem(
        BemId id,
        NomeBem nome,
        TipoBem tipo,
        StatusBem status,
        DateOnly? compradoEm,
        Dinheiro? precoCompra,
        string? observacoes)
    {
        Id = id;
        Nome = nome ?? throw new ArgumentNullException(nameof(nome), "nome do bem é obrigatório");
        Tipo = tipo;
        Status = status;
        CompradoEm = compradoEm;
        PrecoCompra = precoCompra;
        Observacoes = Validacao.TextoOpcional(observacoes, "observações do bem", 500);
    }

    public static Bem Criar(NomeBem nome, TipoBem tipo) =>
        new(BemId.Novo(), nome, tipo, StatusBem.Ativo, null, null, string.Empty);

    public static Bem Criar(NomeBem nome, TipoBem tipo, DateOnly compradoEm, Dinheiro? precoCompra)
    {
        var dataCompra = Validacao.ExigirDataNaoFutura(compradoEm, "data de compra", DateOnly.FromDateTime(DateTime.Now));
        return new Bem(BemId.Novo(), nome, tipo, StatusBem.Ativo, dataCompra, precoCompra, string.Empty);
    }

    public static Bem Restaurar(
        BemId id,
        NomeBem nome,
        TipoBem tipo,
        StatusBem status,
        DateOnly? compradoEm,
        Dinheiro? precoCompra,
        string observacoes,
        BemId? responsavelId = null) =>
        new(id, nome, tipo, status, compradoEm, precoCompra, observacoes) { ResponsavelId = responsavelId };

    public void Renomear(NomeBem novoNome)
    {
        Nome = novoNome ?? throw new ArgumentNullException(nameof(novoNome), "nome do bem é obrigatório");
    }

    public void AlterarTipo(TipoBem novoTipo)
    {
        Tipo = novoTipo;
    }

    /// <summary>Define a pessoa responsável (a validação de que é uma Pessoa fica no caso de uso, que
    /// enxerga os outros bens).</summary>
    public void DefinirResponsavel(BemId? responsavelId)
    {
        if (responsavelId is not null && responsavelId.Value.Equals(Id))
        {
            throw new ArgumentException("um bem não pode ser responsável por ele mesmo");
        }

        ResponsavelId = responsavelId;
    }

    public void AtualizarCompra(DateOnly compradoEm, Dinheiro? precoCompra) =>
        AtualizarCompra((DateOnly?)compradoEm, precoCompra);

    /// <summary>Data e preço de compra são independentes e opcionais: é comum saber quanto custou um
    /// item herdado ou antigo sem lembrar a data (e vice-versa). Nulo limpa o valor; data, se
    /// informada, não pode estar no futuro.</summary>
    public void AtualizarCompra(DateOnly? compradoEm, Dinheiro? precoCompra)
    {
        CompradoEm = compradoEm is null
            ? null
            : Validacao.ExigirDataNaoFutura(compradoEm, "data de compra", DateOnly.FromDateTime(DateTime.Now));
        PrecoCompra = precoCompra;
    }

    public void AtualizarObservacoes(string? observacoes)
    {
        Observacoes = Validacao.TextoOpcional(observacoes, "observações do bem", 500);
    }

    public void MarcarEmManutencao()
    {
        ExigirAbertoParaMudanca();
        Status = StatusBem.EmManutencao;
    }

    /// <summary>Reativa o bem. Um bem arquivado pode voltar a ser ativo (desarquivar); descartado é
    /// um estado final, porque o item físico deixou de existir.</summary>
    public void Ativar()
    {
        if (Status == StatusBem.Descartado)
        {
            throw new InvalidOperationException("bem descartado não pode ser reativado");
        }

        Status = StatusBem.Ativo;
    }

    public void Descartar()
    {
        Status = StatusBem.Descartado;
    }

    public void Arquivar()
    {
        Status = StatusBem.Arquivado;
    }

    private void ExigirAbertoParaMudanca()
    {
        if (Status is StatusBem.Arquivado or StatusBem.Descartado)
        {
            throw new InvalidOperationException("bem arquivado ou descartado não pode ser alterado");
        }
    }
}
