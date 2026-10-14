#Requires -Version 5.1
<#
.SYNOPSIS
    Instala (o actualiza) Lácteos Madeline en esta PC para el usuario actual.

.DESCRIPTION
    - Desde el repositorio: compila el programa (necesita el SDK de .NET 10;
      si falta, ofrece instalarlo con winget).
    - Desde el paquete .zip (carpeta "app" junto a este script): no compila,
      solo instala. No necesita .NET.

    Instala en  %LOCALAPPDATA%\Programs\LacteosMadeline  (sin permisos de
    administrador) y crea accesos directos en el Escritorio y el menú Inicio.
    Los datos del negocio (%LOCALAPPDATA%\LacteosMadeline) nunca se tocan:
    actualizar o desinstalar conserva la base de datos y los respaldos.

.PARAMETER Desinstalar
    Quita el programa y sus accesos directos. Conserva los datos.

.PARAMETER NoIniciar
    No abre el programa al terminar la instalación.

.PARAMETER SinAccesos
    No crea accesos directos en el Escritorio ni en el menú Inicio.
#>
param(
    [switch]$Desinstalar,
    [switch]$NoIniciar,
    [switch]$SinAccesos
)

$ErrorActionPreference = 'Stop'

$NombreVisible  = 'Lácteos Madeline'
$Ejecutable     = 'LacteosMadeline.exe'
$CarpetaDestino = Join-Path $env:LOCALAPPDATA 'Programs\LacteosMadeline'
$CarpetaDatos   = Join-Path $env:LOCALAPPDATA 'LacteosMadeline'
$AccesoEscritorio = Join-Path ([Environment]::GetFolderPath('Desktop')) "$NombreVisible.lnk"
$AccesoMenuInicio = Join-Path ([Environment]::GetFolderPath('Programs')) "$NombreVisible.lnk"

function Escribir-Paso([string]$Texto) {
    Write-Host ''
    Write-Host "==> $Texto" -ForegroundColor Cyan
}

function Detener([string]$Texto) {
    Write-Host ''
    Write-Host "ERROR: $Texto" -ForegroundColor Red
    exit 1
}

function Esperar-CierreDelPrograma {
    while ($true) {
        $abiertos = @(Get-Process -Name 'LacteosMadeline' -ErrorAction SilentlyContinue)
        if ($abiertos.Count -eq 0) { return }
        Write-Host "$NombreVisible está abierto. Ciérrelo (guarde la venta en curso) y presione Enter para continuar..." -ForegroundColor Yellow
        [void](Read-Host)
    }
}

# El antivirus puede retener el .exe unos segundos después de copiarlo o de
# cerrar el programa: se reintenta antes de dar el error.
function Eliminar-Carpeta([string]$Ruta) {
    for ($intento = 1; Test-Path -LiteralPath $Ruta; $intento++) {
        try {
            Remove-Item -LiteralPath $Ruta -Recurse -Force
        }
        catch {
            if ($intento -ge 10) { Detener "No se pudo reemplazar $Ruta. Cierre el programa, espere unos segundos y vuelva a intentarlo." }
            Start-Sleep -Seconds 1
        }
    }
}

function Quitar-Accesos {
    foreach ($acceso in @($AccesoEscritorio, $AccesoMenuInicio)) {
        if (Test-Path -LiteralPath $acceso) { Remove-Item -LiteralPath $acceso -Force }
    }
}

function Crear-Acceso([string]$Ruta, [string]$Destino) {
    $shell = New-Object -ComObject WScript.Shell
    $acceso = $shell.CreateShortcut($Ruta)
    $acceso.TargetPath = $Destino
    $acceso.WorkingDirectory = Split-Path $Destino -Parent
    $acceso.IconLocation = "$Destino,0"
    $acceso.Description = "$NombreVisible - Sistema de ventas e inventario"
    $acceso.Save()
}

function Test-Sdk10 {
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) { return $false }
    $sdks = & dotnet --list-sdks 2>$null
    return [bool]($sdks | Where-Object { $_ -match '^10\.' })
}

function Actualizar-Path {
    $maquina = [Environment]::GetEnvironmentVariable('Path', 'Machine')
    $usuario = [Environment]::GetEnvironmentVariable('Path', 'User')
    $env:Path = "$maquina;$usuario"
}

