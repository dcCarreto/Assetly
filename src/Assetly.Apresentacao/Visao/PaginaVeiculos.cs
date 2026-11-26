using System.Windows;
using System.Windows.Controls;
using Assetly.Aplicacao.Dados;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Despesa;
using Assetly.Dominio.Veiculo;
using static Assetly.Apresentacao.Visao.AcoesApoio;
using static Assetly.Apresentacao.Visao.ContextoApoio;
using static Assetly.Apresentacao.Visao.DialogoApoio;
using static Assetly.Apresentacao.Visao.LayoutApoio;

namespace Assetly.Apresentacao.Visao;

/// <summary>Fichas dos veículos: identificação, documentação (licenciamento), seguro, plano de revisão,
/// quilometragem e consumo médio. Abastecimentos e despesas ficam nas abas vizinhas.</summary>
internal sealed class PaginaVeiculos : IPagina
{
    private readonly ContextoAplicacao _contexto;
    private readonly PaginaAbastecimentos _abastecimentos;
    private readonly PaginaDespesas _despesas;
    private readonly DataGrid _tabela = new();
    private readonly FiltroTabela<VeiculoLinha> _filtro;
    private readonly TextBlock _resumoEsquerda = new();
    private readonly TextBlock _resumoDireita = new();

    public string Titulo => "Veículos";
    public UIElement Conteudo { get; }
    internal int Quantidade { get; private set; }

    internal PaginaVeiculos(ContextoAplicacao contexto, PaginaAbastecimentos abastecimentos, PaginaDespesas despesas)
    {
        _contexto = contexto;
        _abastecimentos = abastecimentos;
        _despesas = despesas;
        ConfigurarTabela();

        var (direita, busca) = CabecalhoBuscaCriacao("Buscar por nome, placa, marca ou modelo", "Novo veículo", () => AbrirDialogo(null));

        _filtro = new FiltroTabela<VeiculoLinha>(_tabela, l => l.Id, l => $"{l.Nome} {l.Placa} {l.MarcaModelo} {l.Origem.Renavam}",
        [
            new("Todos", _ => true),
            new("Com pendência", l => l.SituacaoClasse == "etiqueta-pendencia"),
            new("Atenção", l => l.SituacaoClasse == "etiqueta-atencao"),
            new("Em dia", l => l.SituacaoClasse == "etiqueta-em-dia")
        ]);
        _filtro.ConectarBusca(busca);

        var contextual = BarraContextual(_tabela,
        [
            ("Ficha", () => MostrarFicha(Selecionado().Origem)),
            ("Editar", () => AbrirDialogo(Selecionado().Origem)),
            ("Atualizar km", () => AtualizarKm(Selecionado().Origem)),
            ("Abastecer", () => _abastecimentos.AbrirDialogo(null, Selecionado().Id)),
            ("Nova despesa", () => _despesas.AbrirDialogo(null, Selecionado().Id))
        ],
        [
            ("Excluir", () => Excluir(Selecionado()))
        ]);

        Conteudo = TelaComTabela("Veículos", "Placa, documentação, seguro, revisões e quilometragem.", direita,
            _filtro.Barra, _tabela, contextual, BarraResumoTabela(_resumoEsquerda, _resumoDireita));
        ConfigurarAtalhosTabela(Conteudo, _tabela, busca, () => AbrirDialogo(null), () => AbrirDialogo(Selecionado().Origem), () => Excluir(Selecionado()));
    }

    public bool Localizar(object chave) => _filtro.Localizar(chave);

    public void Atualizar()
    {
        var veiculos = _contexto.ListarVeiculos().Executar();
        Quantidade = veiculos.Count;
        _filtro.AtualizarDados(ConstruirLinhas(veiculos));
    }

    private VeiculoLinha Selecionado() => Selecionado<VeiculoLinha>(_tabela, "Selecione um veículo.");

