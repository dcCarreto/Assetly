using System.Globalization;
using System.IO;
using System.Text;
using Assetly.Aplicacao.Repositorio;
using Assetly.Apresentacao.Visao;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Compartilhado;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Garantia;
using Assetly.Dominio.Manutencao;
using Assetly.Dominio.Despesa;
using Assetly.Dominio.Imovel;
using Assetly.Dominio.Saude;
using Assetly.Dominio.Veiculo;

namespace Assetly.Apresentacao.Semeadura;

internal static class DadosFicticiosAplicacao
{
    internal static void PopularSeNecessario(ContextoAplicacao contexto, string diretorioDocumentos)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(diretorioDocumentos);
        var repositorioBem = contexto.RepositorioBem;
        var repositorioGarantia = contexto.RepositorioGarantia;
        var repositorioManutencao = contexto.RepositorioManutencao;
        var repositorioDocumento = contexto.RepositorioDocumento;

        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var bens = BensPorNome(repositorioBem);

        var notebook = GarantirBem(repositorioBem, bens, "Notebook de trabalho", TipoBem.Eletronico,
            hoje.AddYears(-2), Dinheiro2("5200.00"),
            bem => bem.AtualizarObservacoes("Ambiente de teste: equipamento principal com nota fiscal, garantia próxima e manutenção agendada."));
        var camera = GarantirBem(repositorioBem, bens, "Câmera mirrorless", TipoBem.Eletronico,
            hoje.AddYears(-3), Dinheiro2("4100.00"),
            bem => bem.AtualizarObservacoes("Ambiente de teste: bem sem nota fiscal, com garantia vencida e manutenção atrasada."));
        var geladeira = GarantirBem(repositorioBem, bens, "Geladeira cozinha", TipoBem.Eletrodomestico,
            hoje.AddYears(-1), Dinheiro2("3600.00"),
            bem =>
            {
                bem.MarcarEmManutencao();
                bem.AtualizarObservacoes("Ambiente de teste: item em manutenção com recibos e histórico de assistência.");
            });
        var furadeira = GarantirBem(repositorioBem, bens, "Furadeira de impacto", TipoBem.Ferramenta,
            hoje.AddYears(-4), Dinheiro2("420.00"),
            bem =>
            {
                bem.Arquivar();
                bem.AtualizarObservacoes("Ambiente de teste: item arquivado para validar estados históricos.");
            });
        var monitor = GarantirBem(repositorioBem, bens, "Monitor ultrawide", TipoBem.Eletronico,
            hoje.AddMonths(-8), Dinheiro2("1890.00"),
            bem => bem.AtualizarObservacoes("Ambiente de teste: item ativo com garantia saudável e documentos completos."));
        var bicicleta = GarantirBem(repositorioBem, bens, "Bicicleta elétrica", TipoBem.Veiculo,
            hoje.AddYears(-1).AddMonths(-2), Dinheiro2("7200.00"),
            bem => bem.AtualizarObservacoes("Ambiente de teste: veículo leve com revisão concluída e checklist anexado."));
        var arCondicionado = GarantirBem(repositorioBem, bens, "Ar-condicionado sala", TipoBem.Eletrodomestico,
            hoje.AddMonths(-14), Dinheiro2("2650.00"),
            bem => bem.AtualizarObservacoes("Ambiente de teste: equipamento com instalação documentada e limpeza futura."));
        var sofa = GarantirBem(repositorioBem, bens, "Sofá modular", TipoBem.Movel,
            hoje.AddYears(-5), Dinheiro2("3100.00"),
            bem => bem.AtualizarObservacoes("Ambiente de teste: móvel ativo sem nota fiscal para gerar alerta documental."));
        var impressora = GarantirBem(repositorioBem, bens, "Impressora multifuncional", TipoBem.Eletronico,
            hoje.AddYears(-6), Dinheiro2("980.00"),
            bem =>
            {
                bem.Descartar();
                bem.AtualizarObservacoes("Ambiente de teste: item descartado, útil para validar histórico e status final.");
            });

        PopularGarantias(repositorioGarantia, hoje, notebook, camera, monitor, bicicleta, arCondicionado);
        PopularManutencoes(repositorioManutencao, hoje, notebook, camera, geladeira, bicicleta, arCondicionado, impressora);
        PopularDocumentos(repositorioDocumento, diretorioDocumentos, hoje, notebook, camera, geladeira, furadeira, monitor, bicicleta, arCondicionado, sofa, impressora);

        var titular = GarantirBem(repositorioBem, bens, "Titular demo", TipoBem.Pessoa, null, null,
            bem => bem.AtualizarObservacoes("Ambiente de teste: pessoa titular dos convênios e gastos de saúde (não exige nota fiscal)."));
        PopularSaude(contexto.RepositorioSaude, contexto.RepositorioGastoSaude, hoje, titular);
        PopularVeiculo(contexto, bens, hoje);
        PopularImoveis(contexto, bens, hoje);
        PopularModoFamiliaENotas(contexto, hoje, titular);

