$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$compilerPublish = Join-Path $root "MiniGo.Compiler\bin\Release\net8.0\win-x64\publish"
$idePublish      = Join-Path $root "IDE\bin\Release\net8.0-windows\win-x64\publish"
$zip             = Join-Path $root "MiniGoCompiler.zip"

# Clean previous
if (Test-Path $zip) { Remove-Item -Force $zip }

Write-Host "Publishing MiniGo.Compiler (single-file) ..."
dotnet publish "$root\MiniGo.Compiler" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

Write-Host "Publishing IDE ..."
dotnet publish "$root\IDE" -c Release -r win-x64 --self-contained true

Write-Host "Adding compiler exe to IDE publish ..."
Copy-Item -Force "$compilerPublish\MiniGo.Compiler.exe" "$idePublish\MiniGo.Compiler.exe"

Write-Host "Removing PDBs ..."
Get-ChildItem "$idePublish" -Filter "*.pdb" -Recurse | Remove-Item -Force

Write-Host "Packaging ..."
Compress-Archive -Path "$idePublish\*" -DestinationPath $zip -Force

Write-Host "Done: $zip"
Write-Host "Size: $([math]::Round((Get-Item $zip).Length / 1MB, 1)) MB"
