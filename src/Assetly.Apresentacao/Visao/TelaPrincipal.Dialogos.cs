using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Assetly.Aplicacao.Dados;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Manutencao;

namespace Assetly.Apresentacao.Visao;

internal sealed partial class TelaPrincipal
{
    private void AbrirDialogoBem(Bem? bem)
    {
        var nome = CampoTexto("Nome", bem?.Nome.Valor ?? string.Empty);
        var tipo = Combo(bem?.Tipo ?? TipoBem.Eletronico);
        var compradoEm = DataPicker(bem?.CompradoEm);
        var preco = CampoTexto("Preço", bem?.PrecoCompra?.Valor.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        var moeda = CampoTexto("Moeda", bem?.PrecoCompra?.CodigoMoeda ?? "BRL");
        var observacoes = AreaTexto(bem?.Observacoes ?? string.Empty);
        var grade = GradeFormulario();
        AdicionarLinha(grade, 0, "Nome", nome);
        AdicionarLinha(grade, 1, "Tipo", tipo);
        AdicionarLinha(grade, 2, "Compra", compradoEm);
        AdicionarLinha(grade, 3, "Preço", preco);
        AdicionarLinha(grade, 4, "Moeda", moeda);
        AdicionarLinha(grade, 5, "Observações", observacoes);

        var salvou = MostrarFormulario(bem is null ? "Novo bem" : "Editar bem", grade, () =>
        {
            var compradoEmValor = ValorData(compradoEm);
            var precoValor = DecimalOpcional(preco.Text);
            var moedaValor = TextoOpcional(moeda.Text);
            if (bem is null)
            {
                _contexto.CadastrarBem().Executar(new CadastrarBemComando(nome.Text, ValorEnum<TipoBem>(tipo), compradoEmValor, precoValor, moedaValor, observacoes.Text));
            }
            else
            {
                _contexto.EditarBem().Executar(new EditarBemComando(bem.Id, nome.Text, ValorEnum<TipoBem>(tipo), compradoEmValor, precoValor, moedaValor, observacoes.Text));
            }
        });

        if (salvou)
        {
            AtualizarTudo();
        }
    }

    private void AbrirDialogoGarantia(Garantia? garantia)
    {
        var bemSelecionado = garantia is null ? null : _contexto.BuscarBem().Executar(garantia.BemId);
        var bem = ComboBens(bemSelecionado);
        bem.IsEnabled = garantia is null;
        var tipo = Combo(garantia?.Tipo ?? TipoGarantia.Fabricante);
        var fornecedor = CampoTexto("Fornecedor", garantia?.Fornecedor ?? string.Empty);
        var iniciaEm = DataPicker(garantia?.Periodo.IniciaEm ?? Hoje());
        var terminaEm = DataPicker(garantia?.Periodo.TerminaEm ?? Hoje().AddYears(1));
        var suporte = CampoTexto("Contato de suporte", garantia?.ContatoSuporte ?? string.Empty);
        var grade = GradeFormulario();
        AdicionarLinha(grade, 0, "Bem", bem);
        AdicionarLinha(grade, 1, "Tipo", tipo);
        AdicionarLinha(grade, 2, "Fornecedor", fornecedor);
        AdicionarLinha(grade, 3, "Início", iniciaEm);
        AdicionarLinha(grade, 4, "Fim", terminaEm);
        AdicionarLinha(grade, 5, "Suporte", suporte);

        var salvou = MostrarFormulario(garantia is null ? "Nova garantia" : "Editar garantia", grade, () =>
        {
            var inicio = DataObrigatoria(iniciaEm, "início");
            var fim = DataObrigatoria(terminaEm, "fim");
            if (garantia is null)
            {
                _contexto.RegistrarGarantia().Executar(new RegistrarGarantiaComando(
                    ExigirBemSelecionado(bem).Id, ValorEnum<TipoGarantia>(tipo), fornecedor.Text, inicio, fim, suporte.Text));
            }
            else
            {
                _contexto.EditarGarantia().Executar(new EditarGarantiaComando(garantia.Id, ValorEnum<TipoGarantia>(tipo), fornecedor.Text, inicio, fim, suporte.Text));
            }
        });

        if (salvou)
        {
            AtualizarTudo();
        }
    }

    private void AbrirDialogoManutencao(Manutencao? manutencao)
    {
        var bemSelecionado = manutencao is null ? null : _contexto.BuscarBem().Executar(manutencao.BemId);
        var bem = ComboBens(bemSelecionado);
        bem.IsEnabled = manutencao is null;
        var tipo = Combo(manutencao?.Tipo ?? TipoManutencao.Preventiva);
        var descricao = CampoTexto("Descrição", manutencao?.Descricao ?? string.Empty);
        var data = DataPicker(manutencao?.AgendadaPara ?? Hoje());
        var grade = GradeFormulario();
        AdicionarLinha(grade, 0, "Bem", bem);
        AdicionarLinha(grade, 1, "Tipo", tipo);
        AdicionarLinha(grade, 2, "Descrição", descricao);
        AdicionarLinha(grade, 3, "Agendada para", data);

        var salvou = MostrarFormulario(manutencao is null ? "Nova manutenção" : "Editar manutenção", grade, () =>
        {
            var agendadaPara = DataObrigatoria(data, "data agendada");
            if (manutencao is null)
            {
                _contexto.AgendarManutencao().Executar(new AgendarManutencaoComando(ExigirBemSelecionado(bem).Id, ValorEnum<TipoManutencao>(tipo), descricao.Text, agendadaPara));
            }
            else
            {
                _contexto.EditarManutencao().Executar(new EditarManutencaoComando(manutencao.Id, ValorEnum<TipoManutencao>(tipo), descricao.Text, agendadaPara));
            }
        });

        if (salvou)
        {
            AtualizarTudo();
        }
    }

    private void AbrirDialogoDocumento(Documento? documento)
    {
        var bemSelecionado = documento is null ? null : _contexto.BuscarBem().Executar(documento.BemId);
        var bem = ComboBens(bemSelecionado);
        bem.IsEnabled = documento is null;
        var tipo = Combo(documento?.Tipo ?? TipoDocumento.NotaFiscal);
        var nome = CampoTexto("Nome", documento?.Nome.Valor ?? string.Empty);
        var caminho = CampoTexto("Arquivo de origem", documento?.CaminhoLocal.Valor ?? string.Empty);
        var registradoEm = DataPicker(documento?.RegistradoEm ?? Hoje());
        var grade = GradeFormulario();
        AdicionarLinha(grade, 0, "Bem", bem);
        AdicionarLinha(grade, 1, "Tipo", tipo);
        AdicionarLinha(grade, 2, "Nome", nome);
        AdicionarLinha(grade, 3, "Arquivo", SeletorArquivo(caminho));
        AdicionarLinha(grade, 4, "Registrado em", registradoEm);

        var salvou = MostrarFormulario(documento is null ? "Novo documento" : "Editar documento", grade, () =>
        {
            var registro = DataObrigatoria(registradoEm, "registro");
            if (documento is null)
            {
                _contexto.RegistrarDocumento().Executar(new RegistrarDocumentoComando(ExigirBemSelecionado(bem).Id, ValorEnum<TipoDocumento>(tipo), nome.Text, caminho.Text, registro));
            }
            else
            {
                _contexto.EditarDocumento().Executar(new EditarDocumentoComando(documento.Id, ValorEnum<TipoDocumento>(tipo), nome.Text, caminho.Text, registro));
            }
        });

        if (salvou)
        {
            AtualizarTudo();
        }
    }

    private void AbrirDialogoAlerta()
    {
        var bem = ComboBens(null);
        var tipo = Combo(TipoAlerta.GarantiaVencendo);
        var severidade = Combo(SeveridadeAlerta.Aviso);
        var mensagem = CampoTexto("Mensagem", string.Empty);
        var criadoEm = DataPicker(Hoje());
        var prazoEm = DataPicker(null);
        var grade = GradeFormulario();
        AdicionarLinha(grade, 0, "Bem", bem);
        AdicionarLinha(grade, 1, "Tipo", tipo);
        AdicionarLinha(grade, 2, "Severidade", severidade);
        AdicionarLinha(grade, 3, "Mensagem", mensagem);
        AdicionarLinha(grade, 4, "Criado em", criadoEm);
        AdicionarLinha(grade, 5, "Prazo", prazoEm);

        var salvou = MostrarFormulario("Novo alerta", grade, () => _contexto.CriarAlerta().Executar(new CriarAlertaComando(
            ExigirBemSelecionado(bem).Id, ValorEnum<TipoAlerta>(tipo), ValorEnum<SeveridadeAlerta>(severidade), mensagem.Text,
            DataObrigatoria(criadoEm, "criação"), ValorData(prazoEm))));

        if (salvou)
        {
            AtualizarTudo();
        }
    }

    private bool MostrarFormulario(string titulo, FrameworkElement conteudo, Action executarComando)
    {
        var salvou = false;
        var janela = new Window
        {
            Title = titulo,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Application.Current.MainWindow,
            ResizeMode = ResizeMode.NoResize,
            MinWidth = 420
        };

        var salvar = new Button { Content = "Salvar", Style = (Style)Application.Current.Resources["BotaoPrimarioStyle"], MinWidth = 96, Margin = new Thickness(0, 0, 8, 0) };
        var cancelar = new Button { Content = "Cancelar", Style = (Style)Application.Current.Resources["BotaoSecundarioStyle"], MinWidth = 96 };
        var botoes = UiApoio.Linha(8, salvar, cancelar);
        botoes.HorizontalAlignment = HorizontalAlignment.Right;

        var barraBotoes = new Border
        {
            Background = (Brush)Application.Current.Resources["CorJanelaEscuraFundo"],
            BorderBrush = (Brush)Application.Current.Resources["CardBordaBrush"],
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 12, 16, 12),
            Child = botoes
        };
        DockPanel.SetDock(barraBotoes, Dock.Bottom);

        var corpo = new Border { Padding = new Thickness(16), Child = conteudo };

        var raizConteudo = new DockPanel();
        raizConteudo.Children.Add(barraBotoes);
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

        JanelaEscura.PrepararDialogo(janela, titulo, raizConteudo);
        janela.ShowDialog();
        return salvou;
    }

