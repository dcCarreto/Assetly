using System.Windows;
using System.Windows.Controls;
using Assetly.Dominio.Bem;

namespace Assetly.Apresentacao.Visao;

/// <summary>Item genérico de combo: valor + texto exibido.</summary>
internal sealed record ItemValor<T>(T Valor, string Texto)
{
    public override string ToString() => Texto;
}

/// <summary>Helpers dos módulos de Automóveis e Imóveis.</summary>
internal static class ApoioFichas
{
    internal const string MoedaPadrao = "BRL";

    internal static ComboBox ComboItens<T>(IReadOnlyList<ItemValor<T>> itens, Func<T, bool>? selecionar = null)
    {
        var combo = new ComboBox
        {
            Style = (Style)Application.Current.Resources["ComboDialogoStyle"],
            ItemsSource = itens,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        combo.SelectedItem = (selecionar is null ? null : itens.FirstOrDefault(i => selecionar(i.Valor))) ?? itens.FirstOrDefault();
        return combo;
    }

    internal static T ValorItem<T>(ComboBox combo, string mensagem) =>
        combo.SelectedItem is ItemValor<T> item ? item.Valor : throw new ArgumentException(mensagem);

    /// <summary>Combo de vínculo de uma ficha nova: "criar bem junto" ou um bem existente do tipo certo
    /// que ainda não tem ficha. O valor nulo significa "criar bem novo".</summary>
    internal static ComboBox ComboVinculo(ContextoAplicacao contexto, TipoBem tipo, ISet<BemId> jaComFicha, string textoNovo)
    {
        var itens = new List<ItemValor<BemId?>> { new(null, textoNovo) };
        itens.AddRange(contexto.ListarBens().Executar()
            .Where(b => b.Tipo == tipo && b.Status is StatusBem.Ativo or StatusBem.EmManutencao && !jaComFicha.Contains(b.Id))
            .Select(b => new ItemValor<BemId?>(b.Id, $"Usar bem já cadastrado: {b.Nome.Valor}")));
        return ComboItens(itens);
    }

    /// <summary>Situação resumida de prazos: "Pendência" (algo vencido), "Atenção" (algo perto de vencer) ou "Em dia".</summary>
    /// <summary>Junta prazos (licenciamento, seguro, contrato...) e contas do bem numa contagem de vencidos/próximos e num
    /// texto com o motivo — mostrado ao passar o mouse na situação.</summary>
    internal sealed class Pendencias(DateOnly hoje)
    {
        private readonly List<string> _motivos = [];

        internal int Vencidos { get; private set; }
        internal int Proximos { get; private set; }
        internal string Motivo => _motivos.Count == 0 ? "Nada vencendo." : string.Join(" · ", _motivos);

        internal void Prazo(string nome, DateOnly? data, int diasAviso)
        {
            if (data is null)
            {
                return;
            }

            var dias = data.Value.DayNumber - hoje.DayNumber;
            if (dias < 0)
            {
                Vencidos++;
                _motivos.Add($"{nome} venceu em {Formatador.Data(data)}");
            }
            else if (dias <= diasAviso)
            {
                Proximos++;
                _motivos.Add(dias == 0 ? $"{nome} vence hoje" : $"{nome} vence em {dias} d");
            }
        }

        internal void Vencido(string motivo)
        {
            Vencidos++;
            _motivos.Add(motivo);
        }

        internal void Contas(IEnumerable<Dominio.Despesa.Despesa> despesas)
        {
            var lista = despesas.ToList();
            var vencidas = lista.Count(d => d.StatusEm(hoje) == Dominio.Despesa.StatusDespesa.Vencida);
            var aVencer = lista.Count(d => d.StatusEm(hoje) == Dominio.Despesa.StatusDespesa.AVencer);
            if (vencidas > 0)
            {
                Vencidos += vencidas;
                _motivos.Add(vencidas == 1 ? "1 conta vencida" : $"{vencidas} contas vencidas");
            }

            if (aVencer > 0)
            {
                Proximos += aVencer;
                _motivos.Add(aVencer == 1 ? "1 conta vence em até 7 dias" : $"{aVencer} contas vencem em até 7 dias");
            }
        }
    }

    internal static (string Texto, string Classe, string Linha) Situacao(int vencidos, int proximos) =>
        vencidos > 0 ? ("Pendência", "etiqueta-pendencia", "linha-perigo")
        : proximos > 0 ? ("Atenção", "etiqueta-atencao", "linha-aviso")
        : ("Em dia", "etiqueta-em-dia", "linha-ok");

    /// <summary>Texto curto de um prazo: "15/03/2027" ou "15/03/2027 (vencido)" / "(em 12 dias)".</summary>
    internal static string Prazo(DateOnly? data, DateOnly hoje, int diasAviso)
    {
        if (data is null)
        {
            return Formatador.Data(null);
        }

        var dias = data.Value.DayNumber - hoje.DayNumber;
        var sufixo = dias < 0 ? " (vencido)" : dias <= diasAviso ? (dias == 0 ? " (hoje)" : $" (em {dias} d)") : string.Empty;
        return Formatador.Data(data) + sufixo;
    }
}
