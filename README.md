# Assetly

Assetly é um gerenciador pessoal de código aberto de bens, garantias, documentos e manutenções. O objetivo é ajudar pessoas comuns a manterem itens importantes organizados, com controle de prazos, histórico e documentos locais.

## Situação

Projeto em desenvolvimento inicial de produto mínimo viável. A base atual inclui domínio independente de arcabouços, casos de uso, persistência SQLite local e uma interface JavaFX básica para operar os fluxos principais.

## Objetivos

- Registrar bens pessoais como eletrônicos, eletrodomésticos, veículos, ferramentas e equipamentos de trabalho.
- Acompanhar garantias, datas de vencimento e informações de suporte.
- Guardar metadados de documentos como notas fiscais, recibos, manuais e fotos.
- Registrar histórico de manutenções e próximas datas previstas.
- Gerar alertas locais para garantias vencendo, manutenções atrasadas e documentos ausentes.

## Tecnologias

- Java 21
- JavaFX
- Maven
- SQLite para persistência local
- JUnit 5
- AssertJ

## Arquitetura

Assetly segue uma arquitetura leve inspirada em DDD e Arquitetura Limpa.

- `dominio`: conceitos e regras de negócio. Não deve depender de JavaFX, SQLite ou detalhes de infraestrutura.
- `aplicacao`: casos de uso, contratos de repositório e dados de entrada/saída.
- `infraestrutura`: acesso a banco, repositórios e armazenamento local de arquivos.
- `apresentacao`: contexto da aplicação, visões e modelos de visão JavaFX.

O SQLite é usado como banco local embutido. Dados de execução ficam fora do repositório, dentro da pasta do usuário:

- banco: `~/.assetly/banco/assetly.sqlite3`
- documentos locais: `~/.assetly/documentos/`

O banco deve guardar apenas metadados e caminhos locais dos documentos.
Ao iniciar a aplicação, o banco local é criado e migrado automaticamente.

## Estrutura Atual

```text
Assetly/
├── README.md
├── LICENSE
├── ROADMAP.md
├── pom.xml
└── src/
    ├── main/
    │   ├── java/
    │   │   ├── module-info.java
    │   │   └── com/assetly/
    │   │       ├── AssetlyAplicacao.java
    │   │       ├── dominio/
    │   │       ├── aplicacao/
    │   │       ├── infraestrutura/
    │   │       └── apresentacao/
    │   └── resources/
    │       ├── estilos/
    │       ├── interfaces/
    │       └── banco/
    └── test/
        └── java/com/assetly/
```

Pastas padrão do Java/Maven e arquivos convencionais do repositório mantêm seus nomes em inglês, como `src/main/java`, `src/main/resources`, `src/test/java`, `README.md`, `LICENSE`, `ROADMAP.md`, `pom.xml`, `.gitignore` e `module-info.java`.

## Execução

Requisitos:

- JDK 21 ou superior
- Maven 3.9 ou superior

Executar a aplicação:

```bash
mvn clean javafx:run
```

Executar testes:

```bash
mvn test
```

## Privacidade

Assetly foi pensado como uma aplicação com prioridade local:

- não exige conta;
- não depende de nuvem;
- não possui telemetria no produto mínimo viável;
- os dados ficam no computador do usuário;
- bancos e documentos de execução ficam fora do repositório por padrão.

Não faça commit de bancos reais, notas fiscais, recibos, manuais, fotos, cópias de segurança, exportações ou documentos pessoais.

## Funcionalidades Planejadas

- Cadastro, edição, listagem e mudança de status de bens.
- Cadastro de garantias e indicadores de vencimento.
- Agendamento e histórico de manutenções.
- Painel de alertas.
- Cadastro de metadados de documentos e armazenamento local.
- Exportação CSV e JSON.
- OCR para notas fiscais.
- Leitura de QR Code de notas fiscais.
- Cópias de segurança criptografadas.
- Sincronização opcional.
- Modo família.
- Controle de veículos e imóveis.
- Depreciação estimada.
- Tema claro e escuro.
- Internacionalização.

## Contribuição

Contribuições serão bem-vindas quando a fundação do produto mínimo viável estiver estável. Mantenha mudanças focadas, legíveis e cobertas por testes quando afetarem regras de domínio.

Antes de abrir uma contribuição, confirme que bancos locais, documentos e arquivos de ambiente não foram incluídos.

## Licença

Licença MIT. Consulte [LICENSE](LICENSE).
