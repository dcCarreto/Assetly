# Roteiro do Assetly

Este roteiro organiza o desenvolvimento do Assetly em etapas pequenas, com foco em um produto mínimo viável de área de trabalho com prioridade local em .NET 10, WPF e SQLite. O projeto nasceu em Java 21/JavaFX/Maven (etapas 1 a 8) e foi reescrito integralmente para C#/.NET WPF na etapa 10; as etapas anteriores permanecem registradas como histórico.

## Situação Atual

- Base .NET 10/WPF criada, organizada em cinco projetos (`Assetly.Dominio`, `Assetly.Aplicacao`, `Assetly.Infraestrutura`, `Assetly.Apresentacao`, `Assetly.Testes`).
- Domínio principal implementado e coberto por testes unitários.
- Casos de uso, comandos e contratos de repositório implementados.
- Persistência SQLite local implementada com migração versionada.
- Interface WPF funcional implementada.
- Identidade visual própria, com tema escuro (padrão) ou claro, quatro cores de destaque e densidade de tabela configuráveis.
- Navegação lateral, painel operacional, indicadores, prioridades e estados visuais implementados.
- Ícone estilizado do aplicativo criado e aplicado na janela e no cabeçalho.
- Documentos locais são copiados para armazenamento privado em `~/.assetly/documentos/`.
- Alertas automáticos geram avisos de garantia, manutenção, convênio e nota fiscal ausente sem duplicar registros, e se resolvem sozinhos quando a pendência deixa de existir.
- Exclusão de registros em todos os módulos (com confirmação), além de arquivar/desarquivar e descartar bens.
- Backup manual e automático com cópia consistente do SQLite, e restauração com cópia de segurança do estado anterior.
- Executável único autocontido para Windows via `dotnet publish` (perfil `win-x64`).
- Ambiente de teste separado implementado em `~/.assetly-teste/`, com dados fictícios completos e anexos físicos em PDF, SVG, CSV e TXT.
- Alternância produção/teste via perfis de execução da Visual Studio ou argumento `--teste`/variável `ASSETLY_AMBIENTE` pela linha de comando.
- Instalador Inno Setup por usuário e atualização automática pelas Releases do GitHub, com SHA-256 conferido; versões identificadas pela data de publicação (`AAAA.MM.DD`).
- Patrimônio com depreciação estimada, modo família, notas fiscais eletrônicas (QR Code da NFC-e e OCR), exportação CSV/JSON, backup criptografado e sincronização opcional entre computadores.
- CI no GitHub Actions roda os testes e o autoteste do programa em `main` e `desenvolvimento`; o workflow de Release testa, empacota, instala, testa o programa instalado e só então publica.
- README atualizado com capturas de tela, instalação, execução, privacidade, todos os módulos, backup/restauração/sincronização, atalhos, processo de release e solução de problemas.
- Primeira release publicada: `2026.11.23`.

## Princípios do Projeto

- Manter o domínio isolado de WPF, ADO.NET, SQLite e detalhes de infraestrutura.
- Priorizar um produto mínimo viável simples, usável e fácil de rodar.
- Evitar dependências desnecessárias no início.
- Não versionar bancos locais, documentos reais, anexos, notas fiscais, fotos ou arquivos sensíveis.
- Não fazer commits, pushes ou tags automaticamente.
- Manter nomes de pacotes, classes e documentação de negócio em português.
- Manter convenções do ecossistema .NET e arquivos padrão do repositório em inglês, como `src/Assetly.Dominio`, `src/Assetly.Aplicacao`, `src/Assetly.Infraestrutura`, `src/Assetly.Apresentacao`, `testes/Assetly.Testes`, `README.md`, `LICENSE` e `ROADMAP.md`.
- Manter o tema escuro como padrão; o tema claro é opcional em Configurações.

## Etapa 1 - Base do Projeto

Objetivo: preparar a fundação do repositório e da aplicação.

- Criar estrutura Maven com Java 21.
- Configurar JavaFX.
- Criar `.gitignore` seguro.
- Criar `README.md` inicial.
- Adicionar licença MIT em `LICENSE`.
- Criar pacotes base: `dominio`, `aplicacao`, `infraestrutura` e `apresentacao`.
- Criar `AssetlyAplicacao`.
- Definir onde SQLite e arquivos locais serão armazenados.

Resultado esperado: aplicação JavaFX mínima abrindo sem funcionalidades complexas.

Situação: implementado.

## Etapa 2 - Domínio Principal

Objetivo: implementar as regras centrais sem dependência de arcabouço.

- Criar entidades e objetos de valor para `Bem`, `Garantia`, `Manutencao`, `Documento` e `Alerta`.
- Criar enums de status e tipos.
- Implementar validações básicas.
- Adicionar testes unitários para regras importantes.