    private ComboBox ComboBens(Bem? selecionado)
    {
        var itens = _contexto.ListarBens().Executar().Select(b => new ItemBem(b)).ToList();
        var combo = new ComboBox { Style = (Style)Application.Current.Resources["ComboStyle"], ItemsSource = itens, HorizontalAlignment = HorizontalAlignment.Stretch };
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

    private static Bem ExigirBemSelecionado(ComboBox combo) =>
        combo.SelectedItem is ItemBem item ? item.Bem : throw new ArgumentException("selecione um bem");

    private static ComboBox Combo<TEnum>(TEnum selecionado) where TEnum : struct, Enum
    {
        var itens = Enum.GetValues<TEnum>().Select(v => new ItemEnum<TEnum>(v, Formatador.TextoEnum(v))).ToList();
        return new ComboBox
        {
            Style = (Style)Application.Current.Resources["ComboStyle"],
            ItemsSource = itens,
            SelectedItem = itens.First(i => EqualityComparer<TEnum>.Default.Equals(i.Valor, selecionado)),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
    }

    private static TEnum ValorEnum<TEnum>(ComboBox combo) where TEnum : struct, Enum =>
        ((ItemEnum<TEnum>)combo.SelectedItem!).Valor;

    private static TextBox CampoTexto(string prompt, string? valor) => new()
    {
        Text = valor ?? string.Empty,
        Style = (Style)Application.Current.Resources["CampoTextoStyle"],
        ToolTip = prompt,
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    private static TextBox AreaTexto(string? valor) => new()
    {
        Text = valor ?? string.Empty,
        Style = (Style)Application.Current.Resources["AreaTextoStyle"],
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    private static DatePicker DataPicker(DateOnly? valor) => new()
    {
        Style = (Style)Application.Current.Resources["DatePickerStyle"],
        SelectedDate = valor?.ToDateTime(TimeOnly.MinValue),
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    private static DateOnly? ValorData(DatePicker campo) =>
        campo.SelectedDate is null ? null : DateOnly.FromDateTime(campo.SelectedDate.Value);

    private static DateOnly DataObrigatoria(DatePicker campo, string nome) =>
        ValorData(campo) ?? throw new ArgumentException($"data de {nome} é obrigatória");

    private static Grid GradeFormulario()
    {
        var grade = new Grid();
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        return grade;
    }

    private static void AdicionarLinha(Grid grade, int linha, string rotulo, FrameworkElement campo)
    {
        while (grade.RowDefinitions.Count <= linha)
        {
            grade.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        var topo = linha == 0 ? 0 : 10;
        var label = new TextBlock { Text = rotulo, Style = (Style)Application.Current.Resources["RotuloFormularioStyle"], Margin = new Thickness(0, topo, 10, 0) };
        campo.Margin = new Thickness(campo.Margin.Left, topo, campo.Margin.Right, campo.Margin.Bottom);
        Grid.SetRow(label, linha);
        Grid.SetColumn(label, 0);
        Grid.SetRow(campo, linha);
        Grid.SetColumn(campo, 1);
        grade.Children.Add(label);
        grade.Children.Add(campo);
    }

    private FrameworkElement SeletorArquivo(TextBox campo)
    {
        var selecionar = BotaoSecundario("Selecionar", () => EscolherArquivo(campo));
        selecionar.Margin = new Thickness(8, 0, 0, 0);
        var grade = new Grid();
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grade.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(campo, 0);
        Grid.SetColumn(selecionar, 1);
        grade.Children.Add(campo);
        grade.Children.Add(selecionar);
        return grade;
    }

    private void EscolherArquivo(TextBox campo)
    {
        var arquivo = SelecionarArquivo(campo.Text);
        if (arquivo is not null)
        {
            campo.Text = arquivo;
        }
    }

    private string? SelecionarArquivo(string? caminhoAtual)
    {
        var diretorioAtual = DiretorioInicial(caminhoAtual);
        string? arquivoSelecionado = null;
        List<string> entradas = [];

        var janela = new Window
        {
            Title = "Selecionar documento",
            Width = 680,
            Height = 430,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Application.Current.MainWindow,
            ResizeMode = ResizeMode.NoResize
        };

        var instrucoes = UiApoio.Texto("Escolha o arquivo que será anexado ao bem.", 13, FontWeights.Normal,
            (Brush)Application.Current.Resources["TextoSuaveBrush"]);
        var diretorioCampo = new TextBox { Style = (Style)Application.Current.Resources["CampoTextoStyle"], IsReadOnly = true };
        var lista = new ListBox { Style = (Style)Application.Current.Resources["ListaArquivosStyle"] };

        void Recarregar(string diretorio)
        {
            diretorioAtual = diretorio;
            diretorioCampo.Text = diretorio;
            try
            {
                entradas = Directory.EnumerateFileSystemEntries(diretorio)
                    .OrderBy(caminho => !Directory.Exists(caminho))
                    .ThenBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                lista.ItemsSource = entradas
                    .Select(caminho => (Directory.Exists(caminho) ? "[Pasta] " : "[Arquivo] ") + Path.GetFileName(caminho))
                    .ToList();
            }
            catch (Exception excecao) when (excecao is IOException or UnauthorizedAccessException)
            {
                throw new InvalidOperationException("não foi possível listar os arquivos do diretório selecionado.", excecao);
            }
        }

        lista.SelectionChanged += (_, _) =>
        {
            if (lista.SelectedIndex >= 0 && lista.SelectedIndex < entradas.Count && File.Exists(entradas[lista.SelectedIndex]))
            {
                arquivoSelecionado = entradas[lista.SelectedIndex];
            }
        };
        lista.MouseDoubleClick += (_, _) =>
        {
            if (lista.SelectedIndex < 0 || lista.SelectedIndex >= entradas.Count)
            {
                return;
            }

            var item = entradas[lista.SelectedIndex];
            if (Directory.Exists(item))
            {
                Recarregar(item);
                arquivoSelecionado = null;
                return;
            }

            arquivoSelecionado = item;
            janela.Close();
        };

        var subir = BotaoSecundario("Subir", () =>
        {
            var pai = Path.GetDirectoryName(diretorioAtual.TrimEnd(Path.DirectorySeparatorChar));
            if (!string.IsNullOrEmpty(pai))
            {
                Recarregar(pai);
                arquivoSelecionado = null;
            }
        });

        Recarregar(diretorioAtual);

        var linhaDiretorio = new Grid();
        linhaDiretorio.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        linhaDiretorio.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        subir.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetColumn(diretorioCampo, 0);
        Grid.SetColumn(subir, 1);
        linhaDiretorio.Children.Add(diretorioCampo);
        linhaDiretorio.Children.Add(subir);

        var conteudo = UiApoio.Coluna(10, instrucoes, linhaDiretorio, lista);
        conteudo.Margin = new Thickness(16);
        lista.MinHeight = 260;

        var salvar = new Button { Content = "Selecionar", Style = (Style)Application.Current.Resources["BotaoPrimarioStyle"], MinWidth = 96, Margin = new Thickness(0, 0, 8, 0) };
        var cancelar = new Button { Content = "Cancelar", Style = (Style)Application.Current.Resources["BotaoSecundarioStyle"], MinWidth = 96 };
        var botoes = UiApoio.Linha(8, salvar, cancelar);
        botoes.HorizontalAlignment = HorizontalAlignment.Right;
        var barraBotoes = new Border
        {
            Background = (Brush)Application.Current.Resources["CorJanelaEscuraFundo"],
            BorderBrush = (Brush)Application.Current.Resources["CardBordaBrush"],
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 12, 16, 12),
            Child = botoes
        };
        DockPanel.SetDock(barraBotoes, Dock.Bottom);

        var raizConteudo = new DockPanel();
        raizConteudo.Children.Add(barraBotoes);
        raizConteudo.Children.Add(conteudo);

        salvar.Click += (_, _) =>
        {
            if (arquivoSelecionado is null || !File.Exists(arquivoSelecionado))
            {
                MostrarErro(new InvalidOperationException("selecione um arquivo válido."));
                return;
            }

            janela.Close();
        };
        cancelar.Click += (_, _) =>
        {
            arquivoSelecionado = null;
            janela.Close();
        };

        JanelaEscura.PrepararDialogo(janela, "Selecionar documento", raizConteudo);
        janela.ShowDialog();
        return arquivoSelecionado;
    }

    private static string DiretorioInicial(string? caminhoAtual)
    {
        if (!string.IsNullOrWhiteSpace(caminhoAtual))
        {
            var caminho = Path.GetFullPath(caminhoAtual);
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

        return Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }

    private void MostrarErro(Exception excecao)
    {
        MostrarAlerta("Erro", $"Não foi possível concluir a ação.\n\n{MensagemErro(excecao)}", perigo: true);
    }

    private static string MensagemErro(Exception excecao)
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

    private void MostrarInformacao(string titulo, string mensagem) => MostrarAlerta(titulo, mensagem, perigo: false);

    private void MostrarAlerta(string titulo, string mensagem, bool perigo)
    {
        var texto = new TextBlock
        {
            Text = mensagem,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.Resources["TextoCelulaBrush"],
            Margin = new Thickness(16)
        };

        var fechar = new Button { Content = "Fechar", Style = (Style)Application.Current.Resources["BotaoPrimarioStyle"], MinWidth = 96, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(16) };

        var janela = new Window
        {
            Title = titulo,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Application.Current.MainWindow,
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
        janela.ShowDialog();
    }

    private static decimal? DecimalOpcional(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        if (decimal.TryParse(valor.Trim().Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var resultado))
        {
            return resultado;
        }

        throw new ArgumentException("valor monetário inválido. Use apenas números, ponto ou vírgula.");
    }

    private static string? TextoOpcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private sealed record ItemBem(Bem Bem)
    {
        public override string ToString() => Bem.Nome.Valor;
    }

    private sealed record ItemEnum<TEnum>(TEnum Valor, string Texto) where TEnum : struct, Enum
    {
        public override string ToString() => Texto;
    }
}
