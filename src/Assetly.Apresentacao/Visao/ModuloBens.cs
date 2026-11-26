using System.Windows;
using System.Windows.Controls;
using Assetly.Aplicacao.Dados;
using Assetly.Aplicacao.Leitura;
using Assetly.Apresentacao.Leitura;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using static Assetly.Apresentacao.Visao.AcoesApoio;
using static Assetly.Apresentacao.Visao.DialogoApoio;
using static Assetly.Apresentacao.Visao.LayoutApoio;

namespace Assetly.Apresentacao.Visao;

internal sealed class ModuloBens : IModulo
{
    private readonly ContextoAplicacao _contexto;
    private readonly DataGrid _tabela = new();
    private readonly FiltroTabela<BemLinha> _filtro;
    private readonly TextBlock _resumoEsquerda = new();
    private readonly TextBlock _resumoDireita = new();
    private int _contador;

    public string Titulo => "Bens";
    public string Descricao => "Inventário e status";
    public bool ComContador => true;
    public UIElement Conteudo { get; }
    public int Contador => _contador;

    internal ModuloBens(ContextoAplicacao contexto)
    {
        _contexto = contexto;
        ConfigurarTabela();

        var (criacao, busca) = CabecalhoBuscaCriacao("Buscar por nome, tipo, marca, nº de série, local ou observações", "Novo bem", () => AbrirDialogoBem(null));
        var direita = UiApoio.Linha(10,
            BotaoSecundario("Importar planilha…", () => ImportacaoApoio.ImportarCsvBens(_contexto)),
            BotaoSecundario("Lixeira", () => LixeiraDialogo.Mostrar(_contexto)),
            criacao);

        _filtro = new FiltroTabela<BemLinha>(_tabela, l => l.Id, l => $"{l.Nome} {l.Tipo} {l.Responsavel} {l.Observacoes} {l.Origem.Inventario.Marca} {l.Origem.Inventario.Modelo} " +
            $"{l.Origem.Inventario.NumeroSerie} {l.Origem.Inventario.Loja} {l.Origem.Inventario.Localizacao}",
        [
            new("Todos", _ => true),
            new("Ativos", l => l.Origem.Status == StatusBem.Ativo),
            new("Em manutenção", l => l.Origem.Status == StatusBem.EmManutencao),
            new("Arquivados", l => l.Origem.Status == StatusBem.Arquivado),
            new("Descartados", l => l.Origem.Status == StatusBem.Descartado),
            new("Pessoas", l => l.Origem.Tipo == TipoBem.Pessoa)
        ]);
        _filtro.ConectarBusca(busca);

        var contextual = BarraContextual(_tabela,
        [
            ("Detalhes", MostrarDetalhesBem),
            ("Editar", () => AbrirDialogoBem(SelecionadoAtual().Origem)),
            ("Ativar", () => AlterarStatusBem(StatusBem.Ativo)),
            ("Manutenção", () => AlterarStatusBem(StatusBem.EmManutencao))
        ],
        [
            ("Arquivar", () => AlterarStatusBem(StatusBem.Arquivado)),
            ("Descartar", () => AlterarStatusBem(StatusBem.Descartado)),
            ("Excluir", () => ExcluirBem(SelecionadoAtual().Origem))
        ]);

        Conteudo = TelaComTabela("Bens", "Inventário dos ativos, status de uso e valor de compra.", direita,
            _filtro.Barra, _tabela, contextual, BarraResumoTabela(_resumoEsquerda, _resumoDireita));

        ConfigurarAtalhosTabela(Conteudo, _tabela, busca, () => AbrirDialogoBem(null),
            () => AbrirDialogoBem(SelecionadoAtual().Origem),
            () => ExcluirBem(SelecionadoAtual().Origem));
    }

    public bool Localizar(object chave) => _filtro.Localizar(chave);

    public void Atualizar()
    {
        var bens = _contexto.ListarBens().Executar();
        _contador = bens.Count;
        _filtro.AtualizarDados(ConstruirLinhas(bens));
    }

    private BemLinha SelecionadoAtual() => Selecionado<BemLinha>(_tabela, "Selecione um bem.");

