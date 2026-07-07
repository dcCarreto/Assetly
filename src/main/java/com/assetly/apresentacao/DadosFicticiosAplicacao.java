package com.assetly.apresentacao;

import com.assetly.aplicacao.repositorio.RepositorioAlerta;
import com.assetly.aplicacao.repositorio.RepositorioBem;
import com.assetly.aplicacao.repositorio.RepositorioDocumento;
import com.assetly.aplicacao.repositorio.RepositorioGarantia;
import com.assetly.aplicacao.repositorio.RepositorioManutencao;
import com.assetly.dominio.bem.Bem;
import com.assetly.dominio.bem.NomeBem;
import com.assetly.dominio.bem.TipoBem;
import com.assetly.dominio.compartilhado.Dinheiro;
import com.assetly.dominio.documento.CaminhoDocumentoLocal;
import com.assetly.dominio.documento.Documento;
import com.assetly.dominio.documento.NomeDocumento;
import com.assetly.dominio.documento.TipoDocumento;
import com.assetly.dominio.garantia.Garantia;
import com.assetly.dominio.garantia.TipoGarantia;
import com.assetly.dominio.manutencao.Manutencao;
import com.assetly.dominio.manutencao.TipoManutencao;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.math.BigDecimal;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.time.LocalDate;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Objects;
import java.util.function.Consumer;

final class DadosFicticiosAplicacao {

    private static final DateTimeFormatter DATA_BR = DateTimeFormatter.ofPattern("dd/MM/yyyy");

    private DadosFicticiosAplicacao() {
    }

