#requires -Version 3.0
Set-StrictMode -Version Latest
$WarningPreference = "Stop"
$ErrorActionPreference = "Stop"

$OutDir = "NuGetPackagesFlattened"

& "$PSScriptRoot\New-CleanDirectory" $OutDir | Out-Null
Get-ChildItem -Path NuGetPackages -Include *.dll -Recurse |
    ForEach-Object { Copy-Item $_.FullName $OutDir -Verbose }
