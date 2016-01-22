Param([parameter(Mandatory=$true)] [string] $version)

$core_assembly_info = "Core\Properties\AssemblyInfo.cs"
$coretests_assembly_info = "Core.Tests\Properties\AssemblyInfo.cs"

$dia_assembly_info = "DiaResolver\Properties\AssemblyInfo.cs"
$diatests_assembly_info = "DiaResolver.Tests\Properties\AssemblyInfo.cs"

$testadapter_assembly_info = "TestAdapter\Properties\AssemblyInfo.cs"
$testadaptertests_assembly_info = "TestAdapter.Tests\Properties\AssemblyInfo.cs"

$vsix_assembly_info = "VsPackage\Properties\AssemblyInfo.cs"
$vsixtests_assembly_info = "VsPackage.Tests\Properties\AssemblyInfo.cs"

$vsix_manifest = "VsPackage\source.extension.vsixmanifest"


(Get-Content $core_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $core_assembly_info
(Get-Content $coretests_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $coretests_assembly_info

(Get-Content $dia_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $dia_assembly_info
(Get-Content $diatests_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $diatests_assembly_info

(Get-Content $testadapter_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $testadapter_assembly_info
(Get-Content $testadaptertests_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $testadaptertests_assembly_info

(Get-Content $vsix_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $vsix_assembly_info
(Get-Content $vsixtests_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $vsixtests_assembly_info

(Get-Content $vsix_manifest) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $vsix_manifest
