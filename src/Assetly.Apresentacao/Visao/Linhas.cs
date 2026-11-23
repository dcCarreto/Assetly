using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Manutencao;
using Assetly.Dominio.Saude;

namespace Assetly.Apresentacao.Visao;

// Linhas de exibição das tabelas. Os membros "...Ordenacao" são as chaves usadas quando o usuário
// clica no cabeçalho de uma coluna de data/valor: ordenar o texto "dd/MM/aaaa" ou "R$ 1.234,00"
// daria uma ordem errada.

internal sealed record BemLinha(
    BemId Id, string Nome, string Tipo, string Status, string StatusClasse, string LinhaClasse,
    string Compra, string Valor, string Responsavel, string Observacoes, Bem Origem)
{
    public DateOnly? CompraOrdenacao => Origem.CompradoEm;
    public decimal? ValorOrdenacao => Origem.PrecoCompra?.Valor;
}

internal sealed record GarantiaLinha(
    GarantiaId Id, string Bem, string Tipo, string Fornecedor, string Inicio, string Fim,
    string Status, string StatusClasse, string LinhaClasse, string Suporte, Garantia Origem)
{
    public DateOnly InicioOrdenacao => Origem.Periodo.IniciaEm;
    public DateOnly FimOrdenacao => Origem.Periodo.TerminaEm;
}

internal sealed record ManutencaoLinha(
    ManutencaoId Id, string Bem, string Tipo, string Descricao, string Agendada, string Concluida,
    string Custo, string Status, string StatusClasse, string LinhaClasse, Manutencao Origem)
{
    public DateOnly AgendadaOrdenacao => Origem.AgendadaPara;
    public DateOnly? ConcluidaOrdenacao => Origem.ConcluidaEm;
    public decimal? CustoOrdenacao => Origem.Custo?.Valor;
}

internal sealed record DocumentoLinha(
    DocumentoId Id, string Bem, string Tipo, string Nome, string ArquivoLocal, string Registrado,
    string Status, string StatusClasse, string LinhaClasse, string NotaEletronica, Documento Origem)
{
    public DateOnly RegistradoOrdenacao => Origem.RegistradoEm;
}

internal sealed record AlertaLinha(
    AlertaId Id, string Bem, string Tipo, string Severidade, string SeveridadeClasse, string Mensagem,
    string Criado, string Prazo, string Status, string StatusClasse, string LinhaClasse, Alerta Origem)
{
    public int SeveridadeOrdenacao => Origem.Severidade switch
    {
        SeveridadeAlerta.Critico => 0,
        SeveridadeAlerta.Aviso => 1,
        _ => 2
    };

    public DateOnly CriadoOrdenacao => Origem.CriadoEm;
    public DateOnly? PrazoOrdenacao => Origem.PrazoEm;
}

internal sealed record SaudeLinha(
    SaudeId Id, string Bem, string Tipo, string Fornecedor, string Inicio, string Fim,
    string Status, string StatusClasse, string LinhaClasse, string Contato, Saude Origem)
{
    public DateOnly InicioOrdenacao => Origem.Periodo.IniciaEm;
    public DateOnly FimOrdenacao => Origem.Periodo.TerminaEm;
}

internal sealed record GastoSaudeLinha(
    GastoSaudeId Id, string Bem, string Tipo, string Descricao, string Data, string Valor, GastoSaude Origem)
{
    public DateOnly DataOrdenacao => Origem.Data;
    public decimal ValorOrdenacao => Origem.Valor.Valor;
}
