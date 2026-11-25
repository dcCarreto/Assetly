using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Assetly.Aplicacao.Dados;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Veiculo;
using static Assetly.Apresentacao.Visao.AcoesApoio;
using static Assetly.Apresentacao.Visao.ContextoApoio;
using static Assetly.Apresentacao.Visao.DialogoApoio;
using static Assetly.Apresentacao.Visao.LayoutApoio;

namespace Assetly.Apresentacao.Visao;

/// <summary>Abastecimentos de todos os veículos, com preço por litro e consumo (km/l) calculado pelo
/// método tanque cheio a tanque cheio.</summary>
internal sealed class PaginaAbastecimentos : IPagina
{
    private readonly ContextoAplicacao _contexto;
    private readonly DataGrid _tabela = new();
    private readonly FiltroTabela<AbastecimentoLinha> _filtro;
    private readonly TextBlock _resumoEsquerda = new();
    private readonly TextBlock _resumoDireita = new();

    public string Titulo => "Abastecimentos";
    public UIElement Conteudo { get; }

    internal PaginaAbastecimentos(ContextoAplicacao contexto)
    {
        _contexto = contexto;
        ConfigurarTabela();

        var (direita, busca) = CabecalhoBuscaCriacao("Buscar por veículo, combustível ou posto", "Novo abastecimento", () => AbrirDialogo(null, null));

        _filtro = new FiltroTabela<AbastecimentoLinha>(_tabela, l => l.Id, l => $"{l.Veiculo} {l.Combustivel} {l.Posto}",
        [
            new("Todos", _ => true),
            new("Últimos 30 dias", l => l.Origem.Data >= Hoje().AddDays(-30)),
            new("Este ano", l => l.Origem.Data.Year == Hoje().Year),
            new("Tanque cheio", l => l.Origem.TanqueCheio)
        ]);
        busca.TextChanged += (_, _) => _filtro.DefinirBusca(busca.Text);

        var contextual = BarraContextual(_tabela,
        [
            ("Editar", () => AbrirDialogo(Selecionado().Origem, null)),
            ("Anexar comprovante", () => AnexarComprovante(Selecionado().Origem)),
            ("Ver comprovante", () => ComprovanteApoio.Abrir(_contexto, Vinculo(Selecionado().Origem)))
        ],
        [
            ("Excluir", () => Excluir(Selecionado()))
        ]);

        Conteudo = TelaComTabela("Abastecimentos", "Combustível, preço por litro e consumo de cada veículo.", direita,
            _filtro.Barra, _tabela, contextual, BarraResumoTabela(_resumoEsquerda, _resumoDireita));
        ConfigurarAtalhosTabela(Conteudo, _tabela, busca, () => AbrirDialogo(null, null), () => AbrirDialogo(Selecionado().Origem, null), () => Excluir(Selecionado()));
    }

    public void Atualizar() => _filtro.AtualizarDados(ConstruirLinhas(_contexto.ListarAbastecimentos().Executar()));

    private AbastecimentoLinha Selecionado() => Selecionado<AbastecimentoLinha>(_tabela, "Selecione um abastecimento.");

