<#
.PARAMETER VSPath
Optional path to the Visual Studio install root, which makes
$VSPath\Common7\IDE\devenv.exe available. If unspecified, assumes devenv.exe is
available through PATH, e.g. made through Developer Command Prompt for VS.
#>
#requires -Version 3.0
[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
        [ValidateScript({Test-Path $_ -PathType 'Container'})]
        [string]$VSPath
)
Set-StrictMode -Version Latest
$WarningPreference = "Stop"
$ErrorActionPreference = "Stop"

<#
.SYNOPSIS
Invokes a command, redirects its output to the verbose stream, and throws on
failure.
#>
function Invoke-Executable {
    param(
        [String]$Path,
        [String[]]$Parameters
    )
    Write-Verbose "Invoking: $Path $Parameters"
    Write-Verbose "********************"
    try {
        &$Path $Parameters | Write-Verbose
        if (!$?) {
            throw "Invoke-Executable failed for `"$Path $Parameters`" with working directory `"$pwd`"."
        }
    } finally {
        Write-Verbose "********************"
    }
}

<#
.SYNOPSIS
Converts a Boolean value to string "ON"/"OFF" for true/false.
#>
function Convert-BooleanToOnOff {
    param([Boolean]$Value)
    if ($Value) { "ON" }
    else { "OFF" }
}

<#
.SYNOPSIS
Copies a file to a specified file name, creating all intermediate directories.
#>
function Copy-CreateItem {
    param(
        [String]$Path,
        [String]$Destination
    )
    New-Item -ItemType File -Path $Destination -Force
    Copy-Item -Path $Path -Destination $Destination
}

function Convert-DynamicLibraryLinkageToString {
    param([Boolean]$Value)
    if ($Value) { "dyn" }
    else { "static" }
}

function Convert-DynamicCRTLinkageToString {
    param([Boolean]$Value)
    if ($Value) { "rt-dyn" }
    else { "rt-static" }
}

function New-CleanDirectory {
    param([String]$Path)
    if (Test-Path $Path) { Remove-Item -Recurse -Path $Path }
    New-Item -ItemType Directory -Path $Path
}

function Create-WorkingDirectory {
    param(
        [String]$Prefix,
        [String]$ToolsetName,
        [String]$BuildToolset,
        [String]$Platform,
        [Boolean]$DynamicLibraryLinkage,
        [Boolean]$DynamicCRTLinkage
    )

    $Name = "Intermediate/$Prefix.$ToolsetName.windesktop.msvcstl"
    $Name += ".$(Convert-DynamicLibraryLinkageToString $DynamicLibraryLinkage)"
    $Name += ".$(Convert-DynamicCRTLinkageToString $DynamicCRTLinkage)"
    if ($Platform -ne "") { $Name += ".$Platform" }

    New-CleanDirectory $Name
}

