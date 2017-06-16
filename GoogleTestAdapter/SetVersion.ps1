Param([parameter(Mandatory=$true)] [string] $version)

$common_assembly_info = "Common\Properties\AssemblyInfo.cs"

$core_assembly_info = "Core\Properties\AssemblyInfo.cs"
$coretests_assembly_info = "Core.Tests\Properties\AssemblyInfo.cs"

$dia_assembly_info = "DiaResolver\Properties\AssemblyInfo.cs"
$diatests_assembly_info = "DiaResolver.Tests\Properties\AssemblyInfo.cs"

$packaging_gta_assembly_info = "Packaging.GTA\Properties\AssemblyInfo.cs"
$packaging_tafgt_assembly_info = "Packaging.TAfGT\Properties\AssemblyInfo.cs"
$projecttemplates_vstemplate = "ProjectTemplates\Test\GoogleTest\GoogleTest.vstemplate"

$testadapter_assembly_info = "TestAdapter\Properties\AssemblyInfo.cs"
$testadaptertests_assembly_info = "TestAdapter.Tests\Properties\AssemblyInfo.cs"

$vsdebuggerattacherwrapper_assembly_info = "VsDebuggerAttacherWrapper\Properties\AssemblyInfo.cs"

$vsix_assembly_info = "VsPackage\Properties\AssemblyInfo.cs"
$vsixtests_assembly_info = "VsPackage.Tests\Properties\AssemblyInfo.cs"
$vsixunittests_assembly_info = "VsPackage.Tests.Unit\Properties\AssemblyInfo.cs"
$vsixgeneratedtests_assembly_info = "VsPackage.Tests.Generated\Properties\AssemblyInfo.cs"

$vsix_manifest_gta = "Packaging.GTA\source.extension.vsixmanifest"
$vsix_manifest_tafgt = "Packaging.TAfGT\source.extension.vsixmanifest"

$wizard_assembly_info = "NewProjectWizard\Properties\AssemblyInfo.cs"

(Get-Content $common_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $common_assembly_info

(Get-Content $core_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $core_assembly_info
(Get-Content $coretests_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $coretests_assembly_info

(Get-Content $dia_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $dia_assembly_info
(Get-Content $diatests_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $diatests_assembly_info

(Get-Content $packaging_gta_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $packaging_gta_assembly_info
(Get-Content $packaging_tafgt_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $packaging_tafgt_assembly_info
(Get-Content $projecttemplates_vstemplate) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $projecttemplates_vstemplate

(Get-Content $testadapter_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $testadapter_assembly_info
(Get-Content $testadaptertests_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $testadaptertests_assembly_info

(Get-Content $vsdebuggerattacherwrapper_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $vsdebuggerattacherwrapper_assembly_info

(Get-Content $vsix_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $vsix_assembly_info
(Get-Content $vsixtests_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $vsixtests_assembly_info
(Get-Content $vsixunittests_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $vsixunittests_assembly_info
(Get-Content $vsixgeneratedtests_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $vsixgeneratedtests_assembly_info

(Get-Content $vsix_manifest_gta) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $vsix_manifest_gta
(Get-Content $vsix_manifest_tafgt) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $vsix_manifest_tafgt

(Get-Content $wizard_assembly_info) | ForEach-Object { $_ -replace "0.1.0.0", $version } | Set-Content $wizard_assembly_info