    static void popularSeNecessario(
            RepositorioBem repositorioBem,
            RepositorioGarantia repositorioGarantia,
            RepositorioManutencao repositorioManutencao,
            RepositorioDocumento repositorioDocumento,
            RepositorioAlerta repositorioAlerta,
            Path diretorioDocumentos
    ) {
        Objects.requireNonNull(repositorioBem, "repositório de bens é obrigatório");
        Objects.requireNonNull(repositorioGarantia, "repositório de garantias é obrigatório");
        Objects.requireNonNull(repositorioManutencao, "repositório de manutenções é obrigatório");
        Objects.requireNonNull(repositorioDocumento, "repositório de documentos é obrigatório");
        Objects.requireNonNull(repositorioAlerta, "repositório de alertas é obrigatório");
        Objects.requireNonNull(diretorioDocumentos, "diretório de documentos é obrigatório");

        var hoje = LocalDate.now();
        var bens = bensPorNome(repositorioBem);

        var notebook = garantirBem(
                repositorioBem,
                bens,
                "Notebook de trabalho",
                TipoBem.ELETRONICO,
                hoje.minusYears(2),
                dinheiro("5200.00"),
                bem -> bem.atualizarObservacoes(
                        "Ambiente de teste: equipamento principal com nota fiscal, garantia próxima e manutenção agendada."
                )
        );
        var camera = garantirBem(
                repositorioBem,
                bens,
                "Câmera mirrorless",
                TipoBem.ELETRONICO,
                hoje.minusYears(3),
                dinheiro("4100.00"),
                bem -> bem.atualizarObservacoes(
                        "Ambiente de teste: bem sem nota fiscal, com garantia vencida e manutenção atrasada."
                )
        );
        var geladeira = garantirBem(
                repositorioBem,
                bens,
                "Geladeira cozinha",
                TipoBem.ELETRODOMESTICO,
                hoje.minusYears(1),
                dinheiro("3600.00"),
                bem -> {
                    bem.marcarEmManutencao();
                    bem.atualizarObservacoes("Ambiente de teste: item em manutenção com recibos e histórico de assistência.");
                }
        );
        var furadeira = garantirBem(
                repositorioBem,
                bens,
                "Furadeira de impacto",
                TipoBem.FERRAMENTA,
                hoje.minusYears(4),
                dinheiro("420.00"),
                bem -> {
                    bem.arquivar();
                    bem.atualizarObservacoes("Ambiente de teste: item arquivado para validar estados históricos.");
                }
        );
        var monitor = garantirBem(
                repositorioBem,
                bens,
                "Monitor ultrawide",
                TipoBem.ELETRONICO,
                hoje.minusMonths(8),
                dinheiro("1890.00"),
                bem -> bem.atualizarObservacoes("Ambiente de teste: item ativo com garantia saudável e documentos completos.")
        );
        var bicicleta = garantirBem(
                repositorioBem,
                bens,
                "Bicicleta elétrica",
                TipoBem.VEICULO,
                hoje.minusYears(1).minusMonths(2),
                dinheiro("7200.00"),
                bem -> bem.atualizarObservacoes("Ambiente de teste: veículo leve com revisão concluída e checklist anexado.")
        );
        var arCondicionado = garantirBem(
                repositorioBem,
                bens,
                "Ar-condicionado sala",
                TipoBem.ELETRODOMESTICO,
                hoje.minusMonths(14),
                dinheiro("2650.00"),
                bem -> bem.atualizarObservacoes("Ambiente de teste: equipamento com instalação documentada e limpeza futura.")
        );
        var sofa = garantirBem(
                repositorioBem,
                bens,
                "Sofá modular",
                TipoBem.MOVEL,
                hoje.minusYears(5),
                dinheiro("3100.00"),
                bem -> bem.atualizarObservacoes("Ambiente de teste: móvel ativo sem nota fiscal para gerar alerta documental.")
        );
        var impressora = garantirBem(
                repositorioBem,
                bens,
                "Impressora multifuncional",
                TipoBem.ELETRONICO,
                hoje.minusYears(6),
                dinheiro("980.00"),
                bem -> {
                    bem.descartar();
                    bem.atualizarObservacoes("Ambiente de teste: item descartado, útil para validar histórico e status final.");
                }
        );

        popularGarantias(repositorioGarantia, hoje, notebook, camera, monitor, bicicleta, arCondicionado);
        popularManutencoes(repositorioManutencao, hoje, notebook, camera, geladeira, bicicleta, arCondicionado, impressora);
        popularDocumentos(
                repositorioDocumento,
                diretorioDocumentos,
                hoje,
                notebook,
                camera,
                geladeira,
                furadeira,
                monitor,
                bicicleta,
                arCondicionado,
                sofa,
                impressora
        );

        new com.assetly.aplicacao.casodeuso.GerarAlertasAutomaticos(
                repositorioBem,
                repositorioGarantia,
                repositorioManutencao,
                repositorioDocumento,
                repositorioAlerta
        ).executar(hoje);
    }

    private static Map<String, Bem> bensPorNome(RepositorioBem repositorioBem) {
        var bens = new LinkedHashMap<String, Bem>();
        for (var bem : repositorioBem.listar()) {
            bens.put(bem.nome().valor(), bem);
        }
        return bens;
    }

    private static Bem garantirBem(
            RepositorioBem repositorioBem,
            Map<String, Bem> bens,
            String nome,
            TipoBem tipo,
            LocalDate compradoEm,
            Dinheiro precoCompra,
            Consumer<Bem> preparar
    ) {
        var existente = bens.get(nome);
        if (existente != null) {
            return existente;
        }

        var bem = Bem.criar(new NomeBem(nome), tipo, compradoEm, precoCompra);
        preparar.accept(bem);
        repositorioBem.salvar(bem);
        bens.put(nome, bem);
        return bem;
    }

