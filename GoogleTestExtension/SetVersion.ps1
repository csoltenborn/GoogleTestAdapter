Param([parameter(Mandatory=$true)] [string] $version)

$gta_assembly_info = "GoogleTestAdapter\Properties\AssemblyInfo.cs"
$dia_assembly_info = "DiaAdapter\Properties\AssemblyInfo.cs"
$test_assembly_info = "GoogleTestAdapterTests\Properties\AssemblyInfo.cs"
$vsix_assembly_info = "GoogleTestAdapterVSIX\Properties\AssemblyInfo.cs"
$vsix_manifest = "GoogleTestAdapterVSIX\source.extension.vsixmanifest"

(Get-Content $gta_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $gta_assembly_info
(Get-Content $dia_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $dia_assembly_info
(Get-Content $test_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $test_assembly_info
(Get-Content $vsix_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $vsix_assembly_info
(Get-Content $vsix_manifest) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $vsix_manifest
