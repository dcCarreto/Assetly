using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Assetly.Dominio.Bem;

namespace Assetly.Apresentacao.Visao;

/// <summary>Diálogos (formulário, alerta, confirmação) e construtores de campo compartilhados por
/// todos os módulos com criação/edição. Extraído da antiga TelaPrincipal.Dialogos.cs na modularização.</summary>
internal static class DialogoApoio
{
    /// <summary>Ponto de interceptação para testes de interface: quando definido, recebe a janela do
    /// diálogo em vez de exibi-la (permite renderizar formulários fora da tela). Nulo no uso normal.</summary>
    internal static Action<Window>? InterceptarDialogo { get; set; }

    /// <summary>Janela principal já visível, ou nulo (ex.: autoteste sem janela) — nunca o próprio diálogo, que o WPF
    /// registraria como janela principal se ela ainda não existisse.</summary>
    private static Window? DonoDialogo() => Application.Current.MainWindow is { IsVisible: true } principal ? principal : null;

    private static void ExibirModal(Window janela)
    {
        if (InterceptarDialogo is not null)
        {
            InterceptarDialogo(janela);
            return;
        }

        janela.ShowDialog();
    }

    internal static bool MostrarFormulario(string titulo, FrameworkElement conteudo, Action executarComando, double largura = 600)
    {
        var salvou = false;
        var janela = new Window
        {
            Title = titulo,
            Width = largura,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = DonoDialogo(),
            ResizeMode = ResizeMode.NoResize
        };

        // IsDefault: Enter salva o formulário (exceto dentro de áreas de texto multilinha, que aceitam Enter).
        var salvar = new Button { Content = "Salvar", Style = (Style)Application.Current.Resources["BotaoPrimarioStyle"], MinWidth = 96, Margin = new Thickness(8, 0, 0, 0), IsDefault = true };
        var cancelar = new Button { Content = "Cancelar", Style = (Style)Application.Current.Resources["BotaoSecundarioStyle"], MinWidth = 96 };
        var raizRodape = RodapeFormulario(cancelar, salvar);
        DockPanel.SetDock(raizRodape, Dock.Bottom);

        // Formulários longos (fichas de veículo e imóvel) rolam em vez de passar da altura da tela.
        var corpo = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = Math.Max(320, SystemParameters.WorkArea.Height - 190),
            Content = new Border { Padding = new Thickness(22, 20, 22, 20), Child = conteudo }
        };

        var raizConteudo = new DockPanel();
        raizConteudo.Children.Add(raizRodape);
        raizConteudo.Children.Add(corpo);

        salvar.Click += (_, _) =>
        {
            try
            {
                executarComando();
                salvou = true;
                janela.Close();
            }
            catch (Exception excecao) when (excecao is not OutOfMemoryException and not StackOverflowException)
            {
                MostrarErro(excecao);
            }
        };
        cancelar.Click += (_, _) => janela.Close();
        janela.Loaded += (_, _) => FocarPrimeiroCampo(conteudo);