    private void ConfigurarTabela()
    {
        TabelaApoio.AplicarEstiloBase(_tabela);
        // Mais importante primeiro (o que pede ação vem logo depois do nome): numa janela estreita, o que não cabe some
        // pela direita, sem cortar tudo. Nome, modelo e revisão dividem o que sobra, com mínimo para nunca sumirem; as
        // colunas de data e número têm a largura do conteúdo (fonte monoespaçada).
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Veículo", nameof(VeiculoLinha.Nome), Estrela(1.6), larguraMinima: 150, estiloBase: "CelulaDestaqueStyle"));
        _tabela.Columns.Add(TabelaApoio.ColunaEtiqueta("Situação", nameof(VeiculoLinha.Situacao), nameof(VeiculoLinha.SituacaoClasse), new DataGridLength(104), dica: nameof(VeiculoLinha.Motivo)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Licenciamento", nameof(VeiculoLinha.Licenciamento), new DataGridLength(156), nameof(VeiculoLinha.LicenciamentoOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Seguro", nameof(VeiculoLinha.Seguro), new DataGridLength(156), nameof(VeiculoLinha.SeguroOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Placa", nameof(VeiculoLinha.Placa), new DataGridLength(86)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Marca / modelo", nameof(VeiculoLinha.MarcaModelo), Estrela(1.3), larguraMinima: 120));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Km", nameof(VeiculoLinha.Quilometragem), new DataGridLength(90), nameof(VeiculoLinha.QuilometragemOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Ano", nameof(VeiculoLinha.Ano), new DataGridLength(74)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Próxima revisão", nameof(VeiculoLinha.Revisao), Estrela(1.1), larguraMinima: 110, estiloBase: "CelulaSecundariaStyle"));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Consumo", nameof(VeiculoLinha.Consumo), new DataGridLength(86)));
        var colunas = _tabela.Columns;
        TabelaApoio.ColunasOpcionais(_tabela, colunas[9], colunas[7], colunas[8], colunas[6], colunas[5]); // consumo, ano, revisão, km, modelo
        TabelaApoio.ConfigurarLinhas<VeiculoLinha>(_tabela, l => l.LinhaClasse, MenuContexto, l => MostrarFicha(l.Origem));
    }

    private IReadOnlyList<VeiculoLinha> ConstruirLinhas(IReadOnlyList<Veiculo> veiculos)
    {
        var hoje = Hoje();
        var nomeBem = NomesBens(_contexto);
        var abastecimentos = _contexto.ListarAbastecimentos().Executar().ToLookup(a => a.BemId);
        var despesas = _contexto.ListarDespesas().Executar().ToLookup(d => d.BemId);
        var linhas = veiculos.Select(v =>
        {
            var pendencias = Pendencias(v, despesas[v.BemId], hoje);
            var (situacao, classe, linha) = ApoioFichas.Situacao(pendencias.Vencidos, pendencias.Proximos);
            var consumo = CalculoConsumo.Medio(abastecimentos[v.BemId]);
            return new VeiculoLinha(
                v.BemId,
                nomeBem(v.BemId),
                Formatador.TextoOuTraco(v.PlacaFormatada),
                Formatador.TextoOuTraco(v.Descricao),
                Ano(v),
                Formatador.Km(v.Quilometragem),
                situacao,
                classe,
                linha,
                ApoioFichas.Prazo(v.VencimentoLicenciamento, hoje, Veiculo.DiasAvisoVencimento),
                ApoioFichas.Prazo(v.VencimentoSeguro, hoje, Veiculo.DiasAvisoVencimento),
                Revisao(v),
                consumo is null ? Formatador.TextoOuTraco(null) : $"{Formatador.Numero(consumo, 1)} km/l",
                pendencias.Motivo,
                v);
        }).ToList();

        var comPendencia = linhas.Count(l => l.SituacaoClasse == "etiqueta-pendencia");
        var anoAtual = hoje.Year;
        var combustivelAno = _contexto.ListarAbastecimentos().Executar().Where(a => a.Data.Year == anoAtual).Select(a => (Dinheiro?)a.Valor);
        _resumoEsquerda.Text = $"{veiculos.Count} veículo{(veiculos.Count == 1 ? "" : "s")} · {comPendencia} com pendência";
        _resumoDireita.Text = $"Combustível em {anoAtual}: {Formatador.Total(combustivelAno)}";
        return linhas;
    }

    /// <summary>Conta prazos vencidos e próximos (licenciamento, seguro, revisão por km e por data).</summary>
    internal static ApoioFichas.Pendencias Pendencias(Veiculo veiculo, IEnumerable<Dominio.Despesa.Despesa> despesas, DateOnly hoje)
    {
        var pendencias = new ApoioFichas.Pendencias(hoje);
        pendencias.Prazo("licenciamento", veiculo.VencimentoLicenciamento, Veiculo.DiasAvisoVencimento);
        pendencias.Prazo("seguro", veiculo.VencimentoSeguro, Veiculo.DiasAvisoVencimento);
        pendencias.Prazo("revisão", veiculo.ProximaRevisaoEm, Veiculo.DiasAvisoRevisao);
        if (veiculo.ProximaRevisaoKm is { } km && veiculo.Quilometragem >= km)
        {
            pendencias.Vencido($"revisão dos {Formatador.Km(km)} passou");
        }

        pendencias.Contas(despesas);
        return pendencias;
    }

    private static string Ano(Veiculo veiculo) => (veiculo.AnoFabricacao, veiculo.AnoModelo) switch
    {
        (null, null) => Formatador.TextoOuTraco(null),
        ({ } fabricacao, { } modelo) when fabricacao != modelo => $"{fabricacao}/{modelo % 100:00}",
        ({ } fabricacao, _) => fabricacao.ToString(),
        (_, { } modelo) => modelo.ToString()
    };

    private static string Revisao(Veiculo veiculo)
    {
        var partes = new List<string>();
        if (veiculo.ProximaRevisaoKm is { } km)
        {
            var faltam = km - veiculo.Quilometragem;
            partes.Add(faltam <= 0 ? $"{Formatador.Km(km)} (atrasada)" : $"{Formatador.Km(km)} (faltam {Formatador.Km(faltam)})");
        }

        if (veiculo.ProximaRevisaoEm is { } data)
        {
            partes.Add(Formatador.Data(data));
        }

        return partes.Count == 0 ? Formatador.TextoOuTraco(null) : string.Join(" · ", partes);
    }

    private ContextMenu MenuContexto(VeiculoLinha linha)
    {
        var menu = new ContextMenu();
        menu.Items.Add(ItemMenu("Ficha completa", () => MostrarFicha(linha.Origem)));
        menu.Items.Add(ItemMenu("Editar", () => AbrirDialogo(linha.Origem)));
        menu.Items.Add(ItemMenu("Atualizar quilometragem", () => AtualizarKm(linha.Origem)));
        menu.Items.Add(new Separator());
        menu.Items.Add(ItemMenu("Novo abastecimento", () => _abastecimentos.AbrirDialogo(null, linha.Id)));
        menu.Items.Add(ItemMenu("Nova despesa", () => _despesas.AbrirDialogo(null, linha.Id)));
        menu.Items.Add(new Separator());
        menu.Items.Add(ItemMenu("Excluir…", () => Excluir(linha)));
        return menu;
    }

    private static MenuItem ItemMenu(string texto, Action acao)
    {
        var item = new MenuItem { Header = texto };
        item.Click += (_, _) => ExecutarComTratamento(acao);
        return item;
    }

    private void AbrirDialogo(Veiculo? veiculo)
    {
        var comFicha = _contexto.ListarVeiculos().Executar().Select(v => v.BemId).ToHashSet();
        var vinculo = ApoioFichas.ComboVinculo(_contexto, TipoBem.Veiculo, comFicha, "Cadastrar novo bem junto com o veículo");
        var nome = CampoTexto("Nome do bem (vazio = marca e modelo)", string.Empty);
        var compradoEm = DataPicker(null);
        var preco = CampoTexto("Valor de compra", string.Empty);
        var dados = veiculo?.Dados();
        var marca = CampoTexto("Marca", dados?.Marca);
        var modelo = CampoTexto("Modelo / versão", dados?.Modelo);
        var anoFabricacao = CampoTexto("Ano de fabricação", TextoInteiro(dados?.AnoFabricacao));
        var anoModelo = CampoTexto("Ano do modelo", TextoInteiro(dados?.AnoModelo));
        var cor = CampoTexto("Cor", dados?.Cor);
        var combustivel = Combo(dados?.Combustivel ?? TipoCombustivel.Flex);
        var placa = CampoTexto("Placa (ABC-1234 ou ABC1D23)", veiculo?.PlacaFormatada);
        var km = CampoTexto("Quilometragem atual", TextoInteiro(dados?.Quilometragem ?? 0));
        var renavam = CampoTexto("RENAVAM (9 a 11 dígitos)", dados?.Renavam);
        var chassi = CampoTexto("Chassi (17 caracteres)", dados?.Chassi);
        var licenciamento = DataPicker(dados?.VencimentoLicenciamento);
        var revisaoKm = CampoTexto("Próxima revisão (km)", TextoInteiro(dados?.ProximaRevisaoKm));
        var revisaoData = DataPicker(dados?.ProximaRevisaoEm);
        var seguradora = CampoTexto("Seguradora", dados?.Seguradora);
        var apolice = CampoTexto("Número da apólice", dados?.Apolice);
        var vencimentoSeguro = DataPicker(dados?.VencimentoSeguro);

        var grade = GradeFormulario();
        var linha = 0;
        AdicionarSecao(grade, linha++, "Identificação");
        if (veiculo is null)
        {
            AdicionarCampo(grade, linha++, 0, "Bem", vinculo, ocupaLinhaToda: true);
            AdicionarCampo(grade, linha++, 0, "Nome do bem", nome, ocupaLinhaToda: true);
        }

        AdicionarCampo(grade, linha, 0, "Marca", marca);
        AdicionarCampo(grade, linha++, 1, "Modelo", modelo);
        AdicionarCampo(grade, linha, 0, "Ano de fabricação", anoFabricacao);
        AdicionarCampo(grade, linha++, 1, "Ano do modelo", anoModelo);
        AdicionarCampo(grade, linha, 0, "Cor", cor);
        AdicionarCampo(grade, linha++, 1, "Combustível *", combustivel);
        AdicionarCampo(grade, linha, 0, "Placa", placa);
        AdicionarCampo(grade, linha++, 1, "Quilometragem atual *", km);

        AdicionarSecao(grade, linha++, "Documentação");
        AdicionarCampo(grade, linha, 0, "RENAVAM", renavam);
        AdicionarCampo(grade, linha++, 1, "Chassi", chassi);
        AdicionarCampo(grade, linha++, 0, "Vencimento do licenciamento", licenciamento);

        AdicionarSecao(grade, linha++, "Revisão");
        AdicionarCampo(grade, linha, 0, "Próxima revisão (km)", revisaoKm);
        AdicionarCampo(grade, linha++, 1, "Próxima revisão (data)", revisaoData);

        AdicionarSecao(grade, linha++, "Seguro");
        AdicionarCampo(grade, linha, 0, "Seguradora", seguradora);
        AdicionarCampo(grade, linha++, 1, "Apólice", apolice);
        AdicionarCampo(grade, linha++, 0, "Vencimento do seguro", vencimentoSeguro);

        if (veiculo is null)
        {
            AdicionarSecao(grade, linha++, "Compra (para um bem novo)");
            AdicionarCampo(grade, linha, 0, "Data de compra", compradoEm);
            AdicionarCampo(grade, linha++, 1, "Valor de compra", preco);
            vinculo.SelectionChanged += (_, _) =>
            {
                var novo = vinculo.SelectedItem is ItemValor<BemId?> { Valor: null };
                nome.IsEnabled = novo;
                compradoEm.IsEnabled = novo;
                preco.IsEnabled = novo;
            };
        }

        var salvou = MostrarFormulario(veiculo is null ? "Novo veículo" : $"Editar veículo — {NomeBem(_contexto, veiculo.BemId)}", grade, () =>
        {
            var novosDados = new DadosVeiculo(
                placa.Text, marca.Text, modelo.Text,
                InteiroOpcional(anoFabricacao.Text, "ano de fabricação"), InteiroOpcional(anoModelo.Text, "ano do modelo"),
                cor.Text, renavam.Text, chassi.Text, ValorEnum<TipoCombustivel>(combustivel),
                InteiroOpcional(km.Text, "quilometragem") ?? 0,
                InteiroOpcional(revisaoKm.Text, "próxima revisão (km)"), ValorData(revisaoData), ValorData(licenciamento),
                seguradora.Text, apolice.Text, ValorData(vencimentoSeguro));
            if (veiculo is null)
            {
                _contexto.CadastrarVeiculo().Executar(new CadastrarVeiculoComando(
                    ApoioFichas.ValorItem<BemId?>(vinculo, "selecione o bem"), TextoOpcional(nome.Text), ValorData(compradoEm),
                    DecimalOpcional(preco.Text), ApoioFichas.MoedaPadrao, novosDados));
            }
            else
            {
                _contexto.EditarVeiculo().Executar(new EditarVeiculoComando(veiculo.BemId, novosDados));
            }
        }, 680);

        if (salvou)
        {
            _contexto.NotificarAlteracao();
        }
    }

    private void AtualizarKm(Veiculo veiculo)
    {
        var km = CampoTexto("Quilometragem atual", TextoInteiro(veiculo.Quilometragem));
        var grade = GradeFormulario();
        AdicionarCampo(grade, 0, 0, "Quilometragem no hodômetro *", km, ocupaLinhaToda: true);
        if (MostrarFormulario($"Atualizar quilometragem — {NomeBem(_contexto, veiculo.BemId)}", grade,
                () => _contexto.AtualizarQuilometragem().Executar(veiculo.BemId,
                    InteiroOpcional(km.Text, "quilometragem") ?? throw new ArgumentException("informe a quilometragem.")), 480))
        {
            _contexto.NotificarAlteracao();
        }
    }

    private void MostrarFicha(Veiculo veiculo)
    {
        var hoje = Hoje();
        var bem = _contexto.BuscarBem().Executar(veiculo.BemId);
        var abastecimentos = _contexto.RepositorioAbastecimento.ListarPorBem(veiculo.BemId);
        var despesas = _contexto.RepositorioDespesa.ListarPorBem(veiculo.BemId);
        var manutencoes = _contexto.RepositorioManutencao.ListarPorBem(veiculo.BemId);
        var consumo = CalculoConsumo.Medio(abastecimentos);
        var ultimos12 = abastecimentos.Where(a => a.Data >= hoje.AddYears(-1)).ToList();

        // Custo de combustível por km: gasto depois do primeiro abastecimento dividido pela distância percorrida desde ele.
        var ordenados = abastecimentos.OrderBy(a => a.Quilometragem).ToList();
        var distancia = ordenados.Count >= 2 ? ordenados[^1].Quilometragem - ordenados[0].Quilometragem : 0;
        var custoKm = distancia > 0 ? ordenados.Skip(1).Sum(a => a.Valor.Valor) / distancia : (decimal?)null;

        var texto = $"""
            {bem.Nome.Valor} — {Formatador.TextoEnum(bem.Status)}
            {Formatador.TextoOuTraco(veiculo.Descricao)} · {Ano(veiculo)} · {Formatador.TextoOuTraco(veiculo.Cor)} · {Formatador.TextoEnum(veiculo.Combustivel)}

            Placa: {Formatador.TextoOuTraco(veiculo.PlacaFormatada)}
            RENAVAM: {Formatador.TextoOuTraco(veiculo.Renavam)}
            Chassi: {Formatador.TextoOuTraco(veiculo.Chassi)}
            Quilometragem: {Formatador.Km(veiculo.Quilometragem)}

            Licenciamento: {ApoioFichas.Prazo(veiculo.VencimentoLicenciamento, hoje, Veiculo.DiasAvisoVencimento)}
            Seguro: {Formatador.TextoOuTraco(veiculo.Seguradora)} · apólice {Formatador.TextoOuTraco(veiculo.Apolice)} · vence {ApoioFichas.Prazo(veiculo.VencimentoSeguro, hoje, Veiculo.DiasAvisoVencimento)}
            Próxima revisão: {Revisao(veiculo)}

            Consumo médio: {(consumo is null ? "— (registre ao menos dois abastecimentos de tanque cheio)" : $"{Formatador.Numero(consumo, 1)} km/l")}
            Combustível nos últimos 12 meses: {Formatador.Total(ultimos12.Select(a => (Dinheiro?)a.Valor))} ({ultimos12.Count} abastecimentos)
            Custo de combustível por km: {(custoKm is null ? "—" : Formatador.Dinheiro(Dinheiro.De(custoKm.Value, ApoioFichas.MoedaPadrao)))}
            Despesas pagas: {Formatador.Total(despesas.Where(d => d.PagaEm is not null).Select(d => (Dinheiro?)d.Valor))} · em aberto: {Formatador.Total(despesas.Where(d => d.PagaEm is null).Select(d => (Dinheiro?)d.Valor))}
            Manutenções: {manutencoes.Count} · custo {Formatador.Total(manutencoes.Select(m => m.Custo))}
            Compra: {Formatador.Data(bem.CompradoEm)} · {Formatador.Dinheiro(bem.PrecoCompra)}
            """;
        MostrarInformacao("Ficha do veículo", texto);
    }

    private void Excluir(VeiculoLinha linha)
    {
        var abastecimentos = _contexto.RepositorioAbastecimento.ListarPorBem(linha.Id).Count;
        var despesas = _contexto.RepositorioDespesa.ListarPorBem(linha.Id).Count;
        if (!MostrarConfirmacao("Excluir veículo",
                $"\"{linha.Nome}\" será excluído permanentemente, junto com {abastecimentos} abastecimento(s), {despesas} despesa(s) e tudo o que estiver " +
                "ligado ao bem (garantias, manutenções, documentos, alertas).\n\nSe o veículo foi vendido, prefira arquivá-lo ou descartá-lo no módulo Bens para manter o histórico.",
                "Excluir definitivamente"))
        {
            return;
        }

        _contexto.ExcluirBem().Executar(linha.Id);
        _contexto.NotificarAlteracao();
    }
}
