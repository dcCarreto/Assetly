using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Assetly.Aplicacao.CasoDeUso;
using Assetly.Aplicacao.Repositorio;

namespace Assetly.Apresentacao.Visao;

/// <summary>Lista os bens excluídos (com tudo o que era ligado a eles) e permite restaurá-los ou apagá-los de vez.</summary>
internal static class LixeiraDialogo
{
    internal static void Mostrar(ContextoAplicacao contexto)
    {
        var recursos = Application.Current.Resources;
        var explicacao = UiApoio.Texto($"Bens excluídos ficam aqui por {LixeiraBens.DiasRetencao} dias, com garantias, manutenções, documentos (e os arquivos), " +
            "fichas e despesas. \"Restaurar\" devolve tudo como estava.", 12, FontWeights.Normal, (Brush)recursos["TextSecondary"]);
        explicacao.TextWrapping = TextWrapping.Wrap;
        explicacao.Margin = new Thickness(0, 0, 0, 12);
        var lista = new ListBox { Style = (Style)recursos["ListaArquivosStyle"], Height = 260 };
        System.Windows.Automation.AutomationProperties.SetName(lista, "Itens da lixeira");

        void Recarregar()
        {
            var itens = contexto.LixeiraBens().Listar()
                .Select(item => new ItemValor<ItemLixeira>(item,
                    $"{item.Descricao}   ·   excluído em {Formatador.DataHora(item.ExcluidoEm.LocalDateTime)}   ·   apagado de vez em {Formatador.Data(LixeiraBens.ExpiraEm(item))}"))
                .ToList();
            lista.ItemsSource = itens.Count == 0 ? [new ItemValor<ItemLixeira>(null!, "A lixeira está vazia.")] : itens;
            lista.IsEnabled = itens.Count > 0;
            if (itens.Count > 0)
            {
                lista.SelectedIndex = 0;
            }
        }

        ItemLixeira Selecionado() =>
            (lista.SelectedItem as ItemValor<ItemLixeira>)?.Valor ?? throw new InvalidOperationException("selecione um item da lixeira.");

        var janela = new Window
        {
            Title = "Lixeira",
            Width = 640,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = DialogoApoio.DonoDialogo(),
            ResizeMode = ResizeMode.NoResize
        };

        var restaurar = new Button { Content = "Restaurar", Style = (Style)recursos["BotaoPrimarioStyle"], MinWidth = 96 };
        restaurar.Click += (_, _) => AcoesApoio.ExecutarComTratamento(() =>
        {
            var item = Selecionado();
            var resultado = contexto.LixeiraBens().Restaurar(item.Id);
            contexto.NotificarAlteracao();
            Recarregar();
            DialogoApoio.MostrarInformacao("Bem restaurado", $"\"{item.Descricao}\" voltou com {resultado.Registros} registro(s) ligado(s)." +
                (resultado.Ignorados > 0 ? $"\n{resultado.Ignorados} registro(s) ligados a outro bem que não existe mais ficaram de fora." : string.Empty));
        });
        var excluir = AcoesApoio.BotaoPerigo("Excluir de vez", () =>
        {
            var item = Selecionado();
            if (DialogoApoio.MostrarConfirmacao("Excluir de vez", $"\"{item.Descricao}\" e tudo o que era ligado a ele (inclusive os arquivos dos documentos) serão apagados para sempre. Deseja continuar?", "Excluir de vez"))
            {
                contexto.LixeiraBens().ExcluirDefinitivamente(item.Id);
                Recarregar();
            }
        });
        var fechar = AcoesApoio.BotaoSecundario("Fechar", janela.Close);
        Recarregar();

        var botoes = UiApoio.Linha(8, excluir, fechar, restaurar);
        botoes.HorizontalAlignment = HorizontalAlignment.Right;
        var rodape = new Border
        {
            Background = (Brush)recursos["TableFooterBg"],
            BorderBrush = (Brush)recursos["CardBorder"],
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 12, 16, 12),
            Child = botoes
        };
        DockPanel.SetDock(rodape, Dock.Bottom);
        var raiz = new DockPanel();
        raiz.Children.Add(rodape);
        raiz.Children.Add(new Border { Padding = new Thickness(18, 16, 18, 16), Child = UiApoio.Coluna(0, explicacao, lista) });
        JanelaEscura.PrepararDialogo(janela, "Lixeira", raiz);
        DialogoApoio.ExibirModal(janela);
    }
}
