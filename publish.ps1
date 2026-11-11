param(
    [string]$Version = '1.0.1',
    [switch]$Zip
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$out = 'publish\VietKey'
Remove-Item -Path $out -Recurse -Force -ErrorAction SilentlyContinue

foreach ($name in 'Engine', 'UI') {
    dotnet publish "src\VietKey.$name\VietKey.$name.csproj" -c Release -r win-x64 --no-self-contained `
        -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -p:DeleteExistingFiles=false -o $out
    if ($LASTEXITCODE -ne 0) { throw "Publish of VietKey.$name failed." }
}

if ($Zip) {
    Compress-Archive -Path $out -DestinationPath "publish\VietKey_$Version.zip" -Force
}

Get-ChildItem $out
