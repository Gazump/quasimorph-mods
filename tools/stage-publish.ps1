# Builds a mod and stages the files Steam needs into publish\<UniqueModName>\.
#
# Uploading from LocalUserPresets does not work well: that folder has to be empty while
# testing the subscribed build, or two mods share a UniqueModName and one gets disabled
# as a duplicate.
#
#   .\tools\stage-publish.ps1 -Mod LootTracker -ItemId 3783896795
param(
    [Parameter(Mandatory = $true)][string]$Mod,
    [string]$ItemId
)

$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$project = Join-Path $root "src\$Mod\$Mod.csproj"
if (-not (Test-Path $project)) { throw "No project at $project" }

dotnet build $project /p:QmDeploy=false /v:minimal
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

$manifestPath = Join-Path $root "src\$Mod\modmanifest.json"
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$uniqueName = $manifest.UniqueModName
if (-not $uniqueName) { throw "modmanifest.json has no UniqueModName." }

$dest = Join-Path $root "publish\$uniqueName"
if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
New-Item -ItemType Directory -Force -Path $dest | Out-Null

foreach ($assembly in $manifest.Assemblies) {
    $built = Join-Path $root "src\$Mod\bin\Debug\$assembly"
    if (-not (Test-Path $built)) { throw "Manifest lists $assembly but it was not built." }
    Copy-Item $built $dest
}
Copy-Item $manifestPath $dest

$thumb = Join-Path $root "src\$Mod\thumbnail.png"
if (Test-Path $thumb) {
    if ((Get-Item $thumb).Length -gt 1MB) { Write-Warning "thumbnail.png is over Steam's 1 MB preview limit." }
    Copy-Item $thumb $dest
} else {
    Write-Warning "No thumbnail.png - Steam will keep the item's current preview."
}

$extra = Join-Path $root "src\$Mod\content"
if (Test-Path $extra) { Copy-Item "$extra\*" $dest -Recurse }

Write-Host ""
Write-Host "Staged $uniqueName to $dest" -ForegroundColor Green
Get-ChildItem $dest -Recurse -File | ForEach-Object { "  $($_.Name)  $($_.Length) bytes" }
Write-Host ""
Write-Host "Run this in the in-game developer console:"
if ($ItemId) {
    Write-Host "  mod_updateworkshopitem $ItemId `"$dest`" true"
} else {
    Write-Host "  mod_createworkshopitem `"$dest`""
    Write-Host "  then: mod_updateworkshopitem <item_id> `"$dest`" true"
}
