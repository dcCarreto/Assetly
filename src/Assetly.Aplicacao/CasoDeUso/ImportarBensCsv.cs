using System.Globalization;
using System.Text;
using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Repositorio;
using Assetly.Dominio.Bem;

namespace Assetly.Aplicacao.CasoDeUso;

public sealed record ResultadoImportacaoCsv(int Criados, int JaCadastrados);

/// <summary>
/// Cadastro em massa de bens a partir de uma planilha CSV (separada por ";" ou ","), com cabeçalho. Reconhece as colunas
/// pelo nome — nome (obrigatória), tipo, status, data de compra, preço, moeda, marca, modelo, número de série, loja,
/// localização, valor atual, responsável e observações — inclusive o CSV que a própria tabela de Bens exporta. Todas as
/// linhas são validadas antes de gravar: com qualquer erro, nada é importado. Uma linha igual a um bem já cadastrado
/// (mesmo nome, data e preço de compra) é pulada, para importar a mesma planilha de novo não duplicar.
/// </summary>
public sealed class ImportarBensCsv(IRepositorioBem repositorioBem)
{
    private const int MaximoErrosMostrados = 8;
    private readonly IRepositorioBem _repositorioBem = repositorioBem ?? throw new ArgumentNullException(nameof(repositorioBem));

    private static readonly Dictionary<string, string> Colunas = new()
    {
        ["nome"] = "nome", ["bem"] = "nome", ["item"] = "nome",
        ["tipo"] = "tipo", ["status"] = "status", ["situacao"] = "status",
        ["compra"] = "data", ["datadecompra"] = "data", ["datacompra"] = "data", ["compradoem"] = "data", ["data"] = "data",
        ["valor"] = "preco", ["preco"] = "preco", ["precodecompra"] = "preco", ["valordecompra"] = "preco", ["precocompra"] = "preco",
        ["moeda"] = "moeda", ["marca"] = "marca", ["modelo"] = "modelo",
        ["numerodeserie"] = "serie", ["numeroserie"] = "serie", ["serie"] = "serie", ["ns"] = "serie",
        ["loja"] = "loja", ["localizacao"] = "local", ["local"] = "local", ["comodo"] = "local",
        ["valoratual"] = "valoratual", ["responsavel"] = "responsavel", ["observacoes"] = "obs", ["obs"] = "obs"
    };