    private static void popularGarantias(
            RepositorioGarantia repositorioGarantia,
            LocalDate hoje,
            Bem notebook,
            Bem camera,
            Bem monitor,
            Bem bicicleta,
            Bem arCondicionado
    ) {
        salvarGarantiaSeAusente(
                repositorioGarantia,
                notebook,
                TipoGarantia.FABRICANTE,
                "Fabricante Demo",
                hoje.minusYears(1),
                hoje.plusDays(20)
        );
        salvarGarantiaSeAusente(
                repositorioGarantia,
                camera,
                TipoGarantia.ESTENDIDA,
                "Loja Demo",
                hoje.minusYears(2),
                hoje.minusDays(5)
        );
        salvarGarantiaSeAusente(
                repositorioGarantia,
                monitor,
                TipoGarantia.LOJA,
                "Tech Store Demo",
                hoje.minusMonths(8),
                hoje.plusMonths(16)
        );
        salvarGarantiaSeAusente(
                repositorioGarantia,
                bicicleta,
                TipoGarantia.SERVICO,
                "Oficina Bike Demo",
                hoje.minusMonths(10),
                hoje.plusDays(12)
        );
        salvarGarantiaSeAusente(
                repositorioGarantia,
                arCondicionado,
                TipoGarantia.FABRICANTE,
                "Clima Norte Demo",
                hoje.minusMonths(14),
                hoje.plusMonths(10)
        );
    }

    private static void popularManutencoes(
            RepositorioManutencao repositorioManutencao,
            LocalDate hoje,
            Bem notebook,
            Bem camera,
            Bem geladeira,
            Bem bicicleta,
            Bem arCondicionado,
            Bem impressora
    ) {
        salvarManutencaoSeAusente(
                repositorioManutencao,
                Manutencao.agendar(
                        notebook.id(),
                        TipoManutencao.PREVENTIVA,
                        "Limpeza interna e troca de pasta térmica",
                        hoje.plusDays(5)
                )
        );
        salvarManutencaoSeAusente(
                repositorioManutencao,
                Manutencao.agendar(
                        camera.id(),
                        TipoManutencao.CORRETIVA,
                        "Revisar sensor e lente",
                        hoje.minusDays(3)
                )
        );
        salvarManutencaoSeAusente(
                repositorioManutencao,
                Manutencao.agendar(
                        geladeira.id(),
                        TipoManutencao.PREVENTIVA,
                        "Verificar vedação e limpeza do condensador",
                        hoje.plusDays(21)
                )
        );

        var revisaoBicicleta = Manutencao.agendar(
                bicicleta.id(),
                TipoManutencao.INSPECAO,
                "Revisão de freios, corrente, bateria e calibragem",
                hoje.minusMonths(2)
        );
        revisaoBicicleta.concluir(hoje.minusMonths(2).plusDays(1), dinheiro("185.00"));
        salvarManutencaoSeAusente(repositorioManutencao, revisaoBicicleta);

        var limpezaAr = Manutencao.agendar(
                arCondicionado.id(),
                TipoManutencao.LIMPEZA,
                "Limpeza de filtros e higienização preventiva",
                hoje.plusDays(7)
        );
        salvarManutencaoSeAusente(repositorioManutencao, limpezaAr);

        var diagnosticoImpressora = Manutencao.agendar(
                impressora.id(),
                TipoManutencao.CORRETIVA,
                "Diagnóstico de falha na placa lógica",
                hoje.minusYears(1)
        );
        diagnosticoImpressora.cancelar();
        salvarManutencaoSeAusente(repositorioManutencao, diagnosticoImpressora);
    }

