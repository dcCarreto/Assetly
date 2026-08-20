using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Assetly.Aplicacao.Dados;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Manutencao;
using Assetly.Dominio.Saude;

namespace Assetly.Apresentacao.Visao;

internal sealed partial class TelaPrincipal
{
    private void ConcluirManutencao()
    {
        var manutencao = Selecionado<ManutencaoLinha>(_tabelaManutencoes, "Selecione uma manutenção para concluir.");
        ConcluirManutencao(manutencao.Origem);
    }

    private void ConcluirManutencao(Manutencao manutencao)
    {
        var concluidaEm = DataPicker(Hoje());
        var custo = CampoTexto("Custo", string.Empty);
        var moeda = CampoTexto("Moeda", "BRL");
        var grade = GradeFormulario();
        AdicionarCampo(grade, 0, 0, "Concluída em", concluidaEm);
        AdicionarCampo(grade, 0, 1, "Custo", custo);
        AdicionarCampo(grade, 1, 0, "Moeda", moeda);

        if (MostrarFormulario("Concluir manutenção", grade, () => _contexto.ConcluirManutencao().Executar(new ConcluirManutencaoComando(
                manutencao.Id, DataObrigatoria(concluidaEm, "conclusão"), DecimalOpcional(custo.Text), TextoOpcional(moeda.Text)))))
        {
            AtualizarTudo();
        }
    }

    private void CancelarManutencao()
    {
        var manutencao = Selecionado<ManutencaoLinha>(_tabelaManutencoes, "Selecione uma manutenção para cancelar.");
        CancelarManutencao(manutencao.Origem);
    }

    private void CancelarManutencao(Manutencao manutencao)
    {
        if (!MostrarConfirmacao("Cancelar manutenção", "A manutenção selecionada será marcada como cancelada. Deseja continuar?", "Cancelar manutenção"))
        {
            return;
        }

        _contexto.CancelarManutencao().Executar(manutencao.Id);
        AtualizarTudo();
    }

    private void AbrirDocumentoSelecionado()
    {
        AbrirDocumento(Selecionado<DocumentoLinha>(_tabelaDocumentos, "Selecione um documento para abrir.").Origem);
    }

    private static void AbrirDocumento(Documento documento)
    {
        if (documento.Status == StatusDocumento.Ausente)
        {
            throw new InvalidOperationException("documento marcado como ausente não possui arquivo disponível para abrir.");
        }

        var arquivo = Path.GetFullPath(documento.CaminhoLocal.Valor);
        if (!File.Exists(arquivo))
        {
            throw new InvalidOperationException($"arquivo do documento não foi encontrado: {arquivo}");
        }

        try
        {
            Process.Start(new ProcessStartInfo(arquivo) { UseShellExecute = true });
        }
        catch (Exception excecao) when (excecao is Win32Exception or InvalidOperationException)
        {
            throw new InvalidOperationException($"não foi possível abrir o documento: {arquivo}", excecao);
        }
    }

    private void MarcarDocumentoAusente()
    {
        var documento = Selecionado<DocumentoLinha>(_tabelaDocumentos, "Selecione um documento.");
        MarcarDocumentoAusente(documento.Origem);
    }

    private void MarcarDocumentoAusente(Documento documento)
    {
        _contexto.MarcarDocumentoAusente().Executar(documento.Id);
        AtualizarTudo();
    }

    private void ArquivarDocumento()
    {
        var documento = Selecionado<DocumentoLinha>(_tabelaDocumentos, "Selecione um documento.");
        ArquivarDocumento(documento.Origem);
    }

    private void ArquivarDocumento(Documento documento)
    {
        if (!MostrarConfirmacao("Arquivar documento", "O documento selecionado será movido para o arquivo. Deseja continuar?", "Arquivar"))
        {
            return;
        }

        _contexto.ArquivarDocumento().Executar(documento.Id);
        AtualizarTudo();
    }

    private void TomarCienciaAlerta()
    {
        var alerta = Selecionado<AlertaLinha>(_tabelaAlertas, "Selecione um alerta.");
        TomarCienciaAlerta(alerta.Origem);
    }

    private void TomarCienciaAlerta(Alerta alerta)
    {
        _contexto.TomarCienciaAlerta().Executar(alerta.Id);
        AtualizarTudo();
    }

