<#
.PARAMETER VsixPath
Path to the VSIX file to be expanded.
#>

#requires -Version 3.0
Param(
    [Parameter(Mandatory=$True)][String]$VsixPath
)
Set-StrictMode -Version Latest
$WarningPreference = "Stop"
$ErrorActionPreference = "Stop"

$VsixName = [IO.Path]::GetFileNameWithoutExtension($VsixPath)
$OutPath = "out\vsix\$VsixName"
$VsixZipPath = "out\vsix\$VsixName.zip"

& "$PSScriptRoot\New-CleanDirectory" $OutPath | Out-Null
Copy-Item $VsixPath $VsixZipPath
Expand-Archive -Path $VsixZipPath -DestinationPath $OutPath