    private static void popularDocumentos(
            RepositorioDocumento repositorioDocumento,
            Path diretorioDocumentos,
            LocalDate hoje,
            Bem notebook,
            Bem camera,
            Bem geladeira,
            Bem furadeira,
            Bem monitor,
            Bem bicicleta,
            Bem arCondicionado,
            Bem sofa,
            Bem impressora
    ) {
        salvarDocumentoSeAusente(
                repositorioDocumento,
                notebook,
                TipoDocumento.NOTA_FISCAL,
                "Nota fiscal notebook PDF",
                arquivoPdf(
                        diretorioDocumentos,
                        "nota-fiscal-notebook.pdf",
                        "Nota fiscal demonstrativa - Notebook",
                        List.of(
                                "Fornecedor: Tech Store Demo",
                                "Item: Notebook de trabalho",
                                "Valor: BRL 5.200,00",
                                "Compra: " + data(hoje.minusYears(2)),
                                "Documento gerado para ambiente de teste."
                        )
                ),
                hoje.minusYears(2)
        );
        salvarDocumentoSeAusente(
                repositorioDocumento,
                notebook,
                TipoDocumento.CERTIFICADO_GARANTIA,
                "Certificado garantia notebook",
                arquivoPdf(
                        diretorioDocumentos,
                        "certificado-garantia-notebook.pdf",
                        "Certificado de garantia - Notebook",
                        List.of(
                                "Cobertura: fabricante demo",
                                "Vigencia final: " + data(hoje.plusDays(20)),
                                "Suporte: suporte.notebook.demo@example.test"
                        )
                ),
                hoje.minusYears(1)
        );
        salvarDocumentoSeAusente(
                repositorioDocumento,
                notebook,
                TipoDocumento.MANUAL,
                "Manual rápido notebook",
                arquivoPdf(
                        diretorioDocumentos,
                        "manual-notebook.pdf",
                        "Manual rapido - Notebook",
                        List.of(
                                "1. Manter ventilacao livre.",
                                "2. Executar backup semanal.",
                                "3. Limpar teclado com pano seco.",
                                "4. Agendar manutencao preventiva a cada 12 meses."
                        )
                ),
                hoje.minusYears(2)
        );
        salvarDocumentoSeAusente(
                repositorioDocumento,
                notebook,
                TipoDocumento.FOTO,
                "Foto identificação notebook",
                arquivoSvg(
                        diretorioDocumentos,
                        "foto-notebook.svg",
                        "Notebook",
                        "#38bdf8",
                        "#0f172a"
                ),
                hoje.minusMonths(6)
        );
        salvarDocumentoSeAusente(
                repositorioDocumento,
                notebook,
                TipoDocumento.OUTRO,
                "Histórico manutenção notebook",
                arquivoCsv(
                        diretorioDocumentos,
                        "historico-manutencao-notebook.csv",
                        List.of(
                                "data;servico;valor;observacao",
                                data(hoje.minusMonths(14)) + ";limpeza preventiva;120.00;sem avarias",
                                data(hoje.minusMonths(7)) + ";troca pasta termica;95.00;temperatura normalizada"
                        )
                ),
                hoje.minusMonths(7)
        );

        salvarDocumentoSeAusente(
                repositorioDocumento,
                camera,
                TipoDocumento.MANUAL,
                "Manual câmera mirrorless",
                arquivoPdf(
                        diretorioDocumentos,
                        "manual-camera.pdf",
                        "Manual resumido - Camera mirrorless",
                        List.of(
                                "Limpar sensor apenas em assistencia autorizada.",
                                "Guardar lentes em local seco.",
                                "Atualizar firmware antes de eventos importantes."
                        )
                ),
                hoje.minusYears(3)
        );
        salvarDocumentoSeAusente(
                repositorioDocumento,
                camera,
                TipoDocumento.FOTO,
                "Foto câmera mirrorless",
                arquivoSvg(diretorioDocumentos, "foto-camera.svg", "Camera", "#f97316", "#111827"),
                hoje.minusMonths(8)
        );

        salvarDocumentoSeAusente(
                repositorioDocumento,
                geladeira,
                TipoDocumento.NOTA_FISCAL,
                "Nota fiscal geladeira",
                arquivoPdf(
                        diretorioDocumentos,
                        "nota-fiscal-geladeira.pdf",
                        "Nota fiscal demonstrativa - Geladeira",
                        List.of(
                                "Fornecedor: Casa Frio Demo",
                                "Item: Geladeira cozinha",
                                "Valor: BRL 3.600,00",
                                "Compra: " + data(hoje.minusYears(1))
                        )
                ),
                hoje.minusYears(1)
        );
        salvarDocumentoSeAusente(
                repositorioDocumento,
                geladeira,
                TipoDocumento.RECIBO,
                "Recibo assistência geladeira",
                arquivoPdf(
                        diretorioDocumentos,
                        "recibo-assistencia-geladeira.pdf",
                        "Recibo de assistencia - Geladeira",
                        List.of(
                                "Servico: troca de borracha e verificacao de vedacao",
                                "Tecnico: Assistencia Demo",
                                "Valor: BRL 240,00",
                                "Data: " + data(hoje.minusMonths(2))
                        )
                ),
                hoje.minusMonths(2)
        );
        salvarDocumentoSeAusente(
                repositorioDocumento,
                geladeira,
                TipoDocumento.FOTO,
                "Foto etiqueta geladeira",
                arquivoSvg(diretorioDocumentos, "foto-etiqueta-geladeira.svg", "Etiqueta Geladeira", "#14b8a6", "#082f49"),
                hoje.minusMonths(1)
        );

        salvarDocumentoSeAusente(
                repositorioDocumento,
                furadeira,
                TipoDocumento.RECIBO,
                "Recibo furadeira arquivada",
                arquivoPdf(
                        diretorioDocumentos,
                        "recibo-furadeira.pdf",
                        "Recibo demonstrativo - Furadeira",
                        List.of(
                                "Item: Furadeira de impacto",
                                "Status atual: arquivado",
                                "Documento mantido para historico patrimonial."
                        )
                ),
                hoje.minusYears(4)
        );

        salvarDocumentoSeAusente(
                repositorioDocumento,
                monitor,
                TipoDocumento.NOTA_FISCAL,
                "Nota fiscal monitor",
                arquivoPdf(
                        diretorioDocumentos,
                        "nota-fiscal-monitor.pdf",
                        "Nota fiscal demonstrativa - Monitor",
                        List.of(
                                "Fornecedor: Tech Store Demo",
                                "Item: Monitor ultrawide",
                                "Valor: BRL 1.890,00"
                        )
                ),
                hoje.minusMonths(8)
        );
        salvarDocumentoSeAusente(
                repositorioDocumento,
                monitor,
                TipoDocumento.CERTIFICADO_GARANTIA,
                "Garantia monitor",
                arquivoPdf(
                        diretorioDocumentos,
                        "garantia-monitor.pdf",
                        "Garantia - Monitor ultrawide",
                        List.of(
                                "Cobertura: loja demo",
                                "Vigencia final: " + data(hoje.plusMonths(16)),
                                "Conservar nota fiscal anexada ao bem."
                        )
                ),
                hoje.minusMonths(8)
        );
        salvarDocumentoSeAusente(
                repositorioDocumento,
                monitor,
                TipoDocumento.FOTO,
                "Foto número de série monitor",
                arquivoSvg(diretorioDocumentos, "foto-monitor.svg", "Monitor", "#22c55e", "#052e16"),
                hoje.minusMonths(2)
        );

        salvarDocumentoSeAusente(
                repositorioDocumento,
                bicicleta,
                TipoDocumento.NOTA_FISCAL,
                "Nota fiscal bicicleta elétrica",
                arquivoPdf(
                        diretorioDocumentos,
                        "nota-fiscal-bicicleta-eletrica.pdf",
                        "Nota fiscal demonstrativa - Bicicleta eletrica",
                        List.of(
                                "Fornecedor: Mobilidade Demo",
                                "Item: Bicicleta eletrica",
                                "Valor: BRL 7.200,00"
                        )
                ),
                hoje.minusYears(1).minusMonths(2)
        );
        salvarDocumentoSeAusente(
                repositorioDocumento,
                bicicleta,
                TipoDocumento.RECIBO,
                "Recibo revisão bicicleta",
                arquivoPdf(
                        diretorioDocumentos,
                        "recibo-revisao-bicicleta.pdf",
                        "Recibo de revisao - Bicicleta eletrica",
                        List.of(
                                "Servico: freios, corrente, bateria e calibragem",
                                "Valor: BRL 185,00",
                                "Data: " + data(hoje.minusMonths(2).plusDays(1))
                        )
                ),
                hoje.minusMonths(2).plusDays(1)
        );
        salvarDocumentoSeAusente(
                repositorioDocumento,
                bicicleta,
                TipoDocumento.OUTRO,
                "Checklist mensal bicicleta",
                arquivoTexto(
                        diretorioDocumentos,
                        "checklist-bicicleta.txt",
                        List.of(
                                "Checklist mensal - Bicicleta eletrica",
                                "- Pressao dos pneus conferida",
                                "- Freios dianteiro e traseiro testados",
                                "- Bateria carregada e sem aquecimento",
                                "- Luzes e refletivos funcionando"
                        )
                ),
                hoje.minusDays(15)
        );
        salvarDocumentoSeAusente(
                repositorioDocumento,
                bicicleta,
                TipoDocumento.FOTO,
                "Foto bicicleta elétrica",
                arquivoSvg(diretorioDocumentos, "foto-bicicleta.svg", "Bicicleta", "#a78bfa", "#1e1b4b"),
                hoje.minusMonths(3)
        );

        salvarDocumentoSeAusente(
                repositorioDocumento,
                arCondicionado,
                TipoDocumento.NOTA_FISCAL,
                "Nota fiscal ar-condicionado",
                arquivoPdf(
                        diretorioDocumentos,
                        "nota-fiscal-ar-condicionado.pdf",
                        "Nota fiscal demonstrativa - Ar-condicionado",
                        List.of(
                                "Fornecedor: Clima Norte Demo",
                                "Item: Ar-condicionado sala",
                                "Valor: BRL 2.650,00"
                        )
                ),
                hoje.minusMonths(14)
        );
        salvarDocumentoSeAusente(
                repositorioDocumento,
                arCondicionado,
                TipoDocumento.OUTRO,
                "Laudo instalação ar-condicionado",
                arquivoPdf(
                        diretorioDocumentos,
                        "laudo-instalacao-ar-condicionado.pdf",
                        "Laudo de instalacao - Ar-condicionado",
                        List.of(
                                "Instalacao eletrica dedicada verificada.",
                                "Dreno testado sem vazamento.",
                                "Proxima limpeza: " + data(hoje.plusDays(7))
                        )
                ),
                hoje.minusMonths(14)
        );

        salvarDocumentoSeAusente(
                repositorioDocumento,
                sofa,
                TipoDocumento.FOTO,
                "Foto sofá modular",
                arquivoSvg(diretorioDocumentos, "foto-sofa.svg", "Sofa", "#facc15", "#422006"),
                hoje.minusMonths(5)
        );

        salvarDocumentoSeAusente(
                repositorioDocumento,
                impressora,
                TipoDocumento.OUTRO,
                "Relatório descarte impressora",
                arquivoPdf(
                        diretorioDocumentos,
                        "relatorio-descarte-impressora.pdf",
                        "Relatorio de descarte - Impressora",
                        List.of(
                                "Motivo: falha recorrente na placa logica.",
                                "Status: descartado",
                                "Registro mantido para historico patrimonial."
                        )
                ),
                hoje.minusYears(1)
        );
    }

