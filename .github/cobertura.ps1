# Resume a cobertura dos testes (arquivo Cobertura gerado pelo coverlet) por camada, escreve a tabela no resumo da
# execução do GitHub Actions e falha se uma camada cair abaixo do mínimo. A interface (projeto "Assetly") é só
# informada: boa parte dela é desenho de tela, coberto pelo autoteste.
#
# Uso: pwsh .github/cobertura.ps1 -Pasta <pasta com os resultados do dotnet test>
param(
    [Parameter(Mandatory = $true)][string]$Pasta
)

$ErrorActionPreference = 'Stop'
$minimos = @{ 'Assetly.Dominio' = 90; 'Assetly.Aplicacao' = 80; 'Assetly.Infraestrutura' = 85 }
$nomes = @{ 'Assetly.Dominio' = 'Domínio'; 'Assetly.Aplicacao' = 'Aplicação'; 'Assetly.Infraestrutura' = 'Infraestrutura'; 'Assetly' = 'Interface' }

$arquivo = Get-ChildItem -Path $Pasta -Recurse -Filter 'coverage.cobertura.xml' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -eq $arquivo) { throw "Nenhum coverage.cobertura.xml encontrado em $Pasta." }
[xml]$xml = Get-Content -Path $arquivo.FullName -Raw -Encoding UTF8

$linhas = @('| Camada | Linhas | Ramos | Mínimo |', '| --- | ---: | ---: | ---: |')
$abaixo = @()
foreach ($pacote in $xml.coverage.packages.package) {
    $linhasPct = [math]::Round([double]::Parse($pacote.'line-rate', [Globalization.CultureInfo]::InvariantCulture) * 100, 1)
    $ramosPct = [math]::Round([double]::Parse($pacote.'branch-rate', [Globalization.CultureInfo]::InvariantCulture) * 100, 1)
    $nome = if ($nomes.ContainsKey($pacote.name)) { $nomes[$pacote.name] } else { $pacote.name }
    $minimo = if ($minimos.ContainsKey($pacote.name)) { "$($minimos[$pacote.name])%" } else { '—' }
    $linhas += "| $nome | $linhasPct% | $ramosPct% | $minimo |"
    if ($minimos.ContainsKey($pacote.name) -and $linhasPct -lt $minimos[$pacote.name]) {
        $abaixo += "$nome ($linhasPct% < $($minimos[$pacote.name])%)"
    }
}

$total = [math]::Round([double]::Parse($xml.coverage.'line-rate', [Globalization.CultureInfo]::InvariantCulture) * 100, 1)
$texto = "### Cobertura dos testes`n`n" + ($linhas -join "`n") + "`n`nTotal: $total% das linhas.`n"
Write-Output $texto
if ($env:GITHUB_STEP_SUMMARY) { Add-Content -Path $env:GITHUB_STEP_SUMMARY -Value $texto -Encoding UTF8 }

if ($abaixo.Count -gt 0) { throw "Cobertura abaixo do mínimo: $($abaixo -join ', ')." }