    private void ResolverAlerta()
    {
        var alerta = Selecionado<AlertaLinha>(_tabelaAlertas, "Selecione um alerta.");
        ResolverAlerta(alerta.Origem);
    }

    private void ResolverAlerta(Alerta alerta)
    {
        _contexto.ResolverAlerta().Executar(alerta.Id);
        AtualizarTudo();
    }

    private void AlterarStatusBem(StatusBem status)
    {
        var bem = Selecionado<BemLinha>(_tabelaBens, "Selecione um bem.");
        AlterarStatusBem(bem.Origem, status);
    }

    private void AlterarStatusBem(Bem bem, StatusBem status)
    {
        if (status is StatusBem.Descartado or StatusBem.Arquivado
            && !MostrarConfirmacao(status == StatusBem.Descartado ? "Descartar bem" : "Arquivar bem",
                status == StatusBem.Descartado
                    ? "O bem selecionado será marcado como descartado. Deseja continuar?"
                    : "O bem selecionado será movido para o arquivo. Deseja continuar?",
                status == StatusBem.Descartado ? "Descartar" : "Arquivar"))
        {
            return;
        }

        _contexto.AlterarStatusBem().Executar(new AlterarStatusBemComando(bem.Id, status));
        AtualizarTudo();
    }

    private void CancelarSaude()
    {
        var saude = Selecionado<SaudeLinha>(_tabelaSaude, "Selecione um convênio para cancelar.");
        CancelarSaude(saude.Origem);
    }

    private void CancelarSaude(Saude saude)
    {
        if (!MostrarConfirmacao("Cancelar convênio", "O convênio selecionado será marcado como cancelado. Deseja continuar?", "Cancelar convênio"))
        {
            return;
        }

        _contexto.CancelarSaude().Executar(saude.Id, Hoje());
        AtualizarTudo();
    }

    private void MostrarDetalhesBem()
    {
        var bem = Selecionado<BemLinha>(_tabelaBens, "Selecione um bem.");
        MostrarDetalhesBem(bem.Origem);
    }

    private void MostrarDetalhesBem(Bem bem)
    {
        var garantias = _contexto.RepositorioGarantia.ListarPorBem(bem.Id).Count;
        var manutencoes = _contexto.RepositorioManutencao.ListarPorBem(bem.Id).Count;
        var documentos = _contexto.RepositorioDocumento.ListarPorBem(bem.Id).Count;
        var alertas = _contexto.RepositorioAlerta.ListarPorBem(bem.Id).Count(a => a.Status != StatusAlerta.Resolvido);

        var mensagem = $"""
            Nome: {bem.Nome.Valor}
            Tipo: {Formatador.TextoEnum(bem.Tipo)}
            Status: {Formatador.TextoEnum(bem.Status)}
            Compra: {Formatador.Data(bem.CompradoEm)}
            Valor: {Formatador.Dinheiro(bem.PrecoCompra)}
            Observações: {bem.Observacoes}

            Garantias: {garantias}
            Manutenções: {manutencoes}
            Documentos: {documentos}
            Alertas abertos: {alertas}
            """;
        MostrarInformacao("Detalhes do bem", mensagem);
    }

    private static T Selecionado<T>(DataGrid tabela, string mensagem) where T : class
    {
        if (tabela.SelectedItem is T item)
        {
            return item;
        }

        throw new InvalidOperationException(mensagem);
    }

    private string NomeBem(BemId bemId) => _contexto.RepositorioBem.BuscarPorId(bemId)?.Nome.Valor ?? "Bem não encontrado";

    private void ExecutarComTratamento(Action acao)
    {
        try
        {
            acao();
        }
        catch (Exception excecao) when (excecao is not OutOfMemoryException and not StackOverflowException)
        {
            MostrarErro(excecao);
        }
    }