    private static void salvarGarantiaSeAusente(
            RepositorioGarantia repositorioGarantia,
            Bem bem,
            TipoGarantia tipo,
            String fornecedor,
            LocalDate iniciaEm,
            LocalDate terminaEm
    ) {
        var jaExiste = repositorioGarantia.listarPorBem(bem.id()).stream()
                .anyMatch(garantia -> garantia.tipo() == tipo
                        && garantia.fornecedor().equalsIgnoreCase(fornecedor));
        if (!jaExiste) {
            repositorioGarantia.salvar(Garantia.criar(bem.id(), tipo, fornecedor, iniciaEm, terminaEm));
        }
    }

    private static void salvarManutencaoSeAusente(
            RepositorioManutencao repositorioManutencao,
            Manutencao manutencao
    ) {
        var jaExiste = repositorioManutencao.listarPorBem(manutencao.bemId()).stream()
                .anyMatch(existente -> existente.descricao().equalsIgnoreCase(manutencao.descricao()));
        if (!jaExiste) {
            repositorioManutencao.salvar(manutencao);
        }
    }

    private static void salvarDocumentoSeAusente(
            RepositorioDocumento repositorioDocumento,
            Bem bem,
            TipoDocumento tipo,
            String nome,
            CaminhoDocumentoLocal arquivo,
            LocalDate registradoEm
    ) {
        var jaExiste = repositorioDocumento.listarPorBem(bem.id()).stream()
                .anyMatch(documento -> documento.nome().valor().equalsIgnoreCase(nome));
        if (!jaExiste) {
            repositorioDocumento.salvar(Documento.registrar(
                    bem.id(),
                    tipo,
                    new NomeDocumento(nome),
                    arquivo,
                    registradoEm
            ));
        }
    }