function Invoke-BatchFile {
    param(
        [string]$Path,
        [string]$Parameters
    )

    $tempFile = [IO.Path]::GetTempFileName()

    # Store the output of cmd.exe.  We also ask cmd.exe to output
    # the environment table after the batch file completes
    cmd.exe /c " `"$Path`" $Parameters && set " > $tempFile

    # Go through the environment variables in the temp file.
    # For each of them, set the variable in our local environment.
    Get-Content $tempFile | Foreach-Object {
        if ($_ -match "^(.*?)=(.*)$") {
            Set-Content "env:\$($matches[1])" $matches[2]
        }
    }

    Remove-Item $tempFile
}

function Add-Signing {
    param(
        [String]$Directory,
        [String]$ProjectName
    )

    $xml = [xml](Get-Content "$Directory\$ProjectName.vcxproj")

    $MicroBuildProps = $xml.CreateElement("Import", "http://schemas.microsoft.com/developer/msbuild/2003")
    $MicroBuildProps.SetAttribute("Project", "..\..\..\NuGetPackages\MicroBuild.Core.0.2.0\build\MicroBuild.Core.props")
    $MicroBuildProps.SetAttribute("Condition", "Exists('..\..\..\NuGetPackages\MicroBuild.Core.0.2.0\build\MicroBuild.Core.props')")

    $RealSignGroup = $xml.CreateElement("PropertyGroup", "http://schemas.microsoft.com/developer/msbuild/2003")
    $RealSignGroup.SetAttribute("Condition", "'`$(RealSign)' == 'True'")
    $SignAsm = $xml.CreateElement("SignAssembly", "http://schemas.microsoft.com/developer/msbuild/2003")
    $SignAsm.set_InnerXML("true")
    $DelaySign = $xml.CreateElement("DelaySign", "http://schemas.microsoft.com/developer/msbuild/2003")
    $DelaySign.set_InnerXML("true")
    $RealSignGroup.AppendChild($SignAsm) | Out-Null
    $RealSignGroup.AppendChild($DelaySign) | Out-Null

    $FileSignGroup = $xml.CreateElement("ItemGroup", "http://schemas.microsoft.com/developer/msbuild/2003")
    $FilesToSign = $xml.CreateElement("FilesToSign", "http://schemas.microsoft.com/developer/msbuild/2003")
    $FilesToSign.SetAttribute("Include", "`$(OutDir)\$ProjectName.dll")
    $FilesToSign.SetAttribute("Condition", "'`$(RealSign)' == 'True' and '`$(TargetExt)' == '.dll'")
    $Authenticode = $xml.CreateElement("Authenticode", "http://schemas.microsoft.com/developer/msbuild/2003")
    $Authenticode.set_InnerXML("Microsoft")
    $StrongName = $xml.CreateElement("StrongName", "http://schemas.microsoft.com/developer/msbuild/2003")
    $StrongName.set_InnerXML("StrongName")
    $FilesToSign.AppendChild($Authenticode) | Out-Null
    $FilesToSign.AppendChild($StrongName) | Out-Null
    $FileSignGroup.AppendChild($FilesToSign) | Out-Null

    $MicroBuildTargets = $xml.CreateElement("Import", "http://schemas.microsoft.com/developer/msbuild/2003")
    $MicroBuildTargets.SetAttribute("Project", "..\..\..\NuGetPackages\MicroBuild.Core.0.2.0\build\MicroBuild.Core.targets")
    $MicroBuildTargets.SetAttribute("Condition", "Exists('..\..\..\NuGetPackages\MicroBuild.Core.0.2.0\build\MicroBuild.Core.targets')")

    $xml.Project.AppendChild($MicroBuildProps) | Out-Null
    $xml.Project.AppendChild($RealSignGroup) | Out-Null
    $xml.Project.AppendChild($FileSignGroup) | Out-Null
    $xml.Project.AppendChild($MicroBuildTargets) | Out-Null

    $xml.Save("$Directory\$ProjectName.vcxproj")
}