    /// <summary>
    /// Barra de ações ancorada no rodapé do contêiner da tabela — só aparece quando há seleção.
    /// Ações seguras ficam à esquerda; destrutivas, encostadas na borda direita, bem afastadas das
    /// seguras para que um erro de clique não seja plausível.
    /// </summary>
    private Border BarraContextual(DataGrid tabela, (string Texto, Action Acao)[] seguras, (string Texto, Action Acao)[] destrutivas)
    {
        var recursos = Application.Current.Resources;

        var contagem = new TextBlock
        {
            Text = "1 selecionado",
            FontFamily = (FontFamily)recursos["FontMono"],
            FontSize = 11,
            Foreground = (Brush)recursos["TextTitle"],
            VerticalAlignment = VerticalAlignment.Center
        };

        var limpar = new Button { Style = (Style)recursos["BotaoLimparSelecaoStyle"], VerticalAlignment = VerticalAlignment.Center };
        limpar.Click += (_, _) => tabela.SelectedItem = null;

        var divisor = new Border { Width = 1, Height = 18, Background = (Brush)recursos["InputBorder"], VerticalAlignment = VerticalAlignment.Center };

        var elementosEsquerda = new List<UIElement> { limpar, contagem, divisor };
        elementosEsquerda.AddRange(seguras.Select(a => (UIElement)BotaoSecundario(a.Texto, a.Acao)));
        var painelEsquerda = UiApoio.Linha(10, elementosEsquerda.ToArray());

        var painelDireita = UiApoio.Linha(8, destrutivas.Select(a => (UIElement)BotaoPerigo(a.Texto, a.Acao)).ToArray());

        var grade = new Grid();
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(painelEsquerda, 0);
        Grid.SetColumn(painelDireita, 1);
        grade.Children.Add(painelEsquerda);
        grade.Children.Add(painelDireita);

        var barra = new Border
        {
            Background = (Brush)recursos["SelectionBarBg"],
            BorderBrush = (Brush)recursos["InputBorder"],
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(14, 0, 12, 0),
            Height = 46,
            Visibility = Visibility.Collapsed,
            Child = grade
        };

        tabela.SelectionChanged += (_, _) => barra.Visibility = tabela.SelectedItem is null ? Visibility.Collapsed : Visibility.Visible;
        return barra;
    }

    /// <summary>Botão primário de criação — mesmo estilo do botão primário, com um glifo "+" desenhado à esquerda do rótulo.</summary>
    private Button BotaoPrimarioCriacao(string texto, Action acao)
    {
        var recursos = Application.Current.Resources;
        System.Windows.Shapes.Rectangle Traco(double largura, double altura) => new()
        {
            Width = largura,
            Height = altura,
            Fill = (Brush)recursos["ButtonPrimaryText"],
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var glifo = new Grid { Width = 10, Height = 10, VerticalAlignment = VerticalAlignment.Center };
        glifo.Children.Add(Traco(10, 1.5));
        glifo.Children.Add(Traco(1.5, 10));

        var rotulo = new TextBlock { Text = texto, VerticalAlignment = VerticalAlignment.Center };
        var conteudo = UiApoio.Linha(7, glifo, rotulo);

        var botao = new Button { Content = conteudo, Style = (Style)recursos["BotaoPrimarioStyle"] };
        botao.Click += (_, _) => ExecutarComTratamento(acao);
        return botao;
    }

    private Button BotaoSecundario(string texto, Action acao)
    {
        var botao = new Button { Content = texto, Style = (Style)Application.Current.Resources["BotaoSecundarioStyle"] };
        botao.Click += (_, _) => ExecutarComTratamento(acao);
        return botao;
    }

    private Button BotaoPerigo(string texto, Action acao)
    {
        var botao = new Button { Content = texto, Style = (Style)Application.Current.Resources["BotaoPerigoStyle"] };
        botao.Click += (_, _) => ExecutarComTratamento(acao);
        return botao;
    }

    /// <summary>Atalhos de teclado comuns às telas de tabela: Ctrl+N cria, Ctrl+F foca a busca,
    /// F2 edita a seleção, Delete aciona a ação destrutiva principal (com confirmação já embutida
    /// nela, quando existir), Esc limpa a seleção.</summary>
    private static void ConfigurarAtalhosTabela(UIElement raiz, DataGrid tabela, TextBox busca, Action criar, Action? editar, Action? destrutivaPrincipal)
    {
        raiz.Focusable = true;
        raiz.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                criar();
                e.Handled = true;
            }
            else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                busca.Focus();
                busca.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == Key.F2 && editar is not null && tabela.SelectedItem is not null)
            {
                editar();
                e.Handled = true;
            }
            else if (e.Key == Key.Delete && destrutivaPrincipal is not null && tabela.SelectedItem is not null)
            {
                destrutivaPrincipal();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && tabela.SelectedItem is not null)
            {
                tabela.SelectedItem = null;
                e.Handled = true;
            }
        };
    }
}
