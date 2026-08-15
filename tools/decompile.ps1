# Decompiles Assembly-CSharp.dll into reference\ so the game's classes can be searched.
# Re-run after a Quasimorph update. Needs: dotnet tool install -g ilspycmd --version 9.0.0.7889
param(
    [string]$QuasimorphDir
)

$ErrorActionPreference = 'Stop'

if (-not $QuasimorphDir) {
    $candidates = @(
        'C:\Program Files (x86)\Steam\steamapps\common\Quasimorph'
        'C:\Program Files\Steam\steamapps\common\Quasimorph'
        'D:\SteamLibrary\steamapps\common\Quasimorph'
        'D:\Steam\steamapps\common\Quasimorph'
        'E:\SteamLibrary\steamapps\common\Quasimorph'
        'E:\Steam\steamapps\common\Quasimorph'
    )
    $QuasimorphDir = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $QuasimorphDir) { throw "Could not find Quasimorph. Pass -QuasimorphDir." }
}

$managed = Join-Path $QuasimorphDir 'Quasimorph_Data\Managed'
$dll = Join-Path $managed 'Assembly-CSharp.dll'
if (-not (Test-Path $dll)) { throw "Assembly-CSharp.dll not found at $dll" }

if (-not (Get-Command ilspycmd -ErrorAction SilentlyContinue)) {
    throw "ilspycmd not found. Run: dotnet tool install -g ilspycmd --version 9.0.0.7889"
}

$out = Join-Path $PSScriptRoot '..\reference\Assembly-CSharp'
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Force -Path $out | Out-Null

ilspycmd -p -o $out -r $managed $dll

# The generated project cannot build against the real game and only confuses the C# extension.
Remove-Item (Join-Path $out 'Assembly-CSharp.csproj') -Force -ErrorAction SilentlyContinue

$count = (Get-ChildItem $out -Recurse -Filter *.cs | Measure-Object).Count
Write-Host "Decompiled $count files to $out"
