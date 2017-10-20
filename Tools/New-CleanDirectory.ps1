<#
.PARAMETER Path
Path to the directory to be created clean. All contents will be lost.
#>

#requires -Version 3.0
Param(
    [Parameter(Mandatory=$True)][String]$Path
)
Set-StrictMode -Version Latest
$WarningPreference = "Stop"
$ErrorActionPreference = "Stop"

if (Test-Path $Path) { Remove-Item -Recurse -Path $Path }
New-Item -ItemType Directory -Path $Path
