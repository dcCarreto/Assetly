# Roteiro do Assetly

Este roteiro organiza o desenvolvimento do Assetly em etapas pequenas, com foco em um produto mínimo viável de área de trabalho com prioridade local em Java 21, JavaFX, Maven e SQLite.

## Princípios do Projeto

- Manter o domínio isolado de JavaFX, JDBC, SQLite e detalhes de infraestrutura.
- Priorizar um produto mínimo viável simples, usável e fácil de rodar.
- Evitar dependências desnecessárias no início.
- Não versionar bancos locais, documentos reais, anexos, notas fiscais, fotos ou arquivos sensíveis.
- Não fazer commits, pushes ou tags automaticamente.
- Manter nomes de pacotes, classes e documentação de negócio em português.
- Manter convenções do ecossistema Java/Maven e arquivos padrão do repositório em inglês, como `src/main/java`, `src/main/resources`, `src/test/java`, `README.md`, `LICENSE` e `ROADMAP.md`.

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

## Etapa 2 - Domínio Principal

Objetivo: implementar as regras centrais sem dependência de arcabouço.

- Criar entidades e objetos de valor para `Bem`, `Garantia`, `Manutencao`, `Documento` e `Alerta`.
- Criar enums de status e tipos.
- Implementar validações básicas.
- Adicionar testes unitários para regras importantes.

Resultado esperado: domínio testável, claro e independente da interface e do banco.

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

Situação: implementado com painel, abas JavaFX, listagens, formulários e ações básicas para bens, garantias, manutenções, documentos e alertas.

## Etapa 6 - Documentos Locais

Objetivo: registrar documentos relacionados aos bens sem expor arquivos pessoais.

- Registrar metadados de documentos.
- Copiar arquivos para armazenamento local ignorado pelo Git, como `~/.assetly/documentos/`.
- Salvar no banco apenas metadados e caminho local.

Resultado esperado: documentos associados aos bens sem risco de commit acidental.

## Etapa 7 - Alertas Automáticos

Objetivo: gerar avisos úteis para o usuário.

- Alertar garantias vencendo em até 30 dias.
- Alertar garantias vencidas.
- Alertar manutenções próximas ou atrasadas.
- Alertar ausência de nota fiscal quando aplicável.
- Testar a lógica de geração de alertas.

Resultado esperado: painel de alertas com informações acionáveis.

## Etapa 8 - Polimento do produto mínimo viável

Objetivo: preparar o projeto para uso e apresentação código aberto.

- Melhorar mensagens de erro e validações.
- Revisar layout e navegação.
- Completar `README.md` com instruções de execução e contribuição.
- Revisar testes.
- Conferir `git status` antes de qualquer sugestão de commit.

Resultado esperado: produto mínimo viável organizado, documentado e pronto para evoluir publicamente.

## Funcionalidades Futuras

- OCR para nota fiscal.
- Leitura de QR Code.
- Exportação CSV e JSON.
- Cópia de segurança criptografada.
- Sincronização opcional.
- Modo família.
- Controle de veículos, imóveis e equipamentos de pequenas empresas.
- Painel de valor patrimonial.
- Depreciação estimada.
- Tema claro e escuro.
- Internacionalização.
