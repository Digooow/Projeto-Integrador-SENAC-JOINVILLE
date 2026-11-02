param(
    [string]$Version = "1.2.0",
    [string]$Runtime = "win-x64",
    [switch]$UseVelopack
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$PublishDir = Join-Path $RepoRoot "artifacts\publish\CantinaAlamo-$Runtime"
$ReleasesDir = Join-Path $RepoRoot "artifacts\releases"

Write-Host "==> [1/3] Publicando Cantina.Wpf v$Version ($Runtime)..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null
New-Item -ItemType Directory -Force -Path $ReleasesDir | Out-Null

dotnet publish "$RepoRoot\src\Cantina.Wpf\Cantina.Wpf.csproj" `
    -c Release `
    -r $Runtime `
    --self-contained true `
    -p:Version=$Version `
    -p:InformationalVersion=$Version `
    -p:PublishSingleFile=false `
    -m:1 `
    --disable-build-servers `
    -p:UseSharedCompilation=false `
    -o $PublishDir

if ($UseVelopack -or (Get-Command "vpk" -ErrorAction SilentlyContinue)) {
    Write-Host "==> [2/3] Gerando instalador OTA com Velopack (CantinaAlamo-win-Setup.exe)..." -ForegroundColor Cyan
    vpk pack `
        --packId "CantinaAlamo" `
        --packVersion $Version `
        --packDir $PublishDir `
        --mainExe "CantinaAlamo.exe" `
        --packTitle "DesperdicioZero - Cantina ALAMO (SENAC)" `
        --packAuthors "SENAC Joinville" `
        --outputDir $ReleasesDir
} else {
    Write-Host "==> [2/3] Gerando executável instalador autossuficiente (CantinaAlamo-win-Setup.exe)..." -ForegroundColor Cyan
    $SingleFileDir = Join-Path $RepoRoot "artifacts\publish\single-$Runtime"
    dotnet publish "$RepoRoot\src\Cantina.Wpf\Cantina.Wpf.csproj" `
        -c Release `
        -r $Runtime `
        --self-contained true `
        -p:Version=$Version `
        -p:InformationalVersion=$Version `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -m:1 `
        --disable-build-servers `
        -p:UseSharedCompilation=false `
        -o $SingleFileDir

    Copy-Item (Join-Path $SingleFileDir "CantinaAlamo.exe") (Join-Path $ReleasesDir "CantinaAlamo-win-Setup.exe") -Force
}

$ZipPath = Join-Path $ReleasesDir "CantinaAlamo-v$Version-$Runtime-Portable.zip"
if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
Compress-Archive -Path "$PublishDir\*" -DestinationPath $ZipPath -Force

Write-Host "==> [3/3] Pacotes gerados com sucesso em: $ReleasesDir" -ForegroundColor Green
Get-ChildItem $ReleasesDir | Select-Object Name, Length

