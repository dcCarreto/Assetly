## O que muda

<!-- O problema resolvido e como, em poucas linhas. -->

## Como foi verificado

- [ ] `dotnet build Assetly.slnx -c Release` sem avisos
- [ ] `dotnet test`
- [ ] `Assetly.exe --autoteste --telas=telas` e conferi as telas alteradas
- [ ] Mexe em algo que cresce com os dados? Rodei `--autoteste --volume`

## Cuidados

- [ ] Mudança no banco: migração nova registrada em `MigradorBancoDados`, sem alterar migração publicada
- [ ] Campo novo na exportação só entra no JSON quando preenchido
- [ ] README atualizado, se o uso mudou