    private static CaminhoDocumentoLocal arquivoPdf(
            Path diretorioDocumentos,
            String nome,
            String titulo,
            List<String> linhas
    ) {
        return arquivoBytes(diretorioDocumentos, nome, pdfSimples(titulo, linhas));
    }

    private static CaminhoDocumentoLocal arquivoCsv(Path diretorioDocumentos, String nome, List<String> linhas) {
        return arquivoTexto(diretorioDocumentos, nome, linhas);
    }

    private static CaminhoDocumentoLocal arquivoTexto(Path diretorioDocumentos, String nome, List<String> linhas) {
        return arquivoBytes(
                diretorioDocumentos,
                nome,
                String.join(System.lineSeparator(), linhas).getBytes(StandardCharsets.UTF_8)
        );
    }

    private static CaminhoDocumentoLocal arquivoSvg(
            Path diretorioDocumentos,
            String nome,
            String titulo,
            String corPrincipal,
            String corFundo
    ) {
        var conteudo = """
                <svg xmlns="http://www.w3.org/2000/svg" width="960" height="540" viewBox="0 0 960 540">
                  <rect width="960" height="540" rx="32" fill="%s"/>
                  <rect x="72" y="80" width="816" height="300" rx="24" fill="%s" opacity="0.18"/>
                  <rect x="130" y="150" width="700" height="170" rx="18" fill="%s" opacity="0.28"/>
                  <circle cx="790" cy="120" r="46" fill="%s" opacity="0.72"/>
                  <text x="90" y="440" font-family="Segoe UI, Arial, sans-serif" font-size="46" font-weight="700" fill="#f8fafc">%s</text>
                  <text x="92" y="486" font-family="Segoe UI, Arial, sans-serif" font-size="22" fill="#cbd5e1">Imagem demonstrativa gerada para ambiente de teste</text>
                </svg>
                """.formatted(corFundo, corPrincipal, corPrincipal, corPrincipal, escaparXml(titulo));
        return arquivoBytes(diretorioDocumentos, nome, conteudo.getBytes(StandardCharsets.UTF_8));
    }

