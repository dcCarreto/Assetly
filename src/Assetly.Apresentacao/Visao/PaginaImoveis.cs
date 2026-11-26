using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Assetly.Aplicacao.Dados;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Imovel;
using static Assetly.Apresentacao.Visao.AcoesApoio;
using static Assetly.Apresentacao.Visao.ContextoApoio;
using static Assetly.Apresentacao.Visao.DialogoApoio;
using static Assetly.Apresentacao.Visao.LayoutApoio;

namespace Assetly.Apresentacao.Visao;

/// <summary>Fichas dos imóveis: endereço, características, registro (matrícula e IPTU), condomínio,
/// seguro residencial e locação. As contas ficam na aba vizinha.</summary>
internal sealed class PaginaImoveis : IPagina
{
    private readonly ContextoAplicacao _contexto;
    private readonly PaginaDespesas _contas;
    private readonly DataGrid _tabela = new();
    private readonly FiltroTabela<ImovelLinha> _filtro;
    private readonly TextBlock _resumoEsquerda = new();
    private readonly TextBlock _resumoDireita = new();

    public string Titulo => "Imóveis";
    public UIElement Conteudo { get; }
    internal int Quantidade { get; private set; }

    internal PaginaImoveis(ContextoAplicacao contexto, PaginaDespesas contas)
    {
        _contexto = contexto;
        _contas = contas;
        ConfigurarTabela();

        var (direita, busca) = CabecalhoBuscaCriacao("Buscar por nome, endereço, cidade ou inquilino", "Novo imóvel", () => AbrirDialogo(null));

        _filtro = new FiltroTabela<ImovelLinha>(_tabela, l => l.Id, l => $"{l.Nome} {l.Endereco} {l.Origem.Inquilino} {l.Origem.Matricula}",
        [
            new("Todos", _ => true),
            new("Moradia", l => l.Origem.Situacao == SituacaoImovel.MoradiaPropria),
            new("Alugados", l => l.Origem.Alugado),
            new("Outros", l => l.Origem.Situacao is not (SituacaoImovel.MoradiaPropria or SituacaoImovel.AlugadoParaTerceiros))
        ]);
        _filtro.ConectarBusca(busca);

        var contextual = BarraContextual(_tabela,
        [
            ("Ficha", () => MostrarFicha(Selecionado().Origem)),
            ("Editar", () => AbrirDialogo(Selecionado().Origem)),
            ("Nova conta", () => _contas.AbrirDialogo(null, Selecionado().Id))
        ],
        [
            ("Excluir", () => Excluir(Selecionado()))
        ]);

        Conteudo = TelaComTabela("Imóveis", "Endereço, características, registro, seguro e locação.", direita,
            _filtro.Barra, _tabela, contextual, BarraResumoTabela(_resumoEsquerda, _resumoDireita));
        ConfigurarAtalhosTabela(Conteudo, _tabela, busca, () => AbrirDialogo(null), () => AbrirDialogo(Selecionado().Origem), () => Excluir(Selecionado()));
    }

    public bool Localizar(object chave) => _filtro.Localizar(chave);

    public void Atualizar()
    {
        var imoveis = _contexto.ListarImoveis().Executar();
        Quantidade = imoveis.Count;
        _filtro.AtualizarDados(ConstruirLinhas(imoveis));
    }

    private ImovelLinha Selecionado() => Selecionado<ImovelLinha>(_tabela, "Selecione um imóvel.");