Resultado esperado: domínio testável, claro e independente da interface e do banco.

Situação: implementado com entidades, objetos de valor, enums, validações e testes unitários.

## Etapa 3 - Casos de Uso

Objetivo: organizar os fluxos da aplicação.

- Criar casos de uso para cadastro, edição, listagem e mudança de status de bens.
- Criar casos de uso para garantias, manutenções, documentos e alertas.
- Definir contratos de repositório usados pela camada de aplicação.

Resultado esperado: camada de aplicação orquestrando o domínio sem conhecer JavaFX ou SQLite.

Situação: implementado com casos de uso, comandos de entrada e contratos de repositório em português.

## Etapa 4 - Persistência SQLite

Objetivo: salvar dados localmente de forma simples.

- Criar conexão SQLite.
- Criar scripts SQL versionados para estrutura do banco.
- Implementar repositórios e mapeadores.
- Criar banco local automaticamente ao iniciar o aplicativo.

Resultado esperado: dados persistidos localmente, sem versionar arquivos reais de banco.

Situação: implementado com conexão SQLite, migração SQL versionada, repositórios SQLite e criação automática do banco local ao iniciar a aplicação.

## Etapa 5 - Interface JavaFX Básica

Objetivo: entregar uma interface funcional para o produto mínimo viável.

- Criar painel simples.
- Criar telas de listagem, cadastro, edição e detalhes de bens.
- Criar telas de garantias, manutenções, documentos e alertas.
- Exibir estados visuais para garantia ativa, vencida e próxima do vencimento.

Resultado esperado: usuário consegue navegar pelo fluxo principal do app.

Situação: implementado e evoluído com painel, navegação lateral, listagens, formulários, ações básicas, tema escuro fixo, identidade visual, ícone do aplicativo, indicadores operacionais, prioridades e estados visuais para bens, garantias, manutenções, documentos e alertas.

## Etapa 6 - Documentos Locais

Objetivo: registrar documentos relacionados aos bens sem expor arquivos pessoais.

- Registrar metadados de documentos.
- Copiar arquivos para armazenamento local ignorado pelo Git, como `~/.assetly/documentos/`.
- Salvar no banco apenas metadados e caminho local.

Resultado esperado: documentos associados aos bens sem risco de commit acidental.

Situação: implementado com serviço de armazenamento local, cópia de arquivos para `~/.assetly/documentos/`, seleção de arquivo na interface e persistência do caminho local armazenado.

## Etapa 7 - Alertas Automáticos

Objetivo: gerar avisos úteis para o usuário.

- Alertar garantias vencendo em até 30 dias.
- Alertar garantias vencidas.
- Alertar manutenções próximas ou atrasadas.
- Alertar ausência de nota fiscal quando aplicável.
- Testar a lógica de geração de alertas.

Resultado esperado: painel de alertas com informações acionáveis.

Situação: implementado com geração automática ao atualizar a aplicação, cobrindo garantias vencendo/vencidas, manutenções próximas/atrasadas e ausência de nota fiscal para bens ativos ou em manutenção.

## Etapa 8 - Polimento do produto mínimo viável

Objetivo: preparar o projeto para uso e apresentação código aberto.

- Melhorar mensagens de erro e validações.
- Revisar layout e navegação.
- Consolidar identidade visual escura fixa.
- Aplicar ícone do aplicativo na janela e na interface.
- Refinar painel com indicadores e prioridades acionáveis.
- Completar `README.md` com instruções de execução e contribuição.
- Revisar testes.
- Criar ambiente de teste isolado com dados fictícios completos e anexos reais.
- Criar execução simples por variável de ambiente para produção e teste.
- Adicionar Maven Wrapper e validação automatizada básica para suportar trabalho por branches.
- Conferir `git status` antes de qualquer sugestão de commit.

Resultado esperado: produto mínimo viável organizado, documentado e pronto para evoluir publicamente.

Situação: implementado. O polimento visual, a navegação lateral, o painel operacional, os estados visuais, o ícone do aplicativo, as mensagens de erro, a documentação, o ambiente de teste com dados fictícios, o script de alternância por variável de ambiente, o Maven Wrapper e o CI básico foram revisados. Empacotamento e distribuição seguem na Etapa 9.

## Etapa 9 - Empacotamento e Distribuição

Objetivo: facilitar a execução do Assetly fora do ambiente de desenvolvimento.

- Definir estratégia de empacotamento com .NET (self-contained deployment).
- Gerar artefato executável local.
- Avaliar empacotamento com `dotnet publish` ou MSIX.
- Incluir ícone do aplicativo nos pacotes gerados.
- Documentar requisitos de execução.

Resultado esperado: usuário consegue baixar ou gerar uma versão executável do Assetly com menos configuração manual.