    private static CaminhoDocumentoLocal arquivoBytes(Path diretorioDocumentos, String nome, byte[] conteudo) {
        try {
            Files.createDirectories(diretorioDocumentos);
            var arquivo = diretorioDocumentos.resolve(nome).toAbsolutePath().normalize();
            Files.write(arquivo, conteudo);
            return new CaminhoDocumentoLocal(arquivo);
        } catch (IOException excecao) {
            throw new IllegalStateException("não foi possível criar documento fictício", excecao);
        }
    }

    private static byte[] pdfSimples(String titulo, List<String> linhas) {
        var conteudo = conteudoPdf(titulo, linhas).getBytes(StandardCharsets.US_ASCII);
        var objetos = new ArrayList<byte[]>();
        objetos.add(bytes("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n"));
        objetos.add(bytes("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n"));
        objetos.add(bytes("""
                3 0 obj
                << /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>
                endobj
                """));
        objetos.add(objetoStream(4, conteudo));
        objetos.add(bytes("5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n"));

        var saida = new ByteArrayOutputStream();
        saida.writeBytes(bytes("%PDF-1.4\n"));
        var offsets = new ArrayList<Integer>();
        for (var objeto : objetos) {
            offsets.add(saida.size());
            saida.writeBytes(objeto);
        }

        var inicioXref = saida.size();
        saida.writeBytes(bytes("xref\n"));
        saida.writeBytes(bytes("0 " + (objetos.size() + 1) + "\n"));
        saida.writeBytes(bytes("0000000000 65535 f \n"));
        for (var offset : offsets) {
            saida.writeBytes(bytes(String.format(Locale.ROOT, "%010d 00000 n \n", offset)));
        }
        saida.writeBytes(bytes("""
                trailer
                << /Size 6 /Root 1 0 R >>
                startxref
                """));
        saida.writeBytes(bytes(Integer.toString(inicioXref)));
        saida.writeBytes(bytes("\n%%EOF\n"));
        return saida.toByteArray();
    }