    private void ConfigurarTabela()
    {
        TabelaApoio.AplicarEstiloBase(_tabela);
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Imóvel", nameof(ImovelLinha.Nome), Estrela(1.2), larguraMinima: 140, estiloBase: "CelulaDestaqueStyle"));
        _tabela.Columns.Add(TabelaApoio.ColunaEtiqueta("Situação", nameof(ImovelLinha.Situacao), nameof(ImovelLinha.SituacaoClasse), new DataGridLength(104), dica: nameof(ImovelLinha.Motivo)));
        _tabela.Columns.Add(TabelaApoio.ColunaEtiqueta("Uso", nameof(ImovelLinha.Uso), nameof(ImovelLinha.UsoClasse), new DataGridLength(140)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Tipo", nameof(ImovelLinha.Tipo), new DataGridLength(120)));
        _tabela.Columns.Add(TabelaApoio.ColunaTexto("Endereço", nameof(ImovelLinha.Endereco), Estrela(2.2), larguraMinima: 140, estiloBase: "CelulaSecundariaStyle"));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Área", nameof(ImovelLinha.Area), new DataGridLength(86), nameof(ImovelLinha.AreaOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Condomínio", nameof(ImovelLinha.Condominio), new DataGridLength(108), nameof(ImovelLinha.CondominioOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Aluguel", nameof(ImovelLinha.Aluguel), new DataGridLength(108), nameof(ImovelLinha.AluguelOrdenacao)));
        _tabela.Columns.Add(TabelaApoio.ColunaTextoMono("Seguro", nameof(ImovelLinha.Seguro), new DataGridLength(156), nameof(ImovelLinha.SeguroOrdenacao)));
        var colunas = _tabela.Columns;
        TabelaApoio.ColunasOpcionais(_tabela, colunas[5], colunas[3], colunas[7], colunas[6], colunas[2]); // área, tipo, aluguel, condomínio, uso
        TabelaApoio.ConfigurarLinhas<ImovelLinha>(_tabela, l => l.LinhaClasse, MenuContexto, l => MostrarFicha(l.Origem));
    }

    private IReadOnlyList<ImovelLinha> ConstruirLinhas(IReadOnlyList<Imovel> imoveis)
    {
        var hoje = Hoje();
        var nomeBem = NomesBens(_contexto);
        var alugados = imoveis.Where(i => i.Alugado).ToList();
        _resumoEsquerda.Text = $"{imoveis.Count} imóve{(imoveis.Count == 1 ? "l" : "is")} · {alugados.Count} alugado{(alugados.Count == 1 ? "" : "s")}";
        _resumoDireita.Text = $"Aluguel mensal recebido {Formatador.Total(alugados.Select(i => i.ValorAluguel))} · condomínios {Formatador.Total(imoveis.Select(i => i.ValorCondominio))}";

        var despesas = _contexto.ListarDespesas().Executar().ToLookup(d => d.BemId);
        return imoveis.Select(i =>
        {
            var pendencias = new ApoioFichas.Pendencias(hoje);
            pendencias.Prazo("seguro", i.VencimentoSeguro, Imovel.DiasAvisoSeguro);
            if (i.Alugado)
            {
                pendencias.Prazo("contrato de aluguel", i.FimContratoAluguel, Imovel.DiasAvisoContrato);
            }

            pendencias.Contas(despesas[i.BemId]);
            var (situacao, classe, linha) = ApoioFichas.Situacao(pendencias.Vencidos, pendencias.Proximos);
            return new ImovelLinha(
                i.BemId,
                nomeBem(i.BemId),
                Formatador.TextoEnum(i.Tipo),
                Formatador.TextoEnum(i.Situacao),
                Formatador.ClasseEtiqueta(i.Situacao),
                situacao,
                classe,
                linha,
                Formatador.TextoOuTraco(i.EnderecoCompleto),
                i.AreaM2 is null ? Formatador.TextoOuTraco(null) : $"{Formatador.Numero(i.AreaM2, i.AreaM2 % 1 == 0 ? 0 : 2)} m²",
                Formatador.Dinheiro(i.ValorCondominio),
                i.Alugado ? Formatador.Dinheiro(i.ValorAluguel) : Formatador.TextoOuTraco(null),
                ApoioFichas.Prazo(i.VencimentoSeguro, hoje, Imovel.DiasAvisoSeguro),
                pendencias.Motivo,
                i);
        }).ToList();
    }

    private ContextMenu MenuContexto(ImovelLinha linha)
    {
        var menu = new ContextMenu();
        menu.Items.Add(ItemMenu("Ficha completa", () => MostrarFicha(linha.Origem)));
        menu.Items.Add(ItemMenu("Editar", () => AbrirDialogo(linha.Origem)));
        menu.Items.Add(ItemMenu("Nova conta", () => _contas.AbrirDialogo(null, linha.Id)));
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

    private void AbrirDialogo(Imovel? imovel)
    {
        var comFicha = _contexto.ListarImoveis().Executar().Select(i => i.BemId).ToHashSet();
        var vinculo = ApoioFichas.ComboVinculo(_contexto, TipoBem.Imovel, comFicha, "Cadastrar novo bem junto com o imóvel");
        var nome = CampoTexto("Nome do bem (vazio = endereço)", string.Empty);
        var compradoEm = DataPicker(null);
        var preco = CampoTexto("Valor de compra", string.Empty);
        var d = imovel?.Dados();
        var tipo = Combo(d?.Tipo ?? TipoImovel.Apartamento);
        var situacao = Combo(d?.Situacao ?? SituacaoImovel.MoradiaPropria);
        var logradouro = CampoTexto("Rua, avenida...", d?.Logradouro);
        var numero = CampoTexto("Número", d?.Numero);
        var complemento = CampoTexto("Apartamento, bloco, casa...", d?.Complemento);
        var bairro = CampoTexto("Bairro", d?.Bairro);
        var cidade = CampoTexto("Cidade", d?.Cidade);
        var uf = CampoTexto("Sigla do estado (SP, RJ...)", d?.Uf);
        var cep = CampoTexto("CEP (00000-000)", imovel?.CepFormatado);
        var area = CampoTexto("Área em m²", d?.AreaM2?.ToString("0.##", CultureInfo.GetCultureInfo("pt-BR")));
        var quartos = CampoTexto("Quartos", TextoInteiro(d?.Quartos));
        var banheiros = CampoTexto("Banheiros", TextoInteiro(d?.Banheiros));
        var vagas = CampoTexto("Vagas de garagem", TextoInteiro(d?.Vagas));
        var matricula = CampoTexto("Número da matrícula no cartório de registro de imóveis", d?.Matricula);
        var iptu = CampoTexto("Inscrição imobiliária (IPTU)", d?.InscricaoIptu);
        var condominio = CampoTexto("Valor mensal do condomínio", TextoValor(d?.ValorCondominio?.Valor));
        var seguradora = CampoTexto("Seguradora", d?.Seguradora);
        var apolice = CampoTexto("Número da apólice", d?.Apolice);
        var vencimentoSeguro = DataPicker(d?.VencimentoSeguro);
        var inquilino = CampoTexto("Nome do inquilino", d?.Inquilino);
        var aluguel = CampoTexto("Valor mensal do aluguel", TextoValor(d?.ValorAluguel?.Valor));
        var fimContrato = DataPicker(d?.FimContratoAluguel);

        var grade = GradeFormulario();
        var linha = 0;
        AdicionarSecao(grade, linha++, "Identificação");
        if (imovel is null)
        {
            AdicionarCampo(grade, linha++, 0, "Bem", vinculo, ocupaLinhaToda: true);
            AdicionarCampo(grade, linha++, 0, "Nome do bem", nome, ocupaLinhaToda: true);
        }

        AdicionarCampo(grade, linha, 0, "Tipo *", tipo);
        AdicionarCampo(grade, linha++, 1, "Situação *", situacao);

        AdicionarSecao(grade, linha++, "Endereço");
        AdicionarCampo(grade, linha++, 0, "Logradouro", logradouro, ocupaLinhaToda: true);
        AdicionarCampo(grade, linha, 0, "Número", numero);
        AdicionarCampo(grade, linha++, 1, "Complemento", complemento);
        AdicionarCampo(grade, linha, 0, "Bairro", bairro);
        AdicionarCampo(grade, linha++, 1, "Cidade", cidade);
        AdicionarCampo(grade, linha, 0, "UF", uf);
        AdicionarCampo(grade, linha++, 1, "CEP", cep);

        AdicionarSecao(grade, linha++, "Características e registro");
        AdicionarCampo(grade, linha, 0, "Área (m²)", area);
        AdicionarCampo(grade, linha++, 1, "Quartos", quartos);
        AdicionarCampo(grade, linha, 0, "Banheiros", banheiros);
        AdicionarCampo(grade, linha++, 1, "Vagas de garagem", vagas);
        AdicionarCampo(grade, linha, 0, "Matrícula", matricula);
        AdicionarCampo(grade, linha++, 1, "Inscrição do IPTU", iptu);
        AdicionarCampo(grade, linha++, 0, "Condomínio mensal", condominio);

        AdicionarSecao(grade, linha++, "Seguro residencial");
        AdicionarCampo(grade, linha, 0, "Seguradora", seguradora);
        AdicionarCampo(grade, linha++, 1, "Apólice", apolice);
        AdicionarCampo(grade, linha++, 0, "Vencimento do seguro", vencimentoSeguro);

        AdicionarSecao(grade, linha++, "Locação (se alugado para terceiros)");
        AdicionarCampo(grade, linha, 0, "Inquilino", inquilino);
        AdicionarCampo(grade, linha++, 1, "Aluguel mensal", aluguel);
        AdicionarCampo(grade, linha++, 0, "Fim do contrato", fimContrato);

        if (imovel is null)
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

        Dinheiro? Monetario(TextBox campo) => DecimalOpcional(campo.Text) is { } valor ? Dinheiro.De(valor, ApoioFichas.MoedaPadrao) : null;

        var salvou = MostrarFormulario(imovel is null ? "Novo imóvel" : $"Editar imóvel — {NomeBem(_contexto, imovel.BemId)}", grade, () =>
        {
            var novosDados = new DadosImovel(
                ValorEnum<TipoImovel>(tipo), ValorEnum<SituacaoImovel>(situacao),
                logradouro.Text, numero.Text, complemento.Text, bairro.Text, cidade.Text, uf.Text, cep.Text,
                DecimalOpcional(area.Text), InteiroOpcional(quartos.Text, "quartos"), InteiroOpcional(banheiros.Text, "banheiros"),
                InteiroOpcional(vagas.Text, "vagas"), matricula.Text, iptu.Text, Monetario(condominio),
                seguradora.Text, apolice.Text, ValorData(vencimentoSeguro),
                inquilino.Text, Monetario(aluguel), ValorData(fimContrato));
            if (imovel is null)
            {
                _contexto.CadastrarImovel().Executar(new CadastrarImovelComando(
                    ApoioFichas.ValorItem<BemId?>(vinculo, "selecione o bem"), TextoOpcional(nome.Text), ValorData(compradoEm),
                    DecimalOpcional(preco.Text), ApoioFichas.MoedaPadrao, novosDados));
            }
            else
            {
                _contexto.EditarImovel().Executar(new EditarImovelComando(imovel.BemId, novosDados));
            }
        }, 680);

        if (salvou)
        {
            _contexto.NotificarAlteracao();
        }
    }

    private void MostrarFicha(Imovel imovel)
    {
        var hoje = Hoje();
        var bem = _contexto.BuscarBem().Executar(imovel.BemId);
        var contas = _contexto.RepositorioDespesa.ListarPorBem(imovel.BemId);
        var pagasNoAno = contas.Where(c => c.PagaEm?.Year == hoje.Year).Select(c => (Dinheiro?)c.Valor);
        var emAberto = contas.Where(c => c.PagaEm is null).Select(c => (Dinheiro?)c.Valor);
        var manutencoes = _contexto.RepositorioManutencao.ListarPorBem(imovel.BemId);
        var comodos = string.Join(" · ", new[]
        {
            imovel.Quartos is null ? null : $"{imovel.Quartos} quarto(s)",
            imovel.Banheiros is null ? null : $"{imovel.Banheiros} banheiro(s)",
            imovel.Vagas is null ? null : $"{imovel.Vagas} vaga(s)"
        }.Where(p => p is not null));
        var locacao = imovel.Alugado
            ? $"\nLocação: {Formatador.TextoOuTraco(imovel.Inquilino)} · {Formatador.Dinheiro(imovel.ValorAluguel)}/mês · contrato até {ApoioFichas.Prazo(imovel.FimContratoAluguel, hoje, Imovel.DiasAvisoContrato)}"
            : string.Empty;

        var texto = $"""
            {bem.Nome.Valor} — {Formatador.TextoEnum(imovel.Tipo)} · {Formatador.TextoEnum(imovel.Situacao)}
            {Formatador.TextoOuTraco(imovel.EnderecoCompleto)}

            Área: {(imovel.AreaM2 is null ? "—" : $"{Formatador.Numero(imovel.AreaM2)} m²")}{(comodos.Length > 0 ? $" · {comodos}" : string.Empty)}
            Matrícula: {Formatador.TextoOuTraco(imovel.Matricula)} · Inscrição IPTU: {Formatador.TextoOuTraco(imovel.InscricaoIptu)}
            Condomínio: {Formatador.Dinheiro(imovel.ValorCondominio)}/mês
            Seguro: {Formatador.TextoOuTraco(imovel.Seguradora)} · apólice {Formatador.TextoOuTraco(imovel.Apolice)} · vence {ApoioFichas.Prazo(imovel.VencimentoSeguro, hoje, Imovel.DiasAvisoSeguro)}{locacao}

            Contas pagas em {hoje.Year}: {Formatador.Total(pagasNoAno)} · em aberto: {Formatador.Total(emAberto)}
            Manutenções: {manutencoes.Count} · custo {Formatador.Total(manutencoes.Select(m => m.Custo))}
            Compra: {Formatador.Data(bem.CompradoEm)} · {Formatador.Dinheiro(bem.PrecoCompra)}
            """;
        MostrarInformacao("Ficha do imóvel", texto);
    }

    private void Excluir(ImovelLinha linha)
    {
        var contas = _contexto.RepositorioDespesa.ListarPorBem(linha.Id).Count;
        if (!MostrarConfirmacao("Excluir imóvel",
                $"\"{linha.Nome}\" será excluído permanentemente, junto com {contas} conta(s) e tudo o que estiver ligado ao bem " +
                "(garantias, manutenções, documentos, alertas).\n\nSe o imóvel foi vendido, prefira arquivá-lo no módulo Bens para manter o histórico.",
                "Excluir definitivamente"))
        {
            return;
        }

        _contexto.ExcluirBem().Executar(linha.Id);
        _contexto.NotificarAlteracao();
    }
}