    private void ConfigurarTabela()
    {
        TabelaApoio.AplicarEstiloBase(_tabela);
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Nome", nameof(BemLinha.Nome), Estrela(1.9), estiloBase: "CelulaDestaqueStyle"));
        _tabela.Columns.Add(TabelaApoio.ColunaEtiqueta("Status", nameof(BemLinha.Status), nameof(BemLinha.StatusClasse), new DataGridLength(134)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Tipo", nameof(BemLinha.Tipo), new DataGridLength(150)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Compra", nameof(BemLinha.Compra), new DataGridLength(104), nameof(BemLinha.CompraOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Valor", nameof(BemLinha.Valor), new DataGridLength(132), nameof(BemLinha.ValorOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Responsável", nameof(BemLinha.Responsavel), new DataGridLength(140), estiloBase: "CelulaSecundariaStyle"));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Observações", nameof(BemLinha.Observacoes), Estrela(2.2), estiloBase: "CelulaSecundariaStyle"));
        TabelaApoio.ConfigurarLinhas<BemLinha>(_tabela, l => l.LinhaClasse, l => MenuContextoBem(l.Origem), l => MostrarDetalhesBem(l.Origem));
    }

    private IReadOnlyList<BemLinha> ConstruirLinhas(IReadOnlyList<Bem> bens)
    {
        var ativos = bens.Count(b => b.Status == StatusBem.Ativo);
        var emManutencao = bens.Count(b => b.Status == StatusBem.EmManutencao);
        var emUso = bens.Where(b => b.Status is StatusBem.Ativo or StatusBem.EmManutencao);
        _resumoEsquerda.Text = $"{bens.Count} bens · {ativos} ativos · {emManutencao} em manutenção";
        _resumoDireita.Text = $"Valor dos bens em uso {Formatador.Total(emUso.Select(b => b.PrecoCompra))}";
        var nomes = bens.ToDictionary(b => b.Id, b => b.Nome.Valor);

        return bens.Select(bem => new BemLinha(
            bem.Id,
            bem.Nome.Valor,
            Formatador.TextoEnum(bem.Tipo),
            Formatador.TextoEnum(bem.Status),
            Formatador.ClasseEtiqueta(bem.Status),
            ClasseLinha(bem.Status),
            Formatador.Data(bem.CompradoEm),
            Formatador.Dinheiro(bem.PrecoCompra),
            bem.ResponsavelId is { } responsavel && nomes.TryGetValue(responsavel, out var nomeResponsavel) ? nomeResponsavel : string.Empty,
            bem.Observacoes,
            bem)).ToList();
    }

    private static string ClasseLinha(StatusBem status) => status switch
    {
        StatusBem.Ativo => "linha-ok",
        StatusBem.EmManutencao => "linha-aviso",
        StatusBem.Descartado => "linha-perigo",
        StatusBem.Arquivado => "linha-neutra",
        _ => "linha-neutra"
    };

    private ContextMenu MenuContextoBem(Bem bem)
    {
        var menu = new ContextMenu();
        menu.Items.Add(ItemMenu("Editar", () => AbrirDialogoBem(bem)));
        menu.Items.Add(ItemMenu("Detalhes", () => MostrarDetalhesBem(bem)));
        menu.Items.Add(new Separator());
        menu.Items.Add(ItemMenu(bem.Status == StatusBem.Arquivado ? "Desarquivar (ativar)" : "Ativar", () => AlterarStatusBem(bem, StatusBem.Ativo),
            bem.Status is not (StatusBem.Ativo or StatusBem.Descartado)));
        menu.Items.Add(ItemMenu("Marcar em manutenção", () => AlterarStatusBem(bem, StatusBem.EmManutencao),
            bem.Status is StatusBem.Ativo));
        menu.Items.Add(new Separator());
        menu.Items.Add(ItemMenu("Arquivar", () => AlterarStatusBem(bem, StatusBem.Arquivado), bem.Status is StatusBem.Ativo or StatusBem.EmManutencao));
        menu.Items.Add(ItemMenu("Descartar", () => AlterarStatusBem(bem, StatusBem.Descartado), bem.Status != StatusBem.Descartado));
        menu.Items.Add(ItemMenu("Excluir…", () => ExcluirBem(bem)));
        return menu;
    }

    private static MenuItem ItemMenu(string texto, Action acao, bool habilitado = true)
    {
        var item = new MenuItem { Header = texto, IsEnabled = habilitado };
        item.Click += (_, _) => ExecutarComTratamento(acao);
        return item;
    }

    internal void AbrirDialogoBem(Bem? bem)
    {
        var nome = CampoTexto("Nome", bem?.Nome.Valor ?? string.Empty);
        var tipo = Combo(bem?.Tipo ?? TipoBem.Eletronico);
        var compradoEm = DataPicker(bem?.CompradoEm);
        var preco = CampoTexto("Preço (ex.: 1.234,56)", TextoValor(bem?.PrecoCompra?.Valor));
        var moeda = CampoTexto("Código da moeda (BRL, USD, EUR...)", bem?.PrecoCompra?.CodigoMoeda ?? "BRL");
        var observacoes = AreaTexto(bem?.Observacoes ?? string.Empty);
        var responsavel = ComboResponsavel(bem);
        var inventario = bem?.Inventario ?? InventarioBem.Vazio;
        var marca = CampoTexto("Marca", inventario.Marca);
        var modelo = CampoTexto("Modelo", inventario.Modelo);
        var numeroSerie = CampoTexto("Número de série (etiqueta do produto ou nota)", inventario.NumeroSerie);
        var localizacao = CampoTexto("Onde fica (cômodo, endereço, garagem...)", inventario.Localizacao);
        var loja = CampoTexto("Loja onde foi comprado", inventario.Loja);
        var valorAtual = CampoTexto("Valor de hoje (avaliação, tabela FIPE...). Vazio = estimar pela depreciação", TextoValor(bem?.ValorInformado?.Valor));
        var avaliadoEm = DataPicker(bem?.ValorInformado is null ? null : bem.ValorInformadoEm);
        var taxa = CampoTexto("Depreciação própria em % ao ano. Vazio = a padrão do tipo", TextoValor(bem?.TaxaDepreciacao * 100));
        string? imagemNota = null;
        DadosNotaFiscal? dadosNota = null;
        var situacaoNota = UiApoio.Texto("Foto ou imagem escaneada da nota: lê o QR Code da NFC-e e o texto impresso (OCR do Windows, sem internet) e preenche data e valor.",
            11.5, FontWeights.Normal, (System.Windows.Media.Brush)Application.Current.Resources["TextMuted"]);
        situacaoNota.TextWrapping = TextWrapping.Wrap;
        var lerNota = BotaoSecundario("Preencher pela nota (foto)…", () =>
        {
            var caminho = SelecionarImagem();
            if (caminho is null)
            {
                return;
            }

            var cursor = System.Windows.Input.Mouse.OverrideCursor;
            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            LeituraImagemNota leitura;
            try
            {
                leitura = LeitorNotaImagem.Ler(caminho, ContextoApoio.Hoje());
            }
            finally
            {
                System.Windows.Input.Mouse.OverrideCursor = cursor;
            }

            imagemNota = caminho;
            dadosNota = leitura.Dados;
            if (leitura.Dados.Emissao is { } emissao)
            {
                compradoEm.SelectedDate = emissao.ToDateTime(TimeOnly.MinValue);
            }

            if (leitura.Dados.ValorTotal is { } total)
            {
                preco.Text = TextoValor(total);
                moeda.Text = "BRL";
            }

            situacaoNota.Text = LeitorNotaImagem.Descrever(leitura) + " A imagem será anexada como nota fiscal ao salvar.";
        });
        var grade = GradeFormulario();
        AdicionarCampo(grade, 0, 0, "Nome *", nome, ocupaLinhaToda: true);
        AdicionarCampo(grade, 1, 0, "Tipo *", tipo);
        AdicionarCampo(grade, 1, 1, "Responsável (modo família)", responsavel);
        AdicionarCampo(grade, 2, 0, "Marca", marca);
        AdicionarCampo(grade, 2, 1, "Modelo", modelo);
        AdicionarCampo(grade, 3, 0, "Número de série", numeroSerie);
        AdicionarCampo(grade, 3, 1, "Localização", localizacao);
        AdicionarSecao(grade, 4, "Compra");
        AdicionarCampo(grade, 5, 0, "Data de compra", compradoEm);
        AdicionarCampo(grade, 5, 1, "Preço de compra", preco);
        AdicionarCampo(grade, 6, 0, "Moeda", moeda);
        AdicionarCampo(grade, 6, 1, "Loja", loja);
        AdicionarCampo(grade, 7, 0, "Nota fiscal", UiApoio.Coluna(6, lerNota, situacaoNota), ocupaLinhaToda: true);
        AdicionarSecao(grade, 8, "Valor atual (opcional)");
        AdicionarCampo(grade, 9, 0, "Valor atual informado", valorAtual);
        AdicionarCampo(grade, 9, 1, "Avaliado em (vazio = hoje)", avaliadoEm);
        AdicionarCampo(grade, 10, 0, "Depreciação própria (% ao ano)", taxa);
        AdicionarCampo(grade, 11, 0, "Observações", observacoes, ocupaLinhaToda: true);
        lerNota.HorizontalAlignment = HorizontalAlignment.Left;

        var salvou = MostrarFormulario(bem is null ? "Novo bem" : "Editar bem", grade, () =>
        {
            var compradoEmValor = ValorData(compradoEm);
            var precoValor = DecimalOpcional(preco.Text);
            var moedaValor = TextoOpcional(moeda.Text);
            var responsavelValor = (responsavel.SelectedItem as ItemResponsavel)?.Pessoa?.Id;
            var inventarioValor = new InventarioBem(marca.Text, modelo.Text, numeroSerie.Text, loja.Text, localizacao.Text);
            var valorAtualValor = DecimalOpcional(valorAtual.Text);
            var taxaValor = DecimalOpcional(taxa.Text) / 100;
            var salvo = bem is null
                ? _contexto.CadastrarBem().Executar(new CadastrarBemComando(nome.Text, ValorEnum<TipoBem>(tipo), compradoEmValor, precoValor, moedaValor,
                    observacoes.Text, responsavelValor, inventarioValor, valorAtualValor, ValorData(avaliadoEm), taxaValor))
                : _contexto.EditarBem().Executar(new EditarBemComando(bem.Id, nome.Text, ValorEnum<TipoBem>(tipo), compradoEmValor, precoValor, moedaValor,
                    observacoes.Text, responsavelValor, inventarioValor, valorAtualValor, ValorData(avaliadoEm), taxaValor));
            if (imagemNota is not null)
            {
                _contexto.RegistrarDocumento().Executar(new RegistrarDocumentoComando(salvo.Id, Dominio.Documento.TipoDocumento.NotaFiscal,
                    "Nota fiscal" + (dadosNota?.Chave is { } chave ? $" nº {chave.Numero}" : string.Empty), imagemNota,
                    dadosNota?.Emissao ?? ContextoApoio.Hoje(), dadosNota?.Chave?.Valor, dadosNota?.UrlConsulta));
            }
        });

        if (salvou)
        {
            _contexto.NotificarAlteracao();
        }
    }

    private void AlterarStatusBem(StatusBem status) => AlterarStatusBem(SelecionadoAtual().Origem, status);

    private void AlterarStatusBem(Bem bem, StatusBem status)
    {
        if (status is StatusBem.Descartado or StatusBem.Arquivado
            && !MostrarConfirmacao(status == StatusBem.Descartado ? "Descartar bem" : "Arquivar bem",
                status == StatusBem.Descartado
                    ? $"\"{bem.Nome.Valor}\" será marcado como descartado. Descarte é definitivo: o bem não poderá ser reativado (o histórico é mantido). Deseja continuar?"
                    : $"\"{bem.Nome.Valor}\" será arquivado e deixará de gerar alertas. Você pode desarquivá-lo depois com \"Ativar\". Deseja continuar?",
                status == StatusBem.Descartado ? "Descartar" : "Arquivar"))
        {
            return;
        }

        _contexto.AlterarStatusBem().Executar(new AlterarStatusBemComando(bem.Id, status));
        _contexto.NotificarAlteracao();
    }

    private void ExcluirBem(Bem bem)
    {
        var garantias = _contexto.RepositorioGarantia.ListarPorBem(bem.Id).Count;
        var manutencoes = _contexto.RepositorioManutencao.ListarPorBem(bem.Id).Count;
        var documentos = _contexto.RepositorioDocumento.ListarPorBem(bem.Id).Count;
        var convenios = _contexto.RepositorioSaude.ListarPorBem(bem.Id).Count;
        var gastos = _contexto.RepositorioGastoSaude.ListarPorBem(bem.Id).Count;
        var alertas = _contexto.RepositorioAlerta.ListarPorBem(bem.Id).Count;
        var abastecimentos = _contexto.RepositorioAbastecimento.ListarPorBem(bem.Id).Count;
        var despesas = _contexto.RepositorioDespesa.ListarPorBem(bem.Id).Count;
        var vinculados = garantias + manutencoes + documentos + convenios + gastos + alertas + abastecimentos + despesas;

        var detalhe = vinculados == 0
            ? "Nenhum outro registro está ligado a ele."
            : $"Também serão excluídos: {garantias} garantia(s), {manutencoes} manutenção(ões), {documentos} documento(s) " +
              $"(incluindo as cópias dos arquivos), {convenios} convênio(s), {gastos} gasto(s) de saúde, {abastecimentos} abastecimento(s), " +
              $"{despesas} despesa(s)/conta(s) e {alertas} alerta(s), além da ficha de veículo ou imóvel, se houver.";
        if (!MostrarConfirmacao("Excluir bem",
                $"\"{bem.Nome.Valor}\" será excluído. {detalhe}\n\nTudo vai para a lixeira (botão \"Lixeira\" no topo desta tela) e pode ser restaurado " +
                $"por {Aplicacao.CasoDeUso.LixeiraBens.DiasRetencao} dias. Se o item só saiu de uso, prefira Arquivar ou Descartar para manter o histórico.",
                "Excluir"))
        {
            return;
        }

        _contexto.ExcluirBem().Executar(bem.Id);
        _contexto.NotificarAlteracao();
    }

    private void MostrarDetalhesBem() => MostrarDetalhesBem(SelecionadoAtual().Origem);

    private void MostrarDetalhesBem(Bem bem)
    {
        var garantias = _contexto.RepositorioGarantia.ListarPorBem(bem.Id).Count;
        var manutencoes = _contexto.RepositorioManutencao.ListarPorBem(bem.Id).Count;
        var documentos = _contexto.RepositorioDocumento.ListarPorBem(bem.Id).Count;
        var convenios = _contexto.RepositorioSaude.ListarPorBem(bem.Id).Count;
        var gastos = _contexto.RepositorioGastoSaude.ListarPorBem(bem.Id).Count;
        var alertas = _contexto.RepositorioAlerta.ListarPorBem(bem.Id).Count(a => a.Status != StatusAlerta.Resolvido);

        var mensagem = $"""
            Nome: {bem.Nome.Valor}
            Tipo: {Formatador.TextoEnum(bem.Tipo)}
            Status: {Formatador.TextoEnum(bem.Status)}
            Marca / modelo: {Formatador.TextoOuTraco($"{bem.Inventario.Marca} {bem.Inventario.Modelo}".Trim())}
            Número de série: {Formatador.TextoOuTraco(bem.Inventario.NumeroSerie)}
            Localização: {Formatador.TextoOuTraco(bem.Inventario.Localizacao)}
            Compra: {Formatador.Data(bem.CompradoEm)} {(bem.Inventario.Loja.Length > 0 ? "· " + bem.Inventario.Loja : string.Empty)}
            Valor: {Formatador.Dinheiro(bem.PrecoCompra)}
            {(bem.ValorInformado is null ? "Valor estimado hoje" : $"Valor informado em {Formatador.Data(bem.ValorInformadoEm)}")}: {Formatador.Dinheiro(Depreciacao.ValorEstimado(bem, ContextoApoio.Hoje()))}
            Responsável: {NomeResponsavel(bem)}
            Observações: {Formatador.TextoOuTraco(bem.Observacoes)}

            Garantias: {garantias}
            Manutenções: {manutencoes}
            Documentos: {documentos}
            Convênios: {convenios}
            Gastos de saúde: {gastos}
            Abastecimentos: {_contexto.RepositorioAbastecimento.ListarPorBem(bem.Id).Count}
            Despesas/contas: {_contexto.RepositorioDespesa.ListarPorBem(bem.Id).Count}
            Alertas abertos: {alertas}
            """;
        MostrarInformacao("Detalhes do bem", mensagem);
    }
    private string NomeResponsavel(Bem bem) =>
        bem.ResponsavelId is { } id ? _contexto.RepositorioBem.BuscarPorId(id)?.Nome.Valor ?? "—" : "—";

    /// <summary>Pessoas cadastradas (tipo "Pessoa") que podem ser responsáveis pelo bem — modo família.</summary>
    private ComboBox ComboResponsavel(Bem? bem)
    {
        var itens = new List<ItemResponsavel> { new(null) };
        itens.AddRange(_contexto.ListarBens().Executar()
            .Where(b => b.Tipo == TipoBem.Pessoa && (bem is null || !b.Id.Equals(bem.Id)))
            .OrderBy(b => b.Nome.Valor)
            .Select(b => new ItemResponsavel(b)));
        return new ComboBox
        {
            Style = (Style)Application.Current.Resources["ComboDialogoStyle"],
            ItemsSource = itens,
            SelectedItem = itens.FirstOrDefault(i => i.Pessoa is not null && i.Pessoa.Id.Equals(bem?.ResponsavelId)) ?? itens[0],
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ToolTip = "Cadastre as pessoas da família como bens do tipo \"Pessoa (titular)\" para escolhê-las aqui."
        };
    }

    private sealed record ItemResponsavel(Bem? Pessoa)
    {
        public override string ToString() => Pessoa?.Nome.Valor ?? "Ninguém (bem da casa)";
    }
}
