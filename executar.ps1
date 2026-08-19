$ErrorActionPreference = "Stop"

$env:ASSETLY_AMBIENTE_PRODUCAO = "producao"
$env:ASSETLY_AMBIENTE_TESTE = "teste"

# Altere somente PRODUCAO para TESTE nesta linha para alternar o ambiente.
$env:ASSETLY_AMBIENTE = $env:ASSETLY_AMBIENTE_TESTE

Set-Location -Path $PSScriptRoot

$comandoDotnet = Get-Command "dotnet" -ErrorAction SilentlyContinue
if ($null -eq $comandoDotnet) {
    throw "dotnet não foi encontrado no PATH. Instale o .NET SDK 10 ou superior antes de executar o Assetly."
}

Write-Host "Executando Assetly em ambiente: $env:ASSETLY_AMBIENTE"
& dotnet run --project (Join-Path $PSScriptRoot "src\Assetly.Apresentacao\Assetly.Apresentacao.csproj")
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
