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
        string observacoes) =>
        new(id, nome, tipo, status, compradoEm, precoCompra, observacoes);

    public void Renomear(NomeBem novoNome)
    {
        Nome = novoNome ?? throw new ArgumentNullException(nameof(novoNome), "nome do bem é obrigatório");
    }

    public void AlterarTipo(TipoBem novoTipo)
    {
        Tipo = novoTipo;
    }

    public void AtualizarCompra(DateOnly compradoEm, Dinheiro? precoCompra)
    {
        CompradoEm = Validacao.ExigirDataNaoFutura(compradoEm, "data de compra", DateOnly.FromDateTime(DateTime.Now));
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

    public void Ativar()
    {
        ExigirAbertoParaMudanca();
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
