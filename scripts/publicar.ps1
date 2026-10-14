#Requires -Version 5.1
<#
.SYNOPSIS
    Genera el paquete de instalación de Lácteos Madeline para llevar a
    otras PCs (por USB o como descarga), sin necesidad de .NET en destino.

.DESCRIPTION
    Corre las pruebas automatizadas, compila un LacteosMadeline.exe
    autocontenido (win-x64) y arma:
        dist\LacteosMadeline-<versión>-win-x64.zip
    con el programa, Instalar.cmd y las instrucciones. En la otra PC basta
    con descomprimirlo y ejecutar Instalar.cmd.

.PARAMETER SinPruebas
    No ejecuta las pruebas antes de compilar (no recomendado).
#>
param(
    [switch]$SinPruebas
)

$ErrorActionPreference = 'Stop'

$raiz     = Split-Path $PSScriptRoot -Parent
$proyecto = Join-Path $raiz 'LacteosMadeline.csproj'
$solucion = Join-Path $raiz 'LacteosMadeline.slnx'
$dist     = Join-Path $raiz 'dist'

function Detener([string]$Texto) {
    Write-Host "ERROR: $Texto" -ForegroundColor Red
    exit 1
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Detener 'No se encontró dotnet. Instale el SDK de .NET 10: https://dotnet.microsoft.com/download/dotnet/10.0'
}

$version = ([xml](Get-Content -LiteralPath $proyecto -Raw)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if (-not $version) { $version = '0.0.0' }
$nombrePaquete = "LacteosMadeline-$version-win-x64"

if (-not $SinPruebas) {
    Write-Host '==> Ejecutando pruebas automatizadas' -ForegroundColor Cyan
    & dotnet test $solucion -nologo
    if ($LASTEXITCODE -ne 0) { Detener 'Hay pruebas que fallan. Corríjalas antes de publicar.' }
}

Write-Host '==> Compilando versión de entrega' -ForegroundColor Cyan
$carpetaPaquete = Join-Path $dist $nombrePaquete
if (Test-Path -LiteralPath $carpetaPaquete) { Remove-Item -LiteralPath $carpetaPaquete -Recurse -Force }
& dotnet publish $proyecto -c Release -o (Join-Path $carpetaPaquete 'app') -nologo
if ($LASTEXITCODE -ne 0) { Detener 'La compilación falló.' }

Write-Host '==> Armando el paquete' -ForegroundColor Cyan
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'instalar.ps1') -Destination $carpetaPaquete
Copy-Item -LiteralPath (Join-Path $raiz 'Instalar.cmd') -Destination $carpetaPaquete
Copy-Item -LiteralPath (Join-Path $raiz 'Desinstalar.cmd') -Destination $carpetaPaquete
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LEEME-paquete.txt') -Destination (Join-Path $carpetaPaquete 'LEEME.txt')

$zip = Join-Path $dist "$nombrePaquete.zip"
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
Compress-Archive -Path $carpetaPaquete -DestinationPath $zip

Write-Host ''
Write-Host "Paquete listo: $zip" -ForegroundColor Green
Write-Host 'En la otra PC: descomprimir y ejecutar Instalar.cmd (no necesita .NET ni permisos de administrador).'