Situação: implementado. Instalador Inno Setup (`instalador/Assetly.iss`): instalação por usuário em `%LOCALAPPDATA%\Programs\Assetly`, sem administrador, atalhos (inclusive para o ambiente de teste) e desinstalador que preserva os dados. Versões sem número: cada release é identificada pela data (`AAAA.MM.DD`, com sequencial se houver mais de uma no dia). O workflow `release.yml` calcula a data, roda os testes, publica o executável autocontido, executa o autoteste (`--autoteste`), gera o instalador, instala em silêncio, roda o autoteste do programa instalado, desinstala, gera o SHA-256 e publica a Release. O app instalado procura versão nova ao abrir e a cada 6 horas, baixa o instalador, confere o SHA-256 e se atualiza ao reiniciar ou ao fechar (pode ser desligado em Configurações); o instalador espera o programa fechar e o reabre. Migrações guardam cópia do banco antes de alterar a estrutura, e bancos/backups de versões mais novas são recusados. Assinatura digital: o workflow assina executável, instalador e desinstalador quando os *secrets* do certificado existirem — falta apenas o certificado.

## Etapa 10 - Port para C#/.NET WPF

Objetivo: abandonar Java/JavaFX/Maven e reescrever o Assetly inteiramente em C#/.NET WPF, preservando funcionalidades, regras de negócio e identidade visual.

- Portar o domínio (entidades, VOs, enums, invariantes) mantendo os 17 testes de domínio equivalentes.
- Portar a camada de aplicação (27 casos de uso, incluindo o motor `GerarAlertasAutomaticos`) preservando comportamento e testes.
- Portar a infraestrutura SQLite (migração, repositórios com upsert manual) mantendo o schema idêntico e compatibilidade com bancos já existentes em `~/.assetly` e `~/.assetly-teste`.
- Portar o armazenamento local de documentos com a mesma normalização de nomes de arquivo.
- Recriar a interface em WPF: tema escuro traduzido do `assetly.css` para um `ResourceDictionary`, janela sem chrome nativo via `WindowChrome`, as 6 telas (Painel, Bens, Garantias, Manutenções, Documentos, Alertas), diálogos de formulário, seletor de arquivo customizado e atualização em tempo real.
- Portar o gerador de dados fictícios do ambiente de teste (PDFs, SVGs, CSV, TXT sintéticos).
- Atualizar scripts de execução e documentação para o novo ecossistema .NET.
- Remover por completo o código-fonte, build e ferramentas Java/Maven do repositório.

Resultado esperado: Assetly funcionalmente e visualmente equivalente ao MVP Java, rodando 100% em C#/.NET WPF, sem nenhum vestígio da stack anterior no repositório.

Situação: implementado. Solução `.NET 10` com cinco projetos (`Assetly.Dominio`, `Assetly.Aplicacao`, `Assetly.Infraestrutura`, `Assetly.Apresentacao`, `Assetly.Testes`), 28 testes automatizados verdes, e validação manual do aplicativo em execução (dashboard, tabelas, diálogos e semeadura do ambiente de teste). Java, Maven e JavaFX foram removidos do repositório.

## Etapa 11 - Revisão Geral para Uso Diário

Objetivo: revisar o programa inteiro e eliminar bugs, inconsistências e lacunas que impediam o uso real.

- Alertas automáticos passam a ser marcados como automáticos (migração V005) e se resolvem sozinhos quando a condição deixa de existir; bens arquivados/descartados não geram alertas; convênios vencendo/vencidos geram alertas; o tipo de bem "Pessoa (titular)" não exige nota fiscal.
- Exclusão em todos os módulos, com remoção da cópia privada dos arquivos e exclusão em cascata ao excluir um bem; bem arquivado pode ser desarquivado; alerta resolvido pode ser reaberto.
- Edição de documento não exige mais o arquivo quando só o nome/tipo/data mudam (e não reativa documento ausente); edição de manutenção encerrada permite corrigir descrição e tipo; data e preço de compra são independentes e podem ser apagados na edição.
- Backup com a API de backup online do SQLite (cópia consistente), nomes de instantâneo únicos, bloqueio de destino dentro da pasta de dados (evita cópia recursiva infinita), restauração com cópia de segurança, reaplicação de migrações e relocalização de documentos.
- Interface: o botão "Reprocessar alertas" passou a reprocessar; painel sem contagem dupla e com linhas clicáveis; tabelas preservam ordenação, seleção e rolagem a cada atualização; colunas de data e valor ordenáveis de verdade; atualização imediata de todos os módulos após qualquer alteração; atalhos de teclado funcionando logo após trocar de módulo e sem sequestrar o Delete da busca; Enter salva formulários; seletores nativos de arquivo e pasta; seletor de data no tema e em pt-BR; valores monetários no formato brasileiro; padding dos botões e barras de rolagem corrigidos; nomes acessíveis nos campos.
- Robustez: tratamento global de erros com registro local em `~/.assetly/logs/`, aviso amigável quando o banco não abre, instância única por ambiente, sem pilha de diálogos de erro no ciclo automático.
- Testes: de 69 para 97 testes automatizados, incluindo backup/restauração reais em SQLite, exclusão em cascata, migração V005 e o motor de alertas; validação manual da interface com automação de UI (UI Automation) em todos os módulos, tema claro e fluxos de cadastro, exclusão, backup e restauração.