function Build-Binaries {
    param(
        [String]$ToolsetName,
        [String]$BuildToolset,
        [String]$Platform,
        [Boolean]$DynamicLibraryLinkage,
        [Boolean]$DynamicCRTLinkage
    )

    $Dir = Create-WorkingDirectory -Prefix "build" -ToolsetName $ToolsetName -BuildToolset $BuildToolset -Platform $Platform `
        -DynamicLibraryLinkage $DynamicLibraryLinkage -DynamicCRTLinkage $DynamicCRTLinkage

    $CMakeDir = "$pwd\..\ThirdParty\googletest\googletest"

    Push-Location $Dir
    try {
        $CMakeArgs = @()
        $CMakeArgs += "-G", "Visual Studio 15 2017"
        $CMakeArgs += "-T", $BuildToolset
        $CMakeArgs += "-A", $Platform
        $CMakeArgs += "-D", "BUILD_SHARED_LIBS=$(Convert-BooleanToOnOff $DynamicLibraryLinkage)"
        $CMakeArgs += "-D", "gtest_force_shared_crt=$(Convert-BooleanToOnOff $DynamicCRTLinkage)"
        $CMakeArgs += $CMakeDir
        Invoke-Executable cmake $CMakeArgs

        Add-Signing -Directory $Dir -ProjectName "gtest"
        Add-Signing -Directory $Dir -ProjectName "gtest_main"

        Invoke-Executable msbuild @("gtest.vcxproj",      "/p:Configuration=Debug")
        Invoke-Executable msbuild @("gtest_main.vcxproj", "/p:Configuration=Debug")
        Invoke-Executable msbuild @("gtest.vcxproj",      "/p:Configuration=RelWithDebInfo")
        Invoke-Executable msbuild @("gtest_main.vcxproj", "/p:Configuration=RelWithDebInfo")
    } finally {
        Pop-Location
    }

    $Dir
}

function Build-NuGet {
    param(
        [String]$BuildDir32,
        [String]$BuildDir64,
        [String]$ToolsetName,
        [String]$BuildToolset,
        [Boolean]$DynamicLibraryLinkage,
        [Boolean]$DynamicCRTLinkage,
        [String]$OutputDir
    )

    $Dir = Create-WorkingDirectory -Prefix "nuget" -ToolsetName $ToolsetName -BuildToolset $BuildToolset `
        -DynamicLibraryLinkage $DynamicLibraryLinkage -DynamicCRTLinkage $DynamicCRTLinkage

    $BaseName = "$ToolsetName.windesktop.msvcstl"
    $BaseName += ".$(Convert-DynamicLibraryLinkageToString $DynamicLibraryLinkage)"
    $BaseName += ".$(Convert-DynamicCRTLinkageToString $DynamicCRTLinkage)"
    $PackageName = "Microsoft.googletest.$BaseName"
    $PackageNameDashes = $PackageName.Replace(".", "-")
    $PathToBinaries = "lib\native\" + $BaseName.Replace(".", "\")

    New-Item -ItemType Directory -Path "$Dir\build\native"

    $TargetsTTArgs = @()
    $TargetsTTArgs += "/p:PackageName=`"$PackageName`""
    $TargetsTTArgs += "/p:PackageNameDashes=`"$PackageNameDashes`""
    $TargetsTTArgs += "/p:PathToBinaries=`"$PathToBinaries`""
    $TargetsTTArgs += "/p:ConfigurationType=`"$(Convert-DynamicLibraryLinkageToString $DynamicLibraryLinkage)`""
    $TargetsTTArgs += "/p:OutputFileName=`"$Dir\build\native\$PackageName.targets`""
    $TargetsTTArgs += "googletest.targets.tt.proj"
    Invoke-Executable msbuild $TargetsTTArgs

    $PropertiesUITTArgs = @()
    $PropertiesUITTArgs += "/p:PackageNameDashes=`"$PackageNameDashes`""
    $PropertiesUITTArgs += "/p:OutputFileName=`"$Dir\build\native\$PackageName.propertiesui.xml`""
    $PropertiesUITTArgs += "googletest.propertiesui.xml.tt.proj"
    Invoke-Executable msbuild $PropertiesUITTArgs

    Copy-Item -Recurse -Path "..\ThirdParty\googletest\googletest\include" -Destination "$Dir\build\native\include"

    $BuildToDestinationPath = @()
    $BuildToDestinationPath += ,@($BuildDir32, "$Dir\$PathToBinaries\x86")
    $BuildToDestinationPath += ,@($BuildDir64, "$Dir\$PathToBinaries\x64")
    $BuildToDestinationPath | ForEach-Object {
        $BuildPath = $_[0]
        $DestinationPath = $_[1]

        if ($DynamicLibraryLinkage) {
            Copy-CreateItem -Path "$BuildPath\Debug\gtest.dll"      -Destination "$DestinationPath\Debug\gtest.dll"
            Copy-CreateItem -Path "$BuildPath\Debug\gtest.lib"      -Destination "$DestinationPath\Debug\gtest.lib"
            Copy-CreateItem -Path "$BuildPath\Debug\gtest.pdb"      -Destination "$DestinationPath\Debug\gtest.pdb"
            Copy-CreateItem -Path "$BuildPath\Debug\gtest_main.dll" -Destination "$DestinationPath\Debug\gtest_main.dll"
            Copy-CreateItem -Path "$BuildPath\Debug\gtest_main.lib" -Destination "$DestinationPath\Debug\gtest_main.lib"
            Copy-CreateItem -Path "$BuildPath\Debug\gtest_main.pdb" -Destination "$DestinationPath\Debug\gtest_main.pdb"

            Copy-CreateItem -Path "$BuildPath\RelWithDebInfo\gtest.dll"      -Destination "$DestinationPath\Release\gtest.dll"
            Copy-CreateItem -Path "$BuildPath\RelWithDebInfo\gtest.lib"      -Destination "$DestinationPath\Release\gtest.lib"
            Copy-CreateItem -Path "$BuildPath\RelWithDebInfo\gtest.pdb"      -Destination "$DestinationPath\Release\gtest.pdb"
            Copy-CreateItem -Path "$BuildPath\RelWithDebInfo\gtest_main.dll" -Destination "$DestinationPath\Release\gtest_main.dll"
            Copy-CreateItem -Path "$BuildPath\RelWithDebInfo\gtest_main.lib" -Destination "$DestinationPath\Release\gtest_main.lib"
            Copy-CreateItem -Path "$BuildPath\RelWithDebInfo\gtest_main.pdb" -Destination "$DestinationPath\Release\gtest_main.pdb"
        } else {
            Copy-CreateItem -Path "$BuildPath\Debug\gtest.lib"                              -Destination "$DestinationPath\Debug\gtest.lib"
            Copy-CreateItem -Path "$BuildPath\Debug\gtest.pdb"                              -Destination "$DestinationPath\Debug\gtest.pdb"
            Copy-CreateItem -Path "$BuildPath\Debug\gtest_main.lib"                         -Destination "$DestinationPath\Debug\gtest_main.lib"
            Copy-CreateItem -Path "$BuildPath\Debug\gtest_main.pdb"                         -Destination "$DestinationPath\Debug\gtest_main.pdb"

            Copy-CreateItem -Path "$BuildPath\RelWithDebInfo\gtest.lib"                     -Destination "$DestinationPath\Release\gtest.lib"
            Copy-CreateItem -Path "$BuildPath\gtest.dir\RelWithDebInfo\gtest.pdb"           -Destination "$DestinationPath\Release\gtest.pdb"
            Copy-CreateItem -Path "$BuildPath\RelWithDebInfo\gtest_main.lib"                -Destination "$DestinationPath\Release\gtest_main.lib"
            Copy-CreateItem -Path "$BuildPath\gtest_main.dir\RelWithDebInfo\gtest_main.pdb" -Destination "$DestinationPath\Release\gtest_main.pdb"
        }
    }

    Copy-CreateItem -Recurse -Path "license (MIT).txt"     -Destination "$Dir\license (MIT).txt"
    Copy-CreateItem -Recurse -Path "ThirdPartyNotices.txt" -Destination "$Dir\ThirdPartyNotices.txt"

    $NuspecTTArgs = @()
    $NuspecTTArgs += "/p:PackageName=`"$PackageName`""
    $NuspecTTArgs += "/p:OutputFileName=`"$Dir\googletest.nuspec`""
    $NuspecTTArgs += "googletest.nuspec.tt.proj"
    Invoke-Executable msbuild $NuspecTTArgs

    $NugetPackArgs = @()
    $NugetPackArgs += "pack", "$Dir\googletest.nuspec"
    $NugetPackArgs += "-OutputDirectory", $OutputDir
    $NugetPackArgs += "-BasePath", $Dir
    Invoke-Executable nuget $NugetPackArgs
}

function Build-BinariesAndNuGet {
    param(
        [String]$ToolsetName,
        [String]$BuildToolset,
        [Boolean]$DynamicLibraryLinkage,
        [Boolean]$DynamicCRTLinkage,
        [String]$OutputDir
    )

    $BuildDir32 = Build-Binaries -ToolsetName $ToolsetName -BuildToolset $BuildToolset -Platform "Win32" -DynamicLibraryLinkage $DynamicLibraryLinkage `
        -DynamicCRTLinkage $DynamicCRTLinkage
    $BuildDir64 = Build-Binaries -ToolsetName $ToolsetName -BuildToolset $BuildToolset -Platform "x64"   -DynamicLibraryLinkage $DynamicLibraryLinkage `
        -DynamicCRTLinkage $DynamicCRTLinkage
    Build-NuGet -BuildDir32 $BuildDir32 -BuildDir64 $BuildDir64 -ToolsetName $ToolsetName -BuildToolset $BuildToolset `
        -DynamicLibraryLinkage $DynamicLibraryLinkage -DynamicCRTLinkage $DynamicCRTLinkage -OutputDir $OutputDir | Out-Null
}