    private static String conteudoPdf(String titulo, List<String> linhas) {
        var conteudo = new StringBuilder();
        conteudo.append("BT\n");
        conteudo.append("/F1 18 Tf\n");
        conteudo.append("50 780 Td\n");
        conteudo.append("(").append(escaparPdf(titulo)).append(") Tj\n");
        conteudo.append("/F1 11 Tf\n");
        conteudo.append("0 -34 Td\n");
        conteudo.append("(").append(escaparPdf("Assetly - ambiente de teste")).append(") Tj\n");
        for (var linha : linhas) {
            conteudo.append("0 -22 Td\n");
            conteudo.append("(").append(escaparPdf(linha)).append(") Tj\n");
        }
        conteudo.append("0 -34 Td\n");
        conteudo.append("(").append(escaparPdf("Arquivo gerado automaticamente para validar anexos reais.")).append(") Tj\n");
        conteudo.append("ET\n");
        return conteudo.toString();
    }

    private static byte[] objetoStream(int numero, byte[] conteudo) {
        var saida = new ByteArrayOutputStream();
        saida.writeBytes(bytes(numero + " 0 obj\n"));
        saida.writeBytes(bytes("<< /Length " + conteudo.length + " >>\n"));
        saida.writeBytes(bytes("stream\n"));
        saida.writeBytes(conteudo);
        saida.writeBytes(bytes("endstream\n"));
        saida.writeBytes(bytes("endobj\n"));
        return saida.toByteArray();
    }

    private static byte[] bytes(String valor) {
        return valor.getBytes(StandardCharsets.US_ASCII);
    }

    private static String escaparPdf(String valor) {
        return valor.replace("\\", "\\\\")
                .replace("(", "\\(")
                .replace(")", "\\)")
                .replaceAll("[^\\x20-\\x7E]", "?");
    }

    private static String escaparXml(String valor) {
        return valor.replace("&", "&amp;")
                .replace("<", "&lt;")
                .replace(">", "&gt;")
                .replace("\"", "&quot;");
    }

    private static Dinheiro dinheiro(String valor) {
        return Dinheiro.de(new BigDecimal(valor), "BRL");
    }

    private static String data(LocalDate data) {
        return data.format(DATA_BR);
    }
}