function Asegurar-Sdk {
    if (Test-Sdk10) { return }

    Write-Host 'Para compilar el programa se necesita el SDK de .NET 10 y no está instalado.' -ForegroundColor Yellow
    if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
        Detener ('Instálelo desde https://dotnet.microsoft.com/download/dotnet/10.0 (SDK, Windows x64) ' +
                 'y vuelva a ejecutar Instalar.cmd. O use el paquete .zip ya compilado, que no lo necesita.')
    }

    $respuesta = Read-Host '¿Instalarlo ahora con winget? (S/N)'
    if ($respuesta -notmatch '^[sS]') {
        Detener 'Instalación cancelada: falta el SDK de .NET 10.'
    }

    & winget install --id Microsoft.DotNet.SDK.10 --exact --accept-source-agreements --accept-package-agreements
    Actualizar-Path
    if (-not (Test-Sdk10)) {
        Detener 'No se pudo verificar el SDK de .NET 10. Cierre esta ventana, abra una nueva y ejecute Instalar.cmd otra vez.'
    }
}

# ── Desinstalar ─────────────────────────────────────────────────────────
if ($Desinstalar) {
    Escribir-Paso "Desinstalando $NombreVisible"
    Esperar-CierreDelPrograma
    Quitar-Accesos
    Eliminar-Carpeta $CarpetaDestino
    Write-Host "Programa desinstalado. Los datos del negocio se conservan en: $CarpetaDatos" -ForegroundColor Green
    exit 0
}

# ── Origen: paquete ya compilado o repositorio ──────────────────────────
$paquete  = Join-Path $PSScriptRoot 'app'
$proyecto = Join-Path (Split-Path $PSScriptRoot -Parent) 'LacteosMadeline.csproj'

if (Test-Path -LiteralPath (Join-Path $paquete $Ejecutable)) {
    $origen = $paquete
    Escribir-Paso 'Usando el programa ya compilado del paquete'
}
elseif (Test-Path -LiteralPath $proyecto) {
    Escribir-Paso 'Verificando el SDK de .NET 10'
    Asegurar-Sdk

    $origen = Join-Path (Split-Path $proyecto -Parent) 'dist\app'
    Escribir-Paso 'Compilando el programa (la primera vez puede tardar unos minutos)'
    Eliminar-Carpeta $origen
    & dotnet publish $proyecto -c Release -o $origen -nologo
    if ($LASTEXITCODE -ne 0) { Detener 'La compilación falló. Revise los mensajes de arriba.' }
}
else {
    Detener "No se encontró ni el programa compilado ($paquete) ni el proyecto ($proyecto)."
}

# ── Copiar el programa ──────────────────────────────────────────────────
Escribir-Paso "Instalando en $CarpetaDestino"
Esperar-CierreDelPrograma
Eliminar-Carpeta $CarpetaDestino
New-Item -ItemType Directory -Path $CarpetaDestino -Force | Out-Null
Copy-Item -Path (Join-Path $origen '*') -Destination $CarpetaDestino -Recurse -Force

# Los archivos bajados de Internet traen una marca que hace que Windows
# bloquee o advierta al abrirlos; el programa es de confianza, se quita.
Get-ChildItem -LiteralPath $CarpetaDestino -Recurse -File | Unblock-File

$exeInstalado = Join-Path $CarpetaDestino $Ejecutable
if (-not (Test-Path -LiteralPath $exeInstalado)) { Detener "Falta $Ejecutable en la carpeta de instalación." }

New-Item -ItemType Directory -Path $CarpetaDatos -Force | Out-Null

# ── Accesos directos ────────────────────────────────────────────────────
if (-not $SinAccesos) {
    Escribir-Paso 'Creando accesos directos en el Escritorio y el menú Inicio'
    Quitar-Accesos
    Crear-Acceso $AccesoEscritorio $exeInstalado
    Crear-Acceso $AccesoMenuInicio $exeInstalado
}

Write-Host ''
Write-Host "$NombreVisible quedó instalado." -ForegroundColor Green
Write-Host "  Programa:        $CarpetaDestino"
Write-Host "  Datos y respaldos: $CarpetaDatos"
Write-Host '  Para actualizar, ejecute Instalar.cmd de nuevo: los datos se conservan.'

if (-not $NoIniciar) {
    Start-Process -FilePath $exeInstalado -WorkingDirectory $CarpetaDestino
}
