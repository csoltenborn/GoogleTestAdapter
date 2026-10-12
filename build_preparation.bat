@echo off
 
set VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
for /f "usebackq tokens=*" %%i in (`%VSWHERE% -latest -prerelease -products * -requires Microsoft.VisualStudio.Component.VSSDK -property installationPath`) do set VS_LOCATION=%%i
if not defined VS_LOCATION set VS_LOCATION=C:\Program Files (x86)\Microsoft Visual Studio\2017\Community
echo Using Visual Studio at %VS_LOCATION%

set MS_BUILD="%VS_LOCATION%\MSBuild\Current\Bin\MSBuild.exe"


if defined APPVEYOR goto Build


setlocal

echo:
echo ===========================================================================
echo You have to accept the following licenses before executing this batch file:
echo:
echo Microsoft DIA SDK: Visual Studio End-User License Agreement (https://www.visualstudio.com/license-terms/mlt687465)
echo Google Test: BSD-3-Clause (https://raw.githubusercontent.com/google/googletest/675686a139a731a2c796633e67e9421792363709/googletest/LICENSE)
echo:
set /p input= "Do you accept these licenses? (yes/no) "

if not "%input%" == "yes" goto End


:Build

echo Setting adapter flavor to GTA
powershell -Command "(gc TestAdapterFlavor.props) -replace '>TAfGT<', '>GTA<' | Out-File TestAdapterFlavor.props"

rem echo Executing T4 scripts
rem set VisualStudioVersion=15.0
rem %MS_BUILD% ResolveTTs.proj

rem echo Removing TAfGT projects (for now)
rem powershell -ExecutionPolicy Bypass .\Tools\RemoveProjects.ps1 -flavor GTA

echo Restoring NuGet packages
pushd GoogleTestAdapter
nuget restore

rem msdia140.dll and the DIA interop assembly dia2.dll are prepared by DiaResolver.csproj during the build
popd

echo NOT building Google Test NuGet packages
rem echo Building Google Test NuGet packages
rem git submodule init
rem git submodule update
rem pushd GoogleTestNuGet
rem powershell .\Build.ps1 -Verbose
rem popd


:End