    private void ConfigurarTabela()
    {
        TabelaApoio.AplicarEstiloBase(_tabela);
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Veículo", nameof(AbastecimentoLinha.Veiculo), Estrela(1.4), estiloBase: "CelulaDestaqueStyle"));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Data", nameof(AbastecimentoLinha.Data), new DataGridLength(100), nameof(AbastecimentoLinha.DataOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Hodômetro", nameof(AbastecimentoLinha.Quilometragem), new DataGridLength(112), nameof(AbastecimentoLinha.QuilometragemOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Litros", nameof(AbastecimentoLinha.Litros), new DataGridLength(84), nameof(AbastecimentoLinha.LitrosOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Valor", nameof(AbastecimentoLinha.Valor), new DataGridLength(112), nameof(AbastecimentoLinha.ValorOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("R$/L", nameof(AbastecimentoLinha.PrecoLitro), new DataGridLength(84), nameof(AbastecimentoLinha.PrecoLitroOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Combustível", nameof(AbastecimentoLinha.Combustivel), new DataGridLength(104)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Cheio", nameof(AbastecimentoLinha.TanqueCheio), new DataGridLength(64)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Consumo", nameof(AbastecimentoLinha.Consumo), new DataGridLength(104)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Posto", nameof(AbastecimentoLinha.Posto), Estrela(1), estiloBase: "CelulaSecundariaStyle"));
        TabelaApoio.ConfigurarLinhas<AbastecimentoLinha>(_tabela, _ => "linha-neutra", MenuContexto, l => AbrirDialogo(l.Origem, null));
    }

    private IReadOnlyList<AbastecimentoLinha> ConstruirLinhas(IReadOnlyList<Abastecimento> abastecimentos)
    {
        var nomeBem = NomesBens(_contexto);
        var consumoPorFechamento = abastecimentos
            .GroupBy(a => a.BemId)
            .SelectMany(grupo => CalculoConsumo.Trechos(grupo))
            .ToDictionary(t => t.Fechamento, t => t.KmPorLitro);
        var doAno = abastecimentos.Where(a => a.Data.Year == Hoje().Year).ToList();
        var litrosAno = doAno.Sum(a => a.Litros);
        _resumoEsquerda.Text = $"{abastecimentos.Count} abastecimentos · {Formatador.Numero(litrosAno, 1)} L em {Hoje().Year}";
        _resumoDireita.Text = litrosAno == 0
            ? $"Gasto em {Hoje().Year}: {Formatador.Total([])}"
            : $"Gasto em {Hoje().Year}: {Formatador.Total(doAno.Select(a => (Dominio.Compartilhado.Dinheiro?)a.Valor))} · média {Formatador.Numero(doAno.Sum(a => a.Valor.Valor) / litrosAno, 3)}/L";

        var comComprovante = ComprovanteApoio.ComComprovante(_contexto, TipoVinculoDocumento.Abastecimento);
        return abastecimentos.Select(a => new AbastecimentoLinha(
            a.Id,
            nomeBem(a.BemId),
            Formatador.Data(a.Data),
            Formatador.Km(a.Quilometragem),
            Formatador.Numero(a.Litros, 2),
            Formatador.Dinheiro(a.Valor),
            Formatador.Numero(a.PrecoPorLitro, 3),
            Formatador.TextoEnum(a.Combustivel),
            a.TanqueCheio ? "Sim" : "Não",
            consumoPorFechamento.TryGetValue(a.Id, out var kmL) ? $"{Formatador.Numero(kmL, 1)} km/l" : Formatador.TextoOuTraco(null),
            Formatador.TextoOuTraco(a.Posto) + (comComprovante.Contains(a.Id.Valor) ? ComprovanteApoio.Marca : string.Empty),
            a)).ToList();
    }

    private ContextMenu MenuContexto(AbastecimentoLinha linha)
    {
        var menu = new ContextMenu();
        var editar = new MenuItem { Header = "Editar" };
        editar.Click += (_, _) => ExecutarComTratamento(() => AbrirDialogo(linha.Origem, null));
        var excluir = new MenuItem { Header = "Excluir…" };
        excluir.Click += (_, _) => ExecutarComTratamento(() => Excluir(linha));
        menu.Items.Add(editar);
        var anexar = new MenuItem { Header = "Anexar comprovante…" };
        anexar.Click += (_, _) => ExecutarComTratamento(() => AnexarComprovante(linha.Origem));
        var ver = new MenuItem { Header = "Ver comprovante" };
        ver.Click += (_, _) => ExecutarComTratamento(() => ComprovanteApoio.Abrir(_contexto, Vinculo(linha.Origem)));
        menu.Items.Add(anexar);
        menu.Items.Add(ver);
        menu.Items.Add(new Separator());
        menu.Items.Add(excluir);
        return menu;
    }

    /// <summary>Formulário de abastecimento. Sugere o combustível e o hodômetro atuais do veículo.</summary>
    internal void AbrirDialogo(Abastecimento? abastecimento, BemId? veiculoPreSelecionado)
    {
        var nomeBem = NomesBens(_contexto);
        var veiculos = _contexto.ListarVeiculos().Executar()
            .Select(v => new ItemValor<Veiculo>(v, $"{nomeBem(v.BemId)}{(v.Placa.Length > 0 ? $" ({v.PlacaFormatada})" : string.Empty)}"))
            .OrderBy(i => i.Texto, StringComparer.CurrentCulture)
            .ToList();
        if (veiculos.Count == 0)
        {
            throw new InvalidOperationException("cadastre primeiro um veículo na aba Veículos.");
        }

        var alvo = abastecimento?.BemId ?? veiculoPreSelecionado;
        var veiculo = ApoioFichas.ComboItens(veiculos, v => alvo is not null && v.BemId.Equals(alvo.Value));
        veiculo.IsEnabled = abastecimento is null;
        var atual = ApoioFichas.ValorItem<Veiculo>(veiculo, "selecione o veículo");
        var data = DataPicker(abastecimento?.Data ?? Hoje());
        var km = CampoTexto("Hodômetro (km)", TextoInteiro(abastecimento?.Quilometragem ?? atual.Quilometragem));
        var combustivel = Combo(abastecimento?.Combustivel ?? CombustivelSugerido(atual.Combustivel));
        var litros = CampoTexto("Litros (ou kWh)", abastecimento?.Litros.ToString("0.###", CultureInfo.GetCultureInfo("pt-BR")) ?? string.Empty);
        var valor = CampoTexto("Valor total pago", TextoValor(abastecimento?.Valor.Valor));
        var cheio = CaixaMarcacao("Completei o tanque (necessário para calcular o consumo)", abastecimento?.TanqueCheio ?? true);
        var posto = CampoTexto("Posto", abastecimento?.Posto ?? string.Empty);

        veiculo.SelectionChanged += (_, _) =>
        {
            if (veiculo.SelectedItem is ItemValor<Veiculo> item)
            {
                km.Text = TextoInteiro(item.Valor.Quilometragem);
                combustivel.SelectedItem = ((IEnumerable<ItemEnum<TipoCombustivel>>)combustivel.ItemsSource)
                    .First(i => i.Valor == CombustivelSugerido(item.Valor.Combustivel));
            }
        };

        var grade = GradeFormulario();
        AdicionarCampo(grade, 0, 0, "Veículo *", veiculo);
        AdicionarCampo(grade, 0, 1, "Data *", data);
        AdicionarCampo(grade, 1, 0, "Hodômetro (km) *", km);
        AdicionarCampo(grade, 1, 1, "Combustível *", combustivel);
        AdicionarCampo(grade, 2, 0, "Litros / kWh *", litros);
        AdicionarCampo(grade, 2, 1, "Valor total *", valor);
        AdicionarCampo(grade, 3, 0, "Posto", posto);
        Grid.SetRow(cheio, 4);
        Grid.SetColumnSpan(cheio, 2);
        grade.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        cheio.Margin = new Thickness(0, 16, 0, 0);
        grade.Children.Add(cheio);

        var salvou = MostrarFormulario(abastecimento is null ? "Novo abastecimento" : "Editar abastecimento", grade, () =>
        {
            var dataValor = DataObrigatoria(data, "abastecimento");
            var kmValor = InteiroOpcional(km.Text, "hodômetro") ?? throw new ArgumentException("informe o hodômetro (km) no momento do abastecimento.");
            var litrosValor = DecimalOpcional(litros.Text) ?? throw new ArgumentException("informe a quantidade abastecida.");
            var valorTotal = DecimalOpcional(valor.Text) ?? throw new ArgumentException("informe o valor pago.");
            var tanqueCheio = cheio.IsChecked == true;
            if (abastecimento is null)
            {
                _contexto.RegistrarAbastecimento().Executar(new RegistrarAbastecimentoComando(
                    ApoioFichas.ValorItem<Veiculo>(veiculo, "selecione o veículo").BemId, dataValor, kmValor, litrosValor, valorTotal,
                    ApoioFichas.MoedaPadrao, ValorEnum<TipoCombustivel>(combustivel), tanqueCheio, posto.Text));
            }
            else
            {
                _contexto.EditarAbastecimento().Executar(new EditarAbastecimentoComando(abastecimento.Id, dataValor, kmValor, litrosValor,
                    valorTotal, abastecimento.Valor.CodigoMoeda, ValorEnum<TipoCombustivel>(combustivel), tanqueCheio, posto.Text));
            }
        });

        if (salvou)
        {
            _contexto.NotificarAlteracao();
        }
    }

    private static TipoCombustivel CombustivelSugerido(TipoCombustivel doVeiculo) =>
        doVeiculo is TipoCombustivel.Flex or TipoCombustivel.Hibrido ? TipoCombustivel.Gasolina : doVeiculo;

    private static VinculoDocumento Vinculo(Abastecimento abastecimento) => new(TipoVinculoDocumento.Abastecimento, abastecimento.Id.Valor);

    private void AnexarComprovante(Abastecimento abastecimento) =>
        ComprovanteApoio.Anexar(_contexto, abastecimento.BemId, Vinculo(abastecimento),
            $"abastecimento {Formatador.Data(abastecimento.Data)}" + (string.IsNullOrWhiteSpace(abastecimento.Posto) ? string.Empty : $" · {abastecimento.Posto}"));

    private void Excluir(AbastecimentoLinha linha)
    {
        if (!MostrarConfirmacao("Excluir abastecimento", $"O abastecimento de {linha.Data} ({linha.Veiculo}, {linha.Valor}) será excluído permanentemente. Deseja continuar?", "Excluir"))
        {
            return;
        }

        _contexto.ExcluirAbastecimento().Executar(linha.Id);
        _contexto.NotificarAlteracao();
    }
}