        contexto.GerarAlertasAutomaticos().Executar(hoje);
    }

    /// <summary>Modo família (responsável por bem) e uma nota fiscal eletrônica com chave de acesso e link de consulta.</summary>
    private static void PopularModoFamiliaENotas(ContextoAplicacao contexto, DateOnly hoje, Bem titular)
    {
        var repositorio = contexto.RepositorioBem;
        var bens = BensPorNome(repositorio);
        var dependente = GarantirBem(repositorio, bens, "Dependente demo", TipoBem.Pessoa, null, null,
            bem => bem.AtualizarObservacoes("Ambiente de teste: segunda pessoa da família, responsável por alguns bens."));
        foreach (var (nome, pessoa) in new[]
                 {
                     ("Notebook de trabalho", titular), ("Monitor ultrawide", titular), ("Carro hatch demo", titular),
                     ("Apartamento centro demo", titular), ("Bicicleta elétrica", dependente), ("Câmera mirrorless", dependente)
                 })
        {
            if (bens.TryGetValue(nome, out var bem) && bem.ResponsavelId is null)
            {
                bem.DefinirResponsavel(pessoa.Id);
                repositorio.Salvar(bem);
            }
        }

        if (!bens.TryGetValue("Notebook de trabalho", out var notebook))
        {
            return;
        }

        var nota = contexto.RepositorioDocumento.ListarPorBem(notebook.Id).FirstOrDefault(d => d.Tipo == TipoDocumento.NotaFiscal);
        if (nota is not null && string.IsNullOrEmpty(nota.ChaveAcesso))
        {
            var emissao = hoje.AddYears(-2);
            var base43 = $"41{emissao:yyMM}12345678000195" + "65001000012345112345678";
            var chave = ChaveAcessoNfe.Criar(base43 + ChaveAcessoNfe.DigitoVerificador(base43));
            nota.VincularNotaEletronica(chave, "https://www.fazenda.pr.gov.br/nfce/qrcode?p=" + chave.Valor + "|2|1|1|DEMO");
            contexto.RepositorioDocumento.Salvar(nota);
        }
    }

    private static void PopularVeiculo(ContextoAplicacao contexto, Dictionary<string, Bem> bens, DateOnly hoje)
    {
        var carro = GarantirBem(contexto.RepositorioBem, bens, "Carro hatch demo", TipoBem.Veiculo, hoje.AddYears(-3), Dinheiro2("68000.00"),
            bem => bem.AtualizarObservacoes("Ambiente de teste: carro com ficha completa, abastecimentos e despesas."));

        if (contexto.RepositorioVeiculo.BuscarPorBem(carro.Id) is null)
        {
            contexto.RepositorioVeiculo.Salvar(Veiculo.Criar(carro.Id, new DadosVeiculo(
                "BRA2E19", "Volkswagen", "Polo 1.0 TSI", 2022, 2023, "Branco", "01234567890", "9BWAB45U0PT000123",
                TipoCombustivel.Flex, 38000, 42000, hoje.AddDays(40), hoje.AddDays(18), "Seguradora Demo", "AUTO-2026-0042", hoje.AddDays(200))));
        }

        if (contexto.RepositorioAbastecimento.ListarPorBem(carro.Id).Count == 0)
        {
            var registrar = contexto.RegistrarAbastecimento();
            foreach (var (dias, km, litros, valor, combustivel, cheio, posto) in new[]
                     {
                         (75, 38800, 40.0m, "236.00", TipoCombustivel.Gasolina, true, "Posto Avenida Demo"),
                         (60, 39250, 36.5m, "215.35", TipoCombustivel.Gasolina, true, "Posto Avenida Demo"),
                         (45, 39710, 15.0m, "62.85", TipoCombustivel.Etanol, false, "Posto Rodovia Demo"),
                         (38, 40050, 22.0m, "129.80", TipoCombustivel.Gasolina, true, "Posto Avenida Demo"),
                         (20, 40560, 38.0m, "222.30", TipoCombustivel.Gasolina, true, "Posto Centro Demo"),
                         (5, 41200, 41.0m, "243.95", TipoCombustivel.Gasolina, true, "Posto Avenida Demo")
                     })
            {
                registrar.Executar(new Aplicacao.Dados.RegistrarAbastecimentoComando(carro.Id, hoje.AddDays(-dias), km, litros,
                    decimal.Parse(valor, CultureInfo.InvariantCulture), "BRL", combustivel, cheio, posto));
            }
        }

        SalvarDespesasSeAusentes(contexto, carro,
        [
            (TipoDespesa.Ipva, "IPVA (1/3)", hoje.AddMonths(-2), "980.00", hoje.AddMonths(-2)),
            (TipoDespesa.Ipva, "IPVA (2/3)", hoje.AddDays(-10), "980.00", null),
            (TipoDespesa.Ipva, "IPVA (3/3)", hoje.AddMonths(1), "980.00", null),
            (TipoDespesa.Licenciamento, "Licenciamento anual", hoje.AddDays(18), "160.22", null),
            (TipoDespesa.SeguroVeiculo, "Seguro anual (à vista)", hoje.AddDays(-165), "2850.00", hoje.AddDays(-166)),
            (TipoDespesa.Multa, "Multa — velocidade até 20%", hoje.AddDays(-3), "130.16", null),
            (TipoDespesa.Lavagem, "Lavagem completa", hoje.AddDays(-12), "80.00", hoje.AddDays(-12))
        ]);
    }

    private static void PopularImoveis(ContextoAplicacao contexto, Dictionary<string, Bem> bens, DateOnly hoje)
    {
        var apartamento = GarantirBem(contexto.RepositorioBem, bens, "Apartamento centro demo", TipoBem.Imovel, hoje.AddYears(-6), Dinheiro2("420000.00"),
            bem => bem.AtualizarObservacoes("Ambiente de teste: moradia própria com contas mensais e seguro perto do vencimento."));
        if (contexto.RepositorioImovel.BuscarPorBem(apartamento.Id) is null)
        {
            contexto.RepositorioImovel.Salvar(Imovel.Criar(apartamento.Id, new DadosImovel(
                TipoImovel.Apartamento, SituacaoImovel.MoradiaPropria, "Rua das Flores", "250", "apto 81", "Centro", "Curitiba", "PR", "80010000",
                68m, 2, 2, 1, "45.678 — 1º Registro de Imóveis", "12.345.678-9", Dinheiro2("780.00"),
                "Residencial Demo", "RES-77881", hoje.AddDays(25), null, null, null)));
        }

        SalvarDespesasSeAusentes(contexto, apartamento,
        [
            (TipoDespesa.Condominio, "Condomínio do mês passado", hoje.AddDays(-25), "780.00", hoje.AddDays(-27)),
            (TipoDespesa.Condominio, "Condomínio do mês", hoje.AddDays(5), "780.00", null),
            (TipoDespesa.Energia, "Energia elétrica", hoje.AddDays(-2), "214.37", null),
            (TipoDespesa.Agua, "Água e esgoto", hoje.AddDays(12), "96.40", null),
            (TipoDespesa.Internet, "Internet fibra", hoje.AddDays(-8), "119.90", hoje.AddDays(-9)),
            (TipoDespesa.Iptu, "IPTU (parcela)", hoje.AddDays(30), "185.00", null)
        ]);

        var sala = GarantirBem(contexto.RepositorioBem, bens, "Sala comercial demo", TipoBem.Imovel, hoje.AddYears(-9), Dinheiro2("210000.00"),
            bem => bem.AtualizarObservacoes("Ambiente de teste: imóvel alugado, com contrato terminando em breve."));
        if (contexto.RepositorioImovel.BuscarPorBem(sala.Id) is null)
        {
            contexto.RepositorioImovel.Salvar(Imovel.Criar(sala.Id, new DadosImovel(
                TipoImovel.SalaComercial, SituacaoImovel.AlugadoParaTerceiros, "Av. Sete de Setembro", "1500", "sala 1203", "Batel", "Curitiba", "PR",
                "80230000", 42.5m, null, 1, 1, "98.765 — 4º Registro de Imóveis", "98.765.432-1", Dinheiro2("520.00"),
                null, null, null, "Escritório Demo Ltda.", Dinheiro2("2500.00"), hoje.AddDays(50))));
        }

        SalvarDespesasSeAusentes(contexto, sala,
        [
            (TipoDespesa.Iptu, "IPTU anual (à vista)", hoje.AddMonths(-8), "1340.00", hoje.AddMonths(-8))
        ]);
    }

    private static void SalvarDespesasSeAusentes(ContextoAplicacao contexto, Bem bem,
        IReadOnlyList<(TipoDespesa Tipo, string Descricao, DateOnly Vencimento, string Valor, DateOnly? PagaEm)> despesas)
    {
        var existentes = contexto.RepositorioDespesa.ListarPorBem(bem.Id).Select(d => d.Descricao).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (tipo, descricao, vencimento, valor, pagaEm) in despesas.Where(d => !existentes.Contains(d.Descricao)))
        {
            var despesa = Despesa.Registrar(bem.Id, tipo, descricao, vencimento, Dinheiro2(valor));
            if (pagaEm is not null)
            {
                despesa.Pagar(pagaEm.Value);
            }

            contexto.RepositorioDespesa.Salvar(despesa);
        }
    }

    private static void PopularSaude(IRepositorioSaude repositorioSaude, IRepositorioGastoSaude repositorioGastoSaude, DateOnly hoje, Bem titular)
    {
        var convenios = repositorioSaude.ListarPorBem(titular.Id);
        void SalvarConvenioSeAusente(TipoSaude tipo, string fornecedor, DateOnly inicio, DateOnly fim, string contato, DateOnly? cancelamento = null)
        {
            if (convenios.Any(s => s.Tipo == tipo && string.Equals(s.Fornecedor, fornecedor, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var saude = Saude.Criar(titular.Id, tipo, fornecedor, inicio, fim, contato);
            if (cancelamento is not null)
            {
                saude.Cancelar(cancelamento.Value);
            }

            repositorioSaude.Salvar(saude);
        }

        SalvarConvenioSeAusente(TipoSaude.Convenio, "Plano Saúde Demo", hoje.AddMonths(-11), hoje.AddDays(18), "0800 000 0001");
        SalvarConvenioSeAusente(TipoSaude.Ortodontia, "Odonto Demo", hoje.AddMonths(-6), hoje.AddMonths(18), "0800 000 0002");
        SalvarConvenioSeAusente(TipoSaude.Emergencia, "Resgate Demo", hoje.AddYears(-2), hoje.AddDays(-4), "192 / 0800 000 0003");
        SalvarConvenioSeAusente(TipoSaude.Farmacia, "Farmácia Desconto Demo", hoje.AddYears(-2), hoje.AddYears(-1), "app da farmácia", hoje.AddYears(-1).AddMonths(-2));

        var gastos = repositorioGastoSaude.ListarPorBem(titular.Id);
        void SalvarGastoSeAusente(TipoGastoSaude tipo, string descricao, DateOnly data, string valor)
        {
            if (!gastos.Any(g => string.Equals(g.Descricao, descricao, StringComparison.OrdinalIgnoreCase)))
            {
                repositorioGastoSaude.Salvar(GastoSaude.Registrar(titular.Id, tipo, descricao, data, Dinheiro2(valor)));
            }
        }

        SalvarGastoSeAusente(TipoGastoSaude.Farmacia, "Antialérgico e vitamina D", hoje.AddDays(-6), "87.40");
        SalvarGastoSeAusente(TipoGastoSaude.Consulta, "Consulta clínico geral", hoje.AddMonths(-1), "250.00");
        SalvarGastoSeAusente(TipoGastoSaude.Exame, "Hemograma completo", hoje.AddMonths(-1).AddDays(3), "65.00");
        SalvarGastoSeAusente(TipoGastoSaude.Procedimento, "Limpeza dentária", hoje.AddMonths(-3), "180.00");
    }

    private static Dictionary<string, Bem> BensPorNome(IRepositorioBem repositorioBem)
    {
        var bens = new Dictionary<string, Bem>();
        foreach (var bem in repositorioBem.Listar())
        {
            bens[bem.Nome.Valor] = bem;
        }

        return bens;
    }

    private static Bem GarantirBem(
        IRepositorioBem repositorioBem,
        Dictionary<string, Bem> bens,
        string nome,
        TipoBem tipo,
        DateOnly? compradoEm,
        Dinheiro? precoCompra,
        Action<Bem> preparar)
    {
        if (bens.TryGetValue(nome, out var existente))
        {
            return existente;
        }

        var bem = compradoEm is null
            ? Bem.Criar(new NomeBem(nome), tipo)
            : Bem.Criar(new NomeBem(nome), tipo, compradoEm.Value, precoCompra);
        preparar(bem);
        repositorioBem.Salvar(bem);
        bens[nome] = bem;
        return bem;
    }

    private static void PopularGarantias(
        IRepositorioGarantia repositorioGarantia, DateOnly hoje, Bem notebook, Bem camera, Bem monitor, Bem bicicleta, Bem arCondicionado)
    {
        SalvarGarantiaSeAusente(repositorioGarantia, notebook, TipoGarantia.Fabricante, "Fabricante Demo", hoje.AddYears(-1), hoje.AddDays(20));
        SalvarGarantiaSeAusente(repositorioGarantia, camera, TipoGarantia.Estendida, "Loja Demo", hoje.AddYears(-2), hoje.AddDays(-5));
        SalvarGarantiaSeAusente(repositorioGarantia, monitor, TipoGarantia.Loja, "Tech Store Demo", hoje.AddMonths(-8), hoje.AddMonths(16));
        SalvarGarantiaSeAusente(repositorioGarantia, bicicleta, TipoGarantia.Servico, "Oficina Bike Demo", hoje.AddMonths(-10), hoje.AddDays(12));
        SalvarGarantiaSeAusente(repositorioGarantia, arCondicionado, TipoGarantia.Fabricante, "Clima Norte Demo", hoje.AddMonths(-14), hoje.AddMonths(10));
    }

    private static void PopularManutencoes(
        IRepositorioManutencao repositorioManutencao, DateOnly hoje, Bem notebook, Bem camera, Bem geladeira, Bem bicicleta, Bem arCondicionado, Bem impressora)
    {
        SalvarManutencaoSeAusente(repositorioManutencao, Manutencao.Agendar(
            notebook.Id, TipoManutencao.Preventiva, "Limpeza interna e troca de pasta térmica", hoje.AddDays(5)));
        SalvarManutencaoSeAusente(repositorioManutencao, Manutencao.Agendar(
            camera.Id, TipoManutencao.Corretiva, "Revisar sensor e lente", hoje.AddDays(-3)));
        SalvarManutencaoSeAusente(repositorioManutencao, Manutencao.Agendar(
            geladeira.Id, TipoManutencao.Preventiva, "Verificar vedação e limpeza do condensador", hoje.AddDays(21)));

        var revisaoBicicleta = Manutencao.Agendar(
            bicicleta.Id, TipoManutencao.Inspecao, "Revisão de freios, corrente, bateria e calibragem", hoje.AddMonths(-2));
        revisaoBicicleta.Concluir(hoje.AddMonths(-2).AddDays(1), Dinheiro2("185.00"));
        SalvarManutencaoSeAusente(repositorioManutencao, revisaoBicicleta);

        var limpezaAr = Manutencao.Agendar(
            arCondicionado.Id, TipoManutencao.Limpeza, "Limpeza de filtros e higienização preventiva", hoje.AddDays(7));
        SalvarManutencaoSeAusente(repositorioManutencao, limpezaAr);

        var diagnosticoImpressora = Manutencao.Agendar(
            impressora.Id, TipoManutencao.Corretiva, "Diagnóstico de falha na placa lógica", hoje.AddYears(-1));
        diagnosticoImpressora.Cancelar();
        SalvarManutencaoSeAusente(repositorioManutencao, diagnosticoImpressora);
    }

    private static void PopularDocumentos(
        IRepositorioDocumento repositorioDocumento, string diretorioDocumentos, DateOnly hoje,
        Bem notebook, Bem camera, Bem geladeira, Bem furadeira, Bem monitor, Bem bicicleta, Bem arCondicionado, Bem sofa, Bem impressora)
    {
        SalvarDocumentoSeAusente(repositorioDocumento, notebook, TipoDocumento.NotaFiscal, "Nota fiscal notebook PDF",
            ArquivoPdf(diretorioDocumentos, "nota-fiscal-notebook.pdf", "Nota fiscal demonstrativa - Notebook",
                ["Fornecedor: Tech Store Demo", "Item: Notebook de trabalho", "Valor: BRL 5.200,00", $"Compra: {Data(hoje.AddYears(-2))}", "Documento gerado para ambiente de teste."]),
            hoje.AddYears(-2));
        SalvarDocumentoSeAusente(repositorioDocumento, notebook, TipoDocumento.CertificadoGarantia, "Certificado garantia notebook",
            ArquivoPdf(diretorioDocumentos, "certificado-garantia-notebook.pdf", "Certificado de garantia - Notebook",
                ["Cobertura: fabricante demo", $"Vigencia final: {Data(hoje.AddDays(20))}", "Suporte: suporte.notebook.demo@example.test"]),
            hoje.AddYears(-1));
        SalvarDocumentoSeAusente(repositorioDocumento, notebook, TipoDocumento.Manual, "Manual rápido notebook",
            ArquivoPdf(diretorioDocumentos, "manual-notebook.pdf", "Manual rapido - Notebook",
                ["1. Manter ventilacao livre.", "2. Executar backup semanal.", "3. Limpar teclado com pano seco.", "4. Agendar manutencao preventiva a cada 12 meses."]),
            hoje.AddYears(-2));
        SalvarDocumentoSeAusente(repositorioDocumento, notebook, TipoDocumento.Foto, "Foto identificação notebook",
            ArquivoSvg(diretorioDocumentos, "foto-notebook.svg", "Notebook", "#38bdf8", "#0f172a"),
            hoje.AddMonths(-6));
        SalvarDocumentoSeAusente(repositorioDocumento, notebook, TipoDocumento.Outro, "Histórico manutenção notebook",
            ArquivoCsv(diretorioDocumentos, "historico-manutencao-notebook.csv",
                ["data;servico;valor;observacao", $"{Data(hoje.AddMonths(-14))};limpeza preventiva;120.00;sem avarias", $"{Data(hoje.AddMonths(-7))};troca pasta termica;95.00;temperatura normalizada"]),
            hoje.AddMonths(-7));

        SalvarDocumentoSeAusente(repositorioDocumento, camera, TipoDocumento.Manual, "Manual câmera mirrorless",
            ArquivoPdf(diretorioDocumentos, "manual-camera.pdf", "Manual resumido - Camera mirrorless",
                ["Limpar sensor apenas em assistencia autorizada.", "Guardar lentes em local seco.", "Atualizar firmware antes de eventos importantes."]),
            hoje.AddYears(-3));
        SalvarDocumentoSeAusente(repositorioDocumento, camera, TipoDocumento.Foto, "Foto câmera mirrorless",
            ArquivoSvg(diretorioDocumentos, "foto-camera.svg", "Camera", "#f97316", "#111827"),
            hoje.AddMonths(-8));

        SalvarDocumentoSeAusente(repositorioDocumento, geladeira, TipoDocumento.NotaFiscal, "Nota fiscal geladeira",
            ArquivoPdf(diretorioDocumentos, "nota-fiscal-geladeira.pdf", "Nota fiscal demonstrativa - Geladeira",
                ["Fornecedor: Casa Frio Demo", "Item: Geladeira cozinha", "Valor: BRL 3.600,00", $"Compra: {Data(hoje.AddYears(-1))}"]),
            hoje.AddYears(-1));
        SalvarDocumentoSeAusente(repositorioDocumento, geladeira, TipoDocumento.Recibo, "Recibo assistência geladeira",
            ArquivoPdf(diretorioDocumentos, "recibo-assistencia-geladeira.pdf", "Recibo de assistencia - Geladeira",
                ["Servico: troca de borracha e verificacao de vedacao", "Tecnico: Assistencia Demo", "Valor: BRL 240,00", $"Data: {Data(hoje.AddMonths(-2))}"]),
            hoje.AddMonths(-2));
        SalvarDocumentoSeAusente(repositorioDocumento, geladeira, TipoDocumento.Foto, "Foto etiqueta geladeira",
            ArquivoSvg(diretorioDocumentos, "foto-etiqueta-geladeira.svg", "Etiqueta Geladeira", "#14b8a6", "#082f49"),
            hoje.AddMonths(-1));

        SalvarDocumentoSeAusente(repositorioDocumento, furadeira, TipoDocumento.Recibo, "Recibo furadeira arquivada",
            ArquivoPdf(diretorioDocumentos, "recibo-furadeira.pdf", "Recibo demonstrativo - Furadeira",
                ["Item: Furadeira de impacto", "Status atual: arquivado", "Documento mantido para historico patrimonial."]),
            hoje.AddYears(-4));

        SalvarDocumentoSeAusente(repositorioDocumento, monitor, TipoDocumento.NotaFiscal, "Nota fiscal monitor",
            ArquivoPdf(diretorioDocumentos, "nota-fiscal-monitor.pdf", "Nota fiscal demonstrativa - Monitor",
                ["Fornecedor: Tech Store Demo", "Item: Monitor ultrawide", "Valor: BRL 1.890,00"]),
            hoje.AddMonths(-8));
        SalvarDocumentoSeAusente(repositorioDocumento, monitor, TipoDocumento.CertificadoGarantia, "Garantia monitor",
            ArquivoPdf(diretorioDocumentos, "garantia-monitor.pdf", "Garantia - Monitor ultrawide",
                ["Cobertura: loja demo", $"Vigencia final: {Data(hoje.AddMonths(16))}", "Conservar nota fiscal anexada ao bem."]),
            hoje.AddMonths(-8));
        SalvarDocumentoSeAusente(repositorioDocumento, monitor, TipoDocumento.Foto, "Foto número de série monitor",
            ArquivoSvg(diretorioDocumentos, "foto-monitor.svg", "Monitor", "#22c55e", "#052e16"),
            hoje.AddMonths(-2));

        SalvarDocumentoSeAusente(repositorioDocumento, bicicleta, TipoDocumento.NotaFiscal, "Nota fiscal bicicleta elétrica",
            ArquivoPdf(diretorioDocumentos, "nota-fiscal-bicicleta-eletrica.pdf", "Nota fiscal demonstrativa - Bicicleta eletrica",
                ["Fornecedor: Mobilidade Demo", "Item: Bicicleta eletrica", "Valor: BRL 7.200,00"]),
            hoje.AddYears(-1).AddMonths(-2));
        SalvarDocumentoSeAusente(repositorioDocumento, bicicleta, TipoDocumento.Recibo, "Recibo revisão bicicleta",
            ArquivoPdf(diretorioDocumentos, "recibo-revisao-bicicleta.pdf", "Recibo de revisao - Bicicleta eletrica",
                ["Servico: freios, corrente, bateria e calibragem", "Valor: BRL 185,00", $"Data: {Data(hoje.AddMonths(-2).AddDays(1))}"]),
            hoje.AddMonths(-2).AddDays(1));
        SalvarDocumentoSeAusente(repositorioDocumento, bicicleta, TipoDocumento.Outro, "Checklist mensal bicicleta",
            ArquivoTexto(diretorioDocumentos, "checklist-bicicleta.txt",
                ["Checklist mensal - Bicicleta eletrica", "- Pressao dos pneus conferida", "- Freios dianteiro e traseiro testados", "- Bateria carregada e sem aquecimento", "- Luzes e refletivos funcionando"]),
            hoje.AddDays(-15));
        SalvarDocumentoSeAusente(repositorioDocumento, bicicleta, TipoDocumento.Foto, "Foto bicicleta elétrica",
            ArquivoSvg(diretorioDocumentos, "foto-bicicleta.svg", "Bicicleta", "#a78bfa", "#1e1b4b"),
            hoje.AddMonths(-3));

        SalvarDocumentoSeAusente(repositorioDocumento, arCondicionado, TipoDocumento.NotaFiscal, "Nota fiscal ar-condicionado",
            ArquivoPdf(diretorioDocumentos, "nota-fiscal-ar-condicionado.pdf", "Nota fiscal demonstrativa - Ar-condicionado",
                ["Fornecedor: Clima Norte Demo", "Item: Ar-condicionado sala", "Valor: BRL 2.650,00"]),
            hoje.AddMonths(-14));
        SalvarDocumentoSeAusente(repositorioDocumento, arCondicionado, TipoDocumento.Outro, "Laudo instalação ar-condicionado",
            ArquivoPdf(diretorioDocumentos, "laudo-instalacao-ar-condicionado.pdf", "Laudo de instalacao - Ar-condicionado",
                ["Instalacao eletrica dedicada verificada.", "Dreno testado sem vazamento.", $"Proxima limpeza: {Data(hoje.AddDays(7))}"]),
            hoje.AddMonths(-14));

        SalvarDocumentoSeAusente(repositorioDocumento, sofa, TipoDocumento.Foto, "Foto sofá modular",
            ArquivoSvg(diretorioDocumentos, "foto-sofa.svg", "Sofa", "#facc15", "#422006"),
            hoje.AddMonths(-5));

        SalvarDocumentoSeAusente(repositorioDocumento, impressora, TipoDocumento.Outro, "Relatório descarte impressora",
            ArquivoPdf(diretorioDocumentos, "relatorio-descarte-impressora.pdf", "Relatorio de descarte - Impressora",
                ["Motivo: falha recorrente na placa logica.", "Status: descartado", "Registro mantido para historico patrimonial."]),
            hoje.AddYears(-1));
    }

    private static void SalvarGarantiaSeAusente(
        IRepositorioGarantia repositorioGarantia, Bem bem, TipoGarantia tipo, string fornecedor, DateOnly iniciaEm, DateOnly terminaEm)
    {
        var jaExiste = repositorioGarantia.ListarPorBem(bem.Id)
            .Any(garantia => garantia.Tipo == tipo && string.Equals(garantia.Fornecedor, fornecedor, StringComparison.OrdinalIgnoreCase));
        if (!jaExiste)
        {
            repositorioGarantia.Salvar(Garantia.Criar(bem.Id, tipo, fornecedor, iniciaEm, terminaEm));
        }
    }

    private static void SalvarManutencaoSeAusente(IRepositorioManutencao repositorioManutencao, Manutencao manutencao)
    {
        var jaExiste = repositorioManutencao.ListarPorBem(manutencao.BemId)
            .Any(existente => string.Equals(existente.Descricao, manutencao.Descricao, StringComparison.OrdinalIgnoreCase));
        if (!jaExiste)
        {
            repositorioManutencao.Salvar(manutencao);
        }
    }

    private static void SalvarDocumentoSeAusente(
        IRepositorioDocumento repositorioDocumento, Bem bem, TipoDocumento tipo, string nome, CaminhoDocumentoLocal arquivo, DateOnly registradoEm)
    {
        var jaExiste = repositorioDocumento.ListarPorBem(bem.Id)
            .Any(documento => string.Equals(documento.Nome.Valor, nome, StringComparison.OrdinalIgnoreCase));
        if (!jaExiste)
        {
            repositorioDocumento.Salvar(Documento.Registrar(bem.Id, tipo, new NomeDocumento(nome), arquivo, registradoEm));
        }
    }

    private static CaminhoDocumentoLocal ArquivoPdf(string diretorioDocumentos, string nome, string titulo, IReadOnlyList<string> linhas) =>
        ArquivoBytes(diretorioDocumentos, nome, PdfSimples(titulo, linhas));

    private static CaminhoDocumentoLocal ArquivoCsv(string diretorioDocumentos, string nome, IReadOnlyList<string> linhas) =>
        ArquivoTexto(diretorioDocumentos, nome, linhas);

    private static CaminhoDocumentoLocal ArquivoTexto(string diretorioDocumentos, string nome, IReadOnlyList<string> linhas) =>
        ArquivoBytes(diretorioDocumentos, nome, Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, linhas)));

    private static CaminhoDocumentoLocal ArquivoSvg(string diretorioDocumentos, string nome, string titulo, string corPrincipal, string corFundo)
    {
        var conteudo = $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="960" height="540" viewBox="0 0 960 540">
              <rect width="960" height="540" rx="32" fill="{corFundo}"/>
              <rect x="72" y="80" width="816" height="300" rx="24" fill="{corPrincipal}" opacity="0.18"/>
              <rect x="130" y="150" width="700" height="170" rx="18" fill="{corPrincipal}" opacity="0.28"/>
              <circle cx="790" cy="120" r="46" fill="{corPrincipal}" opacity="0.72"/>
              <text x="90" y="440" font-family="Segoe UI, Arial, sans-serif" font-size="46" font-weight="700" fill="#f8fafc">{EscaparXml(titulo)}</text>
              <text x="92" y="486" font-family="Segoe UI, Arial, sans-serif" font-size="22" fill="#cbd5e1">Imagem demonstrativa gerada para ambiente de teste</text>
            </svg>

            """;
        return ArquivoBytes(diretorioDocumentos, nome, Encoding.UTF8.GetBytes(conteudo));
    }

    private static CaminhoDocumentoLocal ArquivoBytes(string diretorioDocumentos, string nome, byte[] conteudo)
    {
        try
        {
            Directory.CreateDirectory(diretorioDocumentos);
            var arquivo = Path.GetFullPath(Path.Combine(diretorioDocumentos, nome));
            File.WriteAllBytes(arquivo, conteudo);
            return new CaminhoDocumentoLocal(arquivo);
        }
        catch (IOException excecao)
        {
            throw new InvalidOperationException("não foi possível criar documento fictício", excecao);
        }
    }

    private static byte[] PdfSimples(string titulo, IReadOnlyList<string> linhas)
    {
        var conteudo = Encoding.ASCII.GetBytes(ConteudoPdf(titulo, linhas));
        var objetos = new List<byte[]>
        {
            Bytes("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n"),
            Bytes("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n"),
            Bytes("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>\nendobj\n"),
            ObjetoStream(4, conteudo),
            Bytes("5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n")
        };

        using var saida = new MemoryStream();
        Escrever(saida, Bytes("%PDF-1.4\n"));
        var offsets = new List<int>();
        foreach (var objeto in objetos)
        {
            offsets.Add((int)saida.Length);
            Escrever(saida, objeto);
        }

        var inicioXref = (int)saida.Length;
        Escrever(saida, Bytes("xref\n"));
        Escrever(saida, Bytes($"0 {objetos.Count + 1}\n"));
        Escrever(saida, Bytes("0000000000 65535 f \n"));
        foreach (var offset in offsets)
        {
            Escrever(saida, Bytes(offset.ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n"));
        }

        Escrever(saida, Bytes("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n"));
        Escrever(saida, Bytes(inicioXref.ToString(CultureInfo.InvariantCulture)));
        Escrever(saida, Bytes("\n%%EOF\n"));
        return saida.ToArray();
    }

    private static void Escrever(MemoryStream saida, byte[] dados) => saida.Write(dados, 0, dados.Length);

    private static string ConteudoPdf(string titulo, IReadOnlyList<string> linhas)
    {
        var conteudo = new StringBuilder();
        conteudo.Append("BT\n");
        conteudo.Append("/F1 18 Tf\n");
        conteudo.Append("50 780 Td\n");
        conteudo.Append('(').Append(EscaparPdf(titulo)).Append(") Tj\n");
        conteudo.Append("/F1 11 Tf\n");
        conteudo.Append("0 -34 Td\n");
        conteudo.Append('(').Append(EscaparPdf("Assetly - ambiente de teste")).Append(") Tj\n");
        foreach (var linha in linhas)
        {
            conteudo.Append("0 -22 Td\n");
            conteudo.Append('(').Append(EscaparPdf(linha)).Append(") Tj\n");
        }

        conteudo.Append("0 -34 Td\n");
        conteudo.Append('(').Append(EscaparPdf("Arquivo gerado automaticamente para validar anexos reais.")).Append(") Tj\n");
        conteudo.Append("ET\n");
        return conteudo.ToString();
    }

    private static byte[] ObjetoStream(int numero, byte[] conteudo)
    {
        using var saida = new MemoryStream();
        Escrever(saida, Bytes($"{numero} 0 obj\n"));
        Escrever(saida, Bytes($"<< /Length {conteudo.Length} >>\n"));
        Escrever(saida, Bytes("stream\n"));
        Escrever(saida, conteudo);
        Escrever(saida, Bytes("endstream\n"));
        Escrever(saida, Bytes("endobj\n"));
        return saida.ToArray();
    }

    private static byte[] Bytes(string valor) => Encoding.ASCII.GetBytes(valor);

    private static string EscaparPdf(string valor)
    {
        var escapado = valor.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        var caracteres = escapado.Select(c => c is >= (char)0x20 and <= (char)0x7E ? c : '?').ToArray();
        return new string(caracteres);
    }

    private static string EscaparXml(string valor) =>
        valor.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static Dinheiro Dinheiro2(string valor) => Dinheiro.De(decimal.Parse(valor, CultureInfo.InvariantCulture), "BRL");

    private static string Data(DateOnly data) => Formatador.Data(data);
}
