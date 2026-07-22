# PowerShell Compile Script for Native WPF EternSynth

$scriptDir = $PSScriptRoot
if (-not $scriptDir) { $scriptDir = (Get-Location).Path }

$compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$outputExe = Join-Path $scriptDir "EternSynth.exe"

$sourceFiles = @(
    (Join-Path $scriptDir "SfxrSynth.cs"),
    (Join-Path $scriptDir "WpfVectorIcons.cs"),
    (Join-Path $scriptDir "WpfMainWindow.cs")
)

# WPF Assembly Paths
$wpfDir = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF"
$references = @(
    "System.dll",
    "System.Core.dll",
    "System.Xaml.dll",
    "$wpfDir\PresentationFramework.dll",
    "$wpfDir\PresentationCore.dll",
    "$wpfDir\WindowsBase.dll"
)

Write-Host "Compilando aplicación nativa WPF C#..." -ForegroundColor Cyan

# Compile arguments
# /target:winexe ensures it runs as a pure Windows Application with no console popup.
$args = @(
    "/target:winexe",
    "/out:$outputExe",
    "/optimize"
)

foreach ($ref in $references) {
    $args += "/reference:$ref"
}

$args += $sourceFiles

& $compiler $args

if ($LASTEXITCODE -eq 0) {
    Write-Host "¡Compilación nativa exitosa! Archivo creado en: $outputExe" -ForegroundColor Green

    # Create Desktop Shortcut
    Write-Host "Creando acceso directo en el Escritorio..." -ForegroundColor Cyan
    try {
        $WshShell = New-Object -ComObject WScript.Shell
        $desktopPath = [System.IO.Path]::Combine([System.Environment]::GetFolderPath("Desktop"), "EternSynth.lnk")
        $Shortcut = $WshShell.CreateShortcut($desktopPath)
        $Shortcut.TargetPath = $outputExe
        $Shortcut.WorkingDirectory = $scriptDir
        $Shortcut.Description = "Sintetizador y Generador de Sonidos Retro 8-bit"
        $Shortcut.Save()
        Write-Host "¡Acceso directo creado con éxito en: $desktopPath!" -ForegroundColor Green
    }
    catch {
        Write-Warning "No se pudo crear el acceso directo en el Escritorio. Detalles: $_"
    }
} else {
    Write-Error "Error de compilación nativa. Por favor revisa los mensajes de error arriba."
}
