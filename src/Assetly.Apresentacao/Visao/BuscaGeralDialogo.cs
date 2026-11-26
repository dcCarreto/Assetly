using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Assetly.Aplicacao.CasoDeUso;

namespace Assetly.Apresentacao.Visao;

/// <summary>Busca em todos os módulos (Ctrl+K): digita, escolhe com as setas e Enter leva ao registro na tela dele.</summary>
internal static class BuscaGeralDialogo
{
    /// <summary>Devolve o registro escolhido, ou nulo se o usuário fechou sem escolher.</summary>
    internal static ResultadoBusca? Escolher(ContextoAplicacao contexto)
    {
        var recursos = Application.Current.Resources;
        var indice = contexto.BuscaGeral().Indexar();
        var caixa = new TextBox
        {
            Style = (Style)recursos["CampoTextoStyle"],
            Tag = "Nome, marca, nº de série, placa, prestador, descrição…",
            FontSize = 14
        };
        System.Windows.Automation.AutomationProperties.SetName(caixa, "Buscar em tudo");
        var lista = new ListBox { Style = (Style)recursos["ListaArquivosStyle"], Height = 340, Margin = new Thickness(0, 10, 0, 0) };
        System.Windows.Automation.AutomationProperties.SetName(lista, "Resultados da busca");
        const string Orientacao = "Digite para buscar em bens, garantias, manutenções, documentos, alertas, saúde, veículos, imóveis e contas.";
        var dica = UiApoio.Texto(Orientacao,
            11.5, FontWeights.Normal, (Brush)recursos["TextMuted"]);
        dica.Margin = new Thickness(0, 8, 0, 0);
        dica.TextWrapping = TextWrapping.Wrap;

        ResultadoBusca? escolhido = null;
        var janela = new Window
        {
            Title = "Buscar em tudo",
            Width = 680,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = DialogoApoio.Posicao(),
            Owner = DialogoApoio.DonoDialogo(),
            ResizeMode = ResizeMode.NoResize
        };

        void Abrir()
        {
            if (lista.SelectedItem is ItemValor<ResultadoBusca> { Valor: not null } item)
            {
                escolhido = item.Valor;
                janela.Close();
            }
        }

        caixa.TextChanged += (_, _) =>
        {
            var resultados = indice.Buscar(caixa.Text);
            lista.ItemsSource = resultados.Select(r => new ItemValor<ResultadoBusca>(r, $"{r.Titulo}    ·    {Rotulo(r.Area)}{(r.Detalhe.Length > 0 ? " — " + r.Detalhe : string.Empty)}")).ToList();
            lista.SelectedIndex = resultados.Count > 0 ? 0 : -1;
            dica.Text = caixa.Text.Trim().Length == 0 ? Orientacao
                : resultados.Count == 0 ? "Nada encontrado."
                : resultados.Count >= BuscaGeral.MaximoResultados ? $"Mostrando os primeiros {BuscaGeral.MaximoResultados} — digite mais para refinar. Enter abre o selecionado."
                : $"{resultados.Count} resultado(s). Enter abre o selecionado; setas mudam a seleção; Esc fecha.";
        };
        caixa.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Down && lista.Items.Count > 0)
            {
                lista.SelectedIndex = Math.Min(lista.SelectedIndex + 1, lista.Items.Count - 1);
                lista.ScrollIntoView(lista.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Up && lista.Items.Count > 0)
            {
                lista.SelectedIndex = Math.Max(lista.SelectedIndex - 1, 0);
                lista.ScrollIntoView(lista.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                Abrir();
                e.Handled = true;
            }
        };
        lista.MouseDoubleClick += (_, _) => Abrir();
        lista.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Abrir();
                e.Handled = true;
            }
        };
        janela.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                janela.Close();
                e.Handled = true;
            }
        };
        janela.Loaded += (_, _) => caixa.Focus();

        JanelaEscura.PrepararDialogo(janela, "Buscar em tudo",
            new Border { Padding = new Thickness(18, 16, 18, 18), Child = UiApoio.Coluna(0, caixa, dica, lista) });
        DialogoApoio.ExibirModal(janela);
        return escolhido;
    }

    private static string Rotulo(AreaBusca area) => area switch
    {
        AreaBusca.Bem => "Bem",
        AreaBusca.Garantia => "Garantia",
        AreaBusca.Manutencao => "Manutenção",
        AreaBusca.Documento => "Documento",
        AreaBusca.Alerta => "Alerta",
        AreaBusca.Convenio => "Convênio",
        AreaBusca.GastoSaude => "Gasto de saúde",
        AreaBusca.Veiculo => "Veículo",
        AreaBusca.Imovel => "Imóvel",
        AreaBusca.Despesa => "Conta",
        _ => "Abastecimento"
    };
}