function Main {
    # Script works either inside VS env, or takes VS path to set it up.
    if ((Get-Command "msbuild" -ErrorAction SilentlyContinue) -eq $null) {
        if ($VSPath) {
            Invoke-BatchFile "$VSPath\Common7\Tools\VsDevCmd.bat"
        } else {
            throw "msbuild is not available. Start from Developer Command Prompt or call script with VS path."
        }
    }

    # Ensure CMake is available.
    if ((Get-Command "cmake" -ErrorAction SilentlyContinue) -eq $null) {
        $env:Path += ";$env:DevEnvDir\CommonExtensions\Microsoft\CMake\CMake\bin"
    }
    Invoke-Executable cmake --version

    # Ensure nuget is available.
    if ((Get-Command "nuget" -ErrorAction SilentlyContinue) -eq $null) {
        if (!(Test-Path "$pwd\..\NuGetPackages\NuGet.CommandLine.3.5.0\tools\NuGet.exe")) {
            throw "nuget.exe is not available. Provide through PATH or restore NuGet packages for the solution."
        }
        $env:Path += ";$pwd\..\NuGetPackages\NuGet.CommandLine.3.5.0\tools"
    }
    Invoke-Executable nuget

    $OutputDir = "..\GoogleTestAdapter\Packages"

    Build-BinariesAndNuGet -ToolsetName "v140" -BuildToolset "v141" -DynamicLibraryLinkage $false -DynamicCRTLinkage $true  -OutputDir $OutputDir
    Build-BinariesAndNuGet -ToolsetName "v140" -BuildToolset "v141" -DynamicLibraryLinkage $false -DynamicCRTLinkage $false -OutputDir $OutputDir
    Build-BinariesAndNuGet -ToolsetName "v140" -BuildToolset "v141" -DynamicLibraryLinkage $true  -DynamicCRTLinkage $true  -OutputDir $OutputDir

    "Success"
}

. Main
