# Como contribuir com o Assetly

Obrigado pelo interesse! Este guia resume como relatar problemas, sugerir ideias e propor alterações.

## Relatar um problema

Abra uma [issue](../../issues/new/choose) usando o modelo **Problema**. Ele pede:

- a versão (em `Configurações → Versão e atualizações`);
- o ambiente (produção ou teste) e a versão do Windows;
- o que você fez, o que esperava e o que aconteceu;
- o **registro de erros do dia**, que fica em `%USERPROFILE%\.assetly\logs\` (no ambiente de teste, em `%USERPROFILE%\.assetly-teste\logs\`). O caminho também aparece em `Configurações → Dados locais`.

Antes de anexar o log, abra o arquivo e confira: ele traz mensagens de erro e caminhos de pastas, não o conteúdo dos seus cadastros, mas o nome do usuário do Windows aparece nos caminhos. **Nunca anexe o banco de dados, backups ou documentos reais.** Se o problema só acontece com os seus dados, tente reproduzir no ambiente de teste (`Assetly (ambiente de teste)` no menu Iniciar), que usa dados fictícios.

Falhas de segurança (por exemplo, algo que exponha dados ou permita instalar uma atualização falsa) não devem ir para uma issue pública: use **Security → Report a vulnerability** no GitHub.

## Sugerir uma funcionalidade

Use o modelo **Sugestão**. Conte o problema que você quer resolver antes da solução: fica mais fácil achar a forma mais simples de atender.

## Propor uma alteração

1. Crie um branch a partir de `desenvolvimento` (`main` recebe só o que vai para release).
2. Faça a alteração com testes. Antes de abrir o pull request, rode:

   ```powershell
   dotnet build Assetly.slnx -c Release
   dotnet test
   src\Assetly.Apresentacao\bin\Release\net10.0-windows10.0.19041.0\Assetly.exe --autoteste --telas=telas
   ```

   O build não aceita nenhum aviso (`Directory.Build.props`), e o CI confere a cobertura mínima de Domínio, Aplicação e Infraestrutura. O autoteste abre o programa sem janela, desenha todas as telas na pasta `telas` e testa o backup; confira as imagens das telas que você mexeu.
3. Abra o pull request para `desenvolvimento` e preencha o modelo.

### Convenções

- **Português** em nomes de classes, métodos, variáveis, mensagens e commits (o domínio é brasileiro: CPF, IPVA, IRPF…).
- **Camadas:** regra de negócio no Domínio; casos de uso na Aplicação (é onde ficam as decisões que precisam de teste); SQLite e arquivos na Infraestrutura; a Apresentação (WPF) só monta telas e chama casos de uso.
- **Banco de dados:** nunca altere uma migração que já foi publicada. Crie uma nova (`VNNN__descricao.sql` em `src/Assetly.Infraestrutura/Banco/Migracoes/`) e registre em `MigradorBancoDados`. Colunas novas precisam de valor padrão que mantenha o comportamento de quem já usa o programa.
- **Exportação e sincronização:** campos novos entram no JSON de `ExportarDados` só quando preenchidos — assim a "impressão digital" da sincronização não muda para quem não usa o campo e ninguém vê um conflito falso depois de atualizar.
- **Commits:** mensagem curta no imperativo, com prefixo — `feat:`, `fix:`, `perf:`, `docs:`, `build:`, `ci:`, `deps:`.
- **Comentários:** explique o *porquê* que o código não mostra; o *o quê* fica no nome.

### Testes

- `dotnet test` roda domínio, casos de uso, infraestrutura real (SQLite e arquivos em pastas temporárias) e a camada de interface (WPF numa thread própria, sem janela). Os testes rodam em série: vários deles limpam o pool global de conexões do SQLite.
- `Assetly.exe --autoteste --volume[=MB]` monta um banco grande (500 bens, 5 mil contas, 2 mil documentos) e mede o ciclo da tela, a busca, a exportação, o backup e o arquivo da sincronização. Use quando mexer em algo que cresce com os dados; o CI roda com 256 MB.

## Licença

Ao contribuir, você concorda que a sua contribuição seja distribuída sob a [licença MIT](LICENSE) do projeto.
