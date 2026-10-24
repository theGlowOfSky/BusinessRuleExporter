<#
  Builds, tests and packs the plugin. Run from the repository root in PowerShell on Windows:
      powershell -ExecutionPolicy Bypass -File packaging\pack.ps1
  Requires: .NET SDK (VS 2022), .NET Framework 4.8 targeting pack, nuget.exe on PATH.
  Does NOT publish anything.
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$nuspec = Join-Path $root 'packaging\BusinessRuleExporter.nuspec'
$nuspecText = Get-Content $nuspec -Raw
if ($nuspecText -match 'YOUR_GITHUB_USERNAME') { throw 'Replace YOUR_GITHUB_USERNAME in packaging\BusinessRuleExporter.nuspec before packing.' }

dotnet restore BusinessRuleExporter.sln
dotnet build BusinessRuleExporter.sln -c Release --no-restore
dotnet test BusinessRuleExporter.Tests\BusinessRuleExporter.Tests.csproj -c Release --no-build

$out = Join-Path $root 'BusinessRuleExporter\bin\Release'
$dll = Join-Path $out 'BusinessRuleExporter.dll'
if (-not (Test-Path $dll)) { throw "Missing $dll" }

$extra = Get-ChildItem $out -File | Where-Object { $_.Extension -eq '.dll' -and $_.Name -ne 'BusinessRuleExporter.dll' }
if ($extra) { Write-Warning ("Unexpected DLLs in output (host assemblies must not be shipped): " + ($extra.Name -join ', ')) }

$asmVersion = [System.Reflection.AssemblyName]::GetAssemblyName($dll).Version
$specVersion = [version](([xml]$nuspecText).package.metadata.version)
$asmVersionNormalized = $asmVersion.ToString()
$specVersionNormalized = $specVersion.ToString()
if ($asmVersionNormalized -ne $specVersionNormalized) {
    throw "Assembly version $asmVersionNormalized does not match nuspec version $specVersionNormalized"
}

New-Item -ItemType Directory -Force artifacts | Out-Null
nuget pack $nuspec -BasePath $root -OutputDirectory artifacts
Write-Host "Package written to artifacts\. Inspect it with NuGet Package Explorer before publishing."
