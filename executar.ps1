$ErrorActionPreference = "Stop"

$env:ASSETLY_AMBIENTE_PRODUCAO = "producao"
$env:ASSETLY_AMBIENTE_TESTE = "teste"

# Altere somente PRODUCAO para TESTE nesta linha para alternar o ambiente.
$env:ASSETLY_AMBIENTE = $env:ASSETLY_AMBIENTE_TESTE

Set-Location -Path $PSScriptRoot

function Encontrar-MavenNoPath {
    $mavenWrapper = Join-Path $PSScriptRoot "mvnw.cmd"
    if (Test-Path -LiteralPath $mavenWrapper) {
        return $mavenWrapper
    }

    $comando = Get-Command "mvn.cmd" -ErrorAction SilentlyContinue
    if ($null -eq $comando) {
        $comando = Get-Command "mvn" -ErrorAction SilentlyContinue
    }
    if ($null -ne $comando) {
        return $comando.Source
    }
    return $null
}

function Obter-Maven {
    $mavenNoPath = Encontrar-MavenNoPath
    if (-not [string]::IsNullOrWhiteSpace($mavenNoPath)) {
        return $mavenNoPath
    }

    $versaoMaven = "3.9.9"
    $diretorioFerramentas = Join-Path $PSScriptRoot ".tools"
    $diretorioMaven = Join-Path $diretorioFerramentas "apache-maven-$versaoMaven"
    $mavenLocal = Join-Path $diretorioMaven "bin\mvn.cmd"

    if (Test-Path -LiteralPath $mavenLocal) {
        return $mavenLocal
    }

    New-Item -ItemType Directory -Force -Path $diretorioFerramentas | Out-Null
    $arquivoZip = Join-Path $diretorioFerramentas "apache-maven-$versaoMaven-bin.zip"
    $url = "https://archive.apache.org/dist/maven/maven-3/$versaoMaven/binaries/apache-maven-$versaoMaven-bin.zip"

    if (Test-Path -LiteralPath $arquivoZip) {
        Remove-Item -LiteralPath $arquivoZip -Force
    }

    Write-Host "Maven não encontrado no PATH. Baixando Apache Maven $versaoMaven para .tools..."
    Invoke-WebRequest -Uri $url -OutFile $arquivoZip -UseBasicParsing
    Expand-Archive -Path $arquivoZip -DestinationPath $diretorioFerramentas -Force
    Remove-Item -LiteralPath $arquivoZip -Force

    if (-not (Test-Path -LiteralPath $mavenLocal)) {
        throw "Maven local não foi encontrado após o download: $mavenLocal"
    }

    return $mavenLocal
}

$maven = Obter-Maven
Write-Host "Executando Assetly em ambiente: $env:ASSETLY_AMBIENTE"
& $maven "javafx:run"