    public ResultadoImportacaoCsv Executar(string conteudo)
    {
        var linhas = LerCsv(conteudo ?? string.Empty);
        if (linhas.Count == 0)
        {
            throw new ArgumentException("a planilha está vazia.");
        }

        var indices = new Dictionary<string, int>();
        for (var i = 0; i < linhas[0].Count; i++)
        {
            if (Colunas.TryGetValue(Normalizar(linhas[0][i]), out var chave))
            {
                indices.TryAdd(chave, i);
            }
        }

        if (!indices.ContainsKey("nome"))
        {
            throw new ArgumentException("a primeira linha da planilha precisa ter os nomes das colunas, com uma coluna \"Nome\".");
        }

        var existentes = _repositorioBem.Listar();
        var pessoas = existentes.Where(b => b.Tipo == TipoBem.Pessoa)
            .GroupBy(b => Normalizar(b.Nome.Valor)).ToDictionary(g => g.Key, g => g.First().Id);
        var novos = new List<Bem>();
        var erros = new List<string>();
        for (var numero = 2; numero <= linhas.Count; numero++)
        {
            var linha = linhas[numero - 1];
            string? Valor(string chave) =>
                indices.TryGetValue(chave, out var i) && i < linha.Count && linha[i].Trim() is { Length: > 0 } texto && texto != "—" ? texto : null;

            if (linha.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            try
            {
                var bem = Bem.Criar(new NomeBem(Valor("nome")), Valor("tipo") is { } tipo ? Enumerado<TipoBem>(tipo, "tipo") : TipoBem.Outro);
                var moeda = Valor("moeda") ?? "BRL";
                bem.AtualizarCompra(Valor("data") is { } data ? Data(data) : null, ApoioCasosUso.DinheiroOpcional(ValorMonetario.InterpretarOpcional(Valor("preco")), moeda));
                bem.AtualizarInventario(new InventarioBem(Valor("marca"), Valor("modelo"), Valor("serie"), Valor("loja"), Valor("local")));
                bem.AtualizarObservacoes(Valor("obs"));
                if (ValorMonetario.InterpretarOpcional(Valor("valoratual")) is { } atual)
                {
                    bem.InformarValorAtual(Dominio.Compartilhado.Dinheiro.De(atual, moeda), DateOnly.FromDateTime(DateTime.Now));
                }

                if (Valor("responsavel") is { } responsavel && pessoas.TryGetValue(Normalizar(responsavel), out var pessoa))
                {
                    bem.DefinirResponsavel(pessoa);
                }

                switch (Valor("status") is { } status ? Enumerado<StatusBem>(status, "status") : StatusBem.Ativo)
                {
                    case StatusBem.EmManutencao: bem.MarcarEmManutencao(); break;
                    case StatusBem.Arquivado: bem.Arquivar(); break;
                    case StatusBem.Descartado: bem.Descartar(); break;
                }

                novos.Add(bem);
            }
            catch (Exception excecao) when (excecao is ArgumentException or InvalidOperationException or FormatException)
            {
                erros.Add($"linha {numero}: {excecao.Message}");
            }
        }

        if (erros.Count > 0)
        {
            var lista = string.Join("\n", erros.Take(MaximoErrosMostrados)) + (erros.Count > MaximoErrosMostrados ? $"\n… e mais {erros.Count - MaximoErrosMostrados}" : string.Empty);
            throw new ArgumentException($"a planilha tem {erros.Count} linha(s) com problema — nada foi importado. Corrija e importe de novo:\n\n{lista}");
        }

        var chaves = existentes.Select(Chave).ToHashSet();
        var criados = 0;
        foreach (var bem in novos)
        {
            if (chaves.Add(Chave(bem)))
            {
                _repositorioBem.Salvar(bem);
                criados++;
            }
        }

        return new ResultadoImportacaoCsv(criados, novos.Count - criados);
    }

    private static string Chave(Bem bem) => $"{Normalizar(bem.Nome.Valor)}|{bem.CompradoEm}|{bem.PrecoCompra?.Valor}|{bem.PrecoCompra?.CodigoMoeda}";

    private static TEnum Enumerado<TEnum>(string texto, string campo) where TEnum : struct, Enum
    {
        var normalizado = Normalizar(texto);
        foreach (var valor in Enum.GetValues<TEnum>())
        {
            var nome = Normalizar(valor.ToString());
            // "Pessoa (titular)" → "pessoatitular"; os rótulos da tela começam pelo nome do valor.
            if (normalizado == nome || normalizado.StartsWith(nome, StringComparison.Ordinal) && valor.ToString() == "Pessoa")
            {
                return valor;
            }
        }

        throw new ArgumentException($"{campo} desconhecido: \"{texto}\".");
    }

    private static DateOnly Data(string texto)
    {
        string[] formatos = ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yy"];
        return DateOnly.TryParseExact(texto.Trim(), formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data)
            ? data
            : throw new ArgumentException($"data inválida: \"{texto}\" (use dd/mm/aaaa).");
    }

    /// <summary>Minúsculas, sem acentos, sem espaços nem pontuação: "Número de série" → "numerodeserie".</summary>
    internal static string Normalizar(string texto)
    {
        var decomposto = texto.Normalize(NormalizationForm.FormD);
        return new string(decomposto.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }

    /// <summary>CSV com aspas (campo com separador, aspas duplicadas, quebra de linha dentro de aspas). O separador é ";"
    /// (Excel em português) ou "," — o que aparecer mais na primeira linha.</summary>
    internal static List<List<string>> LerCsv(string conteudo)
    {
        conteudo = conteudo.TrimStart('﻿');
        var primeira = conteudo.Split('\n')[0];
        var separador = primeira.Count(c => c == ';') >= primeira.Count(c => c == ',') ? ';' : ',';
        var linhas = new List<List<string>>();
        var linha = new List<string>();
        var campo = new StringBuilder();
        var entreAspas = false;
        for (var i = 0; i < conteudo.Length; i++)
        {
            var c = conteudo[i];
            if (entreAspas)
            {
                if (c == '"' && i + 1 < conteudo.Length && conteudo[i + 1] == '"')
                {
                    campo.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    entreAspas = false;
                }
                else
                {
                    campo.Append(c);
                }
            }
            else if (c == '"')
            {
                entreAspas = true;
            }
            else if (c == separador)
            {
                linha.Add(campo.ToString());
                campo.Clear();
            }
            else if (c == '\n')
            {
                linha.Add(campo.ToString().TrimEnd('\r'));
                campo.Clear();
                linhas.Add(linha);
                linha = [];
            }
            else
            {
                campo.Append(c);
            }
        }

        if (campo.Length > 0 || linha.Count > 0)
        {
            linha.Add(campo.ToString().TrimEnd('\r'));
            linhas.Add(linha);
        }

        return linhas;
    }
}