Situação: implementado.

## Etapa 12 - Automóveis e Imóveis

Objetivo: controlar tudo sobre veículos e imóveis, não só o bem genérico.

- Domínio: `Veiculo` (placa antiga/Mercosul, RENAVAM, chassi, anos, combustível, quilometragem, revisão por km/data, licenciamento, seguro), `Abastecimento` com `CalculoConsumo` (tanque cheio a tanque cheio), `Imovel` (tipo, situação, endereço com UF/CEP validados, área, cômodos, matrícula, IPTU, condomínio, seguro, locação) e `Despesa` (contas com vencimento, pagamento e status), todos ligados 1:N ou 1:1 a um bem e excluídos em cascata com ele (migração V006).
- Aplicação: cadastro da ficha criando o bem junto ou vinculando a um bem existente do tipo certo, abastecimento que atualiza o hodômetro, despesas parceladas/mensais, pagamento e estorno; alertas automáticos de licenciamento, seguro, revisão, contas e contrato de aluguel, com resolução automática.
- Interface: módulos "Automóveis" (Veículos, Abastecimentos, Despesas) e "Imóveis" (Imóveis, Contas) em abas, com fichas completas, consumo e custo por km, totais por ano, filtros e atalhos; painel com "Contas vencidas" e prioridades levando ao módulo certo; dados de demonstração no ambiente de teste.
- Testes: 118 testes automatizados (domínio, casos de uso, alertas e persistência SQLite dos novos módulos) e renderização das telas e formulários fora da tela.

Situação: implementado.

## Etapa 13 - Patrimônio, Notas Eletrônicas, Segurança dos Dados e Distribuição

Objetivo: completar as funcionalidades planejadas para uso diário e profissionalizar a distribuição.

- Patrimônio: depreciação linear por tipo de bem (eletrônicos 20% a.a., veículos 12%, demais 10%, com valor residual; imóveis sem depreciação) e módulo `Patrimônio` com valor de compra x estimado hoje, depreciação acumulada, distribuição por tipo e por responsável, evolução anual e maiores bens.
- Modo família: responsável por bem (uma pessoa cadastrada, validada; excluir a pessoa só desfaz o vínculo), com coluna, busca e filtro `Pessoas`.
- Notas fiscais eletrônicas: chave de acesso de 44 dígitos validada (UF, mês, dígito verificador), modelo/série/número/CNPJ, link de consulta; leitura do QR Code da NFC-e (ZXing, versões 1 e 2, inclusive contingência) e do texto por OCR do Windows (`Windows.Media.Ocr`, local); `Preencher pela nota (foto)` no cadastro de bens e `Consultar nota` na Sefaz.
- Exportação: botão `Exportar CSV` em todas as tabelas (linhas visíveis, `;` e UTF-8 com BOM para o Excel) e exportação completa em JSON.
- Backup criptografado: arquivo único `.assetly` com AES-256-GCM em blocos (chave PBKDF2-SHA256, 600 mil iterações), detecção de senha errada, alteração ou truncamento; senha guardada com DPAPI do Windows para os backups automáticos; restauração de arquivo `.assetly` ou pasta.
- Sincronização opcional por uma pasta de nuvem do usuário: impressão digital canônica dos dados (ignorando alertas automáticos e caminhos locais), envio/recebimento automático, conflito com escolha do usuário, computador novo recebe direto; cópia de segurança antes de receber (migração V008).
- Identidade visual: ícone novo, tela de abertura (splash), ícones por módulo na barra lateral, logotipo na barra de título e imagens do instalador.
- Distribuição: Inno Setup no lugar do Velopack, versões por data, atualizador próprio com verificação de SHA-256 e autoteste de ponta a ponta usado no CI e antes de cada release.
- Testes: 172 testes automatizados, mais o autoteste do executável publicado e do programa instalado.

Situação: implementado.

## Funcionalidades Futuras

- Assinatura digital do instalador (o workflow já está pronto; falta o certificado de assinatura de código).
- Internacionalização (a interface é toda em português do Brasil; exige extrair os textos para arquivos de recursos).
- Controle de equipamentos de pequenas empresas.