        JanelaEscura.PrepararDialogo(janela, titulo, raizConteudo);
        ExibirModal(janela);
        return salvou;
    }

    /// <summary>Coloca o cursor no primeiro campo editável do formulário, para que o usuário já possa digitar.</summary>
    private static void FocarPrimeiroCampo(DependencyObject raiz)
    {
        var campo = PrimeiroCampoEditavel(raiz);
        if (campo is null)
        {
            return;
        }

        campo.Focus();
        if (campo is TextBox texto)
        {
            texto.SelectAll();
        }
    }

    private static Control? PrimeiroCampoEditavel(DependencyObject origem)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(origem); i++)
        {
            var filho = VisualTreeHelper.GetChild(origem, i);
            if (filho is TextBox { IsReadOnly: false, IsEnabled: true } or PasswordBox { IsEnabled: true } or ComboBox { IsEnabled: true } or DatePicker { IsEnabled: true })
            {
                return (Control)filho;
            }

            if (filho is not DatePicker && PrimeiroCampoEditavel(filho) is { } encontrado)
            {
                return encontrado;
            }
        }

        return null;
    }

    /// <summary>Rodapé padrão de diálogo de formulário: nota de campo obrigatório à esquerda, ações à direita (Salvar sempre a mais à direita).</summary>
    internal static Border RodapeFormulario(params Button[] botoesNaOrdem)
    {
        var nota = UiApoio.Texto("* campo obrigatório", 11.5, FontWeights.Normal, (Brush)Application.Current.Resources["TextMuted"]);
        nota.VerticalAlignment = VerticalAlignment.Center;

        var botoes = UiApoio.Linha(8, botoesNaOrdem.Cast<UIElement>().ToArray());

        var grade = new Grid();
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(nota, 0);
        Grid.SetColumn(botoes, 1);
        grade.Children.Add(nota);
        grade.Children.Add(botoes);

        return new Border
        {
            Background = (Brush)Application.Current.Resources["TableFooterBg"],
            BorderBrush = (Brush)Application.Current.Resources["CardBorder"],
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(22, 14, 22, 14),
            Child = grade
        };
    }

    /// <summary>Lista de bens para vincular um registro. Num cadastro novo, bens descartados ou
    /// arquivados ficam de fora (não faz sentido registrar garantia nova para um item que não existe
    /// mais); na edição, o bem já vinculado sempre aparece.</summary>
    internal static ComboBox ComboBens(ContextoAplicacao contexto, Bem? selecionado)
    {
        var itens = contexto.ListarBens().Executar()
            .Where(b => (selecionado is not null && b.Id.Equals(selecionado.Id)) || b.Status is StatusBem.Ativo or StatusBem.EmManutencao)
            .Select(b => new ItemBem(b))
            .ToList();
        if (itens.Count == 0)
        {
            throw new InvalidOperationException("nenhum bem ativo cadastrado. Cadastre primeiro um bem no módulo Bens (para convênios e gastos de saúde, use o tipo \"Pessoa (titular)\").");
        }

        var combo = new ComboBox { Style = (Style)Application.Current.Resources["ComboDialogoStyle"], ItemsSource = itens, HorizontalAlignment = HorizontalAlignment.Stretch };
        if (selecionado is not null)
        {
            combo.SelectedItem = itens.FirstOrDefault(i => i.Bem.Id.Equals(selecionado.Id)) ?? itens.FirstOrDefault();
        }
        else if (itens.Count > 0)
        {
            combo.SelectedItem = itens[0];
        }

        return combo;
    }

    internal static Bem ExigirBemSelecionado(ComboBox combo) =>
        combo.SelectedItem is ItemBem item ? item.Bem : throw new ArgumentException("selecione um bem");

    internal static ComboBox Combo<TEnum>(TEnum selecionado) where TEnum : struct, Enum
    {
        var itens = Enum.GetValues<TEnum>().Select(v => new ItemEnum<TEnum>(v, Formatador.TextoEnum(v))).ToList();
        return new ComboBox
        {
            Style = (Style)Application.Current.Resources["ComboDialogoStyle"],
            ItemsSource = itens,
            SelectedItem = itens.First(i => EqualityComparer<TEnum>.Default.Equals(i.Valor, selecionado)),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
    }

    /// <summary>Combo só com alguns valores do enum (ex.: tipos de despesa de veículo).</summary>
    internal static ComboBox ComboValores<TEnum>(IEnumerable<TEnum> valores, TEnum selecionado) where TEnum : struct, Enum
    {
        var itens = valores.Select(v => new ItemEnum<TEnum>(v, Formatador.TextoEnum(v))).ToList();
        return new ComboBox
        {
            Style = (Style)Application.Current.Resources["ComboDialogoStyle"],
            ItemsSource = itens,
            SelectedItem = itens.FirstOrDefault(i => EqualityComparer<TEnum>.Default.Equals(i.Valor, selecionado)) ?? itens.First(),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
    }

    internal static CheckBox CaixaMarcacao(string texto, bool marcada) => new()
    {
        Style = (Style)Application.Current.Resources["CheckBoxStyle"],
        Content = texto,
        IsChecked = marcada,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 8, 0, 0)
    };

    /// <summary>Título de seção dentro de um formulário em grade (ocupa a linha inteira).</summary>
    internal static void AdicionarSecao(Grid grade, int linha, string titulo)
    {
        while (grade.RowDefinitions.Count <= linha)
        {
            grade.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        var texto = new TextBlock
        {
            Text = titulo,
            Style = (Style)Application.Current.Resources["PanelTituloStyle"],
            Margin = new Thickness(0, linha == 0 ? 0 : 24, 0, 0)
        };
        var separador = new Border
        {
            BorderBrush = (Brush)Application.Current.Resources["CardBorder"],
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(0, 0, 0, 6),
            Child = texto
        };
        Grid.SetRow(separador, linha);
        Grid.SetColumnSpan(separador, 2);
        grade.Children.Add(separador);
    }

    internal static int? InteiroOpcional(string? texto, string campo)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var digitos = texto.Trim().Replace(".", string.Empty).Replace(" ", string.Empty).Replace("km", string.Empty, StringComparison.OrdinalIgnoreCase);
        return int.TryParse(digitos, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valor)
            ? valor
            : throw new ArgumentException($"{campo}: informe um número inteiro (ex.: 52.300).");
    }

    internal static string TextoInteiro(int? valor) => valor?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    internal static TEnum ValorEnum<TEnum>(ComboBox combo) where TEnum : struct, Enum =>
        ((ItemEnum<TEnum>)combo.SelectedItem!).Valor;

    internal static TextBox CampoTexto(string prompt, string? valor) => new()
    {
        Text = valor ?? string.Empty,
        Style = (Style)Application.Current.Resources["CampoTextoDialogoStyle"],
        ToolTip = prompt,
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    internal static TextBox AreaTexto(string? valor) => new()
    {
        Text = valor ?? string.Empty,
        Style = (Style)Application.Current.Resources["AreaTextoStyle"],
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    internal static DatePicker DataPicker(DateOnly? valor) => new()
    {
        Style = (Style)Application.Current.Resources["DatePickerDialogoStyle"],
        SelectedDate = valor?.ToDateTime(TimeOnly.MinValue),
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    internal static DateOnly? ValorData(DatePicker campo) =>
        campo.SelectedDate is null ? null : DateOnly.FromDateTime(campo.SelectedDate.Value);

    internal static DateOnly DataObrigatoria(DatePicker campo, string nome) =>
        ValorData(campo) ?? throw new ArgumentException($"data de {nome} é obrigatória");

    /// <summary>Grade de 2 colunas iguais para os campos do formulário — campos longos (Nome, Observações,
    /// Descrição, Mensagem) ocupam as 2 colunas.</summary>
    internal static Grid GradeFormulario()
    {
        var grade = new Grid();
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        return grade;
    }

    internal static void AdicionarCampo(Grid grade, int linha, int coluna, string rotulo, FrameworkElement campo, bool ocupaLinhaToda = false)
    {
        while (grade.RowDefinitions.Count <= linha)
        {
            grade.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        var rotuloLabel = new TextBlock
        {
            Text = rotulo.ToUpperInvariant(),
            Style = (Style)Application.Current.Resources["RotuloKpiStyle"],
            Margin = new Thickness(0, 0, 0, 6)
        };
        campo.HorizontalAlignment = HorizontalAlignment.Stretch;
        // Nome acessível = rótulo visível (sem o "*"), para leitores de tela e automação de interface.
        var nomeAcessivel = rotulo.TrimEnd('*', ' ');
        if (campo is Control)
        {
            System.Windows.Automation.AutomationProperties.SetName(campo, nomeAcessivel);
        }
        else if (campo is Panel painel)
        {
            foreach (var controle in painel.Children.OfType<TextBox>())
            {
                System.Windows.Automation.AutomationProperties.SetName(controle, nomeAcessivel);
            }
        }

        var bloco = UiApoio.Coluna(0, rotuloLabel, campo);

        var topo = linha == 0 ? 0 : 16;
        bloco.Margin = new Thickness(coluna == 1 && !ocupaLinhaToda ? 18 : 0, topo, 0, 0);

        Grid.SetRow(bloco, linha);
        if (ocupaLinhaToda)
        {
            Grid.SetColumn(bloco, 0);
            Grid.SetColumnSpan(bloco, 2);
        }
        else
        {
            Grid.SetColumn(bloco, coluna);
        }

        grade.Children.Add(bloco);
    }

    internal static string DiretorioInicial(string? caminhoAtual)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(caminhoAtual))
            {
                var caminho = Path.GetFullPath(caminhoAtual.Trim());
                if (Directory.Exists(caminho))
                {
                    return caminho;
                }

                var pai = Path.GetDirectoryName(caminho);
                if (!string.IsNullOrEmpty(pai) && Directory.Exists(pai))
                {
                    return pai;
                }
            }
        }
        catch (Exception excecao) when (excecao is ArgumentException or NotSupportedException or PathTooLongException)
        {
            // Texto digitado que não é um caminho válido: cai na pasta padrão abaixo.
        }

        var documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Directory.Exists(documentos) ? documentos : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    internal static void MostrarErro(Exception excecao)
    {
        MostrarAlerta("Erro", $"Não foi possível concluir a ação.\n\n{MensagemErro(excecao)}");
    }

    internal static string MensagemErro(Exception excecao)
    {
        if (!string.IsNullOrWhiteSpace(excecao.Message))
        {
            return excecao.Message;
        }

        if (excecao.InnerException is { Message.Length: > 0 } causa)
        {
            return causa.Message;
        }

        return "Ocorreu um erro inesperado. Verifique os dados informados e tente novamente.";
    }

    internal static void MostrarInformacao(string titulo, string mensagem) => MostrarAlerta(titulo, mensagem);

    internal static void MostrarAlerta(string titulo, string mensagem)
    {
        var texto = new TextBlock
        {
            Text = mensagem,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.Resources["TextBody"],
            Margin = new Thickness(16)
        };

        var fechar = new Button { Content = "Fechar", Style = (Style)Application.Current.Resources["BotaoPrimarioStyle"], MinWidth = 96, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(16) };

        var janela = new Window
        {
            Title = titulo,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = DonoDialogo(),
            ResizeMode = ResizeMode.NoResize,
            MinWidth = 380,
            MaxWidth = 520
        };
        fechar.Click += (_, _) => janela.Close();

        DockPanel.SetDock(fechar, Dock.Bottom);
        var raizConteudo = new DockPanel();
        raizConteudo.Children.Add(fechar);
        raizConteudo.Children.Add(texto);

        JanelaEscura.PrepararDialogo(janela, titulo, raizConteudo);
        ExibirModal(janela);
    }

    /// <summary>Diálogo de confirmação Sim/Não usado antes de ações destrutivas (descartar, arquivar, cancelar).</summary>
    internal static bool MostrarConfirmacao(string titulo, string mensagem, string textoConfirmar)
    {
        var confirmou = false;
        var texto = new TextBlock
        {
            Text = mensagem,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.Resources["TextBody"],
            Margin = new Thickness(16)
        };

        var janela = new Window
        {
            Title = titulo,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = DonoDialogo(),
            ResizeMode = ResizeMode.NoResize,
            MinWidth = 380,
            MaxWidth = 480
        };

        var confirmar = new Button { Content = textoConfirmar, Style = (Style)Application.Current.Resources["BotaoPerigoStyle"], MinWidth = 96, Margin = new Thickness(8, 0, 0, 0) };
        var cancelar = new Button { Content = "Cancelar", Style = (Style)Application.Current.Resources["BotaoSecundarioStyle"], MinWidth = 96 };
        confirmar.Click += (_, _) => { confirmou = true; janela.Close(); };
        cancelar.Click += (_, _) => janela.Close();

        var botoes = UiApoio.Linha(8, cancelar, confirmar);
        botoes.HorizontalAlignment = HorizontalAlignment.Right;
        var barraBotoes = new Border
        {
            Background = (Brush)Application.Current.Resources["TableFooterBg"],
            BorderBrush = (Brush)Application.Current.Resources["CardBorder"],
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 12, 16, 12),
            Child = botoes
        };
        DockPanel.SetDock(barraBotoes, Dock.Bottom);

        var raizConteudo = new DockPanel();
        raizConteudo.Children.Add(barraBotoes);
        raizConteudo.Children.Add(texto);

        JanelaEscura.PrepararDialogo(janela, titulo, raizConteudo);
        ExibirModal(janela);
        return confirmou;
    }

    /// <summary>Diálogo com várias opções (ex.: conflito de sincronização). Devolve o índice escolhido, ou -1 se fechado.
    /// A primeira opção é a recomendada (botão primário, mais à direita).</summary>
    internal static int MostrarEscolha(string titulo, string mensagem, params string[] opcoes)
    {
        var escolhida = -1;
        var texto = new TextBlock { Text = mensagem, TextWrapping = TextWrapping.Wrap, Foreground = (Brush)Application.Current.Resources["TextBody"], Margin = new Thickness(16) };
        var janela = new Window
        {
            Title = titulo,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = DonoDialogo(),
            ResizeMode = ResizeMode.NoResize,
            MinWidth = 420,
            MaxWidth = 560
        };

        var botoes = new List<UIElement>();
        for (var i = opcoes.Length - 1; i >= 0; i--)
        {
            var indice = i;
            var botao = new Button
            {
                Content = opcoes[i],
                Style = (Style)Application.Current.Resources[i == 0 ? "BotaoPrimarioStyle" : "BotaoSecundarioStyle"],
                MinWidth = 96,
                Padding = new Thickness(12, 0, 12, 0)
            };
            botao.Click += (_, _) => { escolhida = indice; janela.Close(); };
            botoes.Add(botao);
        }

        var linha = UiApoio.Linha(8, botoes.ToArray());
        linha.HorizontalAlignment = HorizontalAlignment.Right;
        var barraBotoes = new Border
        {
            Background = (Brush)Application.Current.Resources["TableFooterBg"],
            BorderBrush = (Brush)Application.Current.Resources["CardBorder"],
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 12, 16, 12),
            Child = linha
        };
        DockPanel.SetDock(barraBotoes, Dock.Bottom);
        var raiz = new DockPanel();
        raiz.Children.Add(barraBotoes);
        raiz.Children.Add(texto);
        JanelaEscura.PrepararDialogo(janela, titulo, raiz);
        ExibirModal(janela);
        return escolhida;
    }

    internal static PasswordBox CampoSenha() => new() { Style = (Style)Application.Current.Resources["CampoSenhaStyle"] };

    /// <summary>Pede uma senha (ex.: abrir um backup protegido). Devolve nulo se o usuário cancelar.</summary>
    internal static string? PedirSenha(string titulo, string explicacao)
    {
        var senha = CampoSenha();
        var texto = UiApoio.Texto(explicacao, 12, FontWeights.Normal, (Brush)Application.Current.Resources["TextSecondary"]);
        texto.TextWrapping = TextWrapping.Wrap;
        var grade = GradeFormulario();
        Grid.SetColumnSpan(texto, 2);
        grade.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grade.Children.Add(texto);
        AdicionarCampo(grade, 1, 0, "Senha", senha, ocupaLinhaToda: true);
        string? resultado = null;
        var ok = MostrarFormulario(titulo, grade, () =>
        {
            if (senha.Password.Length == 0)
            {
                throw new ArgumentException("digite a senha.");
            }

            resultado = senha.Password;
        }, largura: 480);
        return ok ? resultado : null;
    }

    /// <summary>Seletor de arquivo de backup do Assetly (.assetly).</summary>
    internal static string? SelecionarArquivoBackup(string? pastaInicial)
    {
        var dialogo = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Selecione o arquivo de backup do Assetly",
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = DiretorioInicial(pastaInicial),
            Filter = "Backup do Assetly (*.assetly)|*.assetly|Todos os arquivos|*.*"
        };
        return dialogo.ShowDialog(Application.Current.MainWindow) == true ? dialogo.FileName : null;
    }

    internal static decimal? DecimalOpcional(string? valor) => Aplicacao.Dados.ValorMonetario.InterpretarOpcional(valor);

    /// <summary>Valor monetário para exibir num campo de edição, no formato brasileiro ("1234,56").</summary>
    internal static string TextoValor(decimal? valor) =>
        valor?.ToString("0.00", CultureInfo.GetCultureInfo("pt-BR")) ?? string.Empty;

    /// <summary>Seletor de arquivo nativo do Windows — navegação por unidades, rede, busca e acesso rápido.</summary>
    internal static string? SelecionarArquivo(string titulo, string? caminhoAtual)
    {
        var dialogo = new Microsoft.Win32.OpenFileDialog
        {
            Title = titulo,
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = DiretorioInicial(caminhoAtual),
            Filter = "Documentos e imagens|*.pdf;*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp;*.heic;*.svg;*.txt;*.csv;*.doc;*.docx;*.xls;*.xlsx;*.odt;*.ods;*.xml;*.zip|Todos os arquivos|*.*"
        };
        return dialogo.ShowDialog(Application.Current.MainWindow) == true ? dialogo.FileName : null;
    }

    /// <summary>Seletor de foto/imagem escaneada (para ler nota fiscal por QR Code e OCR).</summary>
    internal static string? SelecionarImagem()
    {
        var dialogo = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Foto ou imagem escaneada da nota fiscal",
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            Filter = "Imagens|" + string.Join(';', Leitura.LeitorNotaImagem.Extensoes.Select(e => "*" + e)) + "|Todos os arquivos|*.*"
        };
        return dialogo.ShowDialog(Application.Current.MainWindow) == true ? dialogo.FileName : null;
    }

    /// <summary>Seletor de pasta nativo do Windows.</summary>
    internal static string? SelecionarPasta(string titulo, string? caminhoAtual)
    {
        var dialogo = new Microsoft.Win32.OpenFolderDialog
        {
            Title = titulo,
            Multiselect = false,
            InitialDirectory = DiretorioInicial(caminhoAtual)
        };
        return dialogo.ShowDialog(Application.Current.MainWindow) == true ? dialogo.FolderName : null;
    }

    internal static string? TextoOpcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    internal sealed record ItemBem(Bem Bem)
    {
        public override string ToString() => Bem.Nome.Valor;
    }

    internal sealed record ItemEnum<TEnum>(TEnum Valor, string Texto) where TEnum : struct, Enum
    {
        public override string ToString() => Texto;
    }
}
