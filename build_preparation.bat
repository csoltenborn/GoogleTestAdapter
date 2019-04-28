@echo off
 
set VS_LOCATION=C:\Program Files (x86)\Microsoft Visual Studio\2017\Community
set DIA_SDK="%VS_LOCATION%\DIA SDK\bin"
set VC_VARS_BAT="%VS_LOCATION%\VC\Auxiliary\Build\vcvars32.bat"
set MS_BUILD="%VS_LOCATION%\MSBuild\15.0\Bin\MSBuild.exe"


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

echo Setting up VS Developer Prompt environment
pushd .
call %VC_VARS_BAT%
popd

echo Copying DIA dlls
pushd DiaResolver
copy %DIA_SDK%\msdia140.dll x86
copy %DIA_SDK%\amd64\msdia140.dll x64

echo Generating dia2.dll
pushd dia2
powershell -ExecutionPolicy Bypass .\compile_typelib.ps1

popd
popd
popd

echo NOT building Google Test NuGet packages
rem echo Building Google Test NuGet packages
rem git submodule init
rem git submodule update
rem pushd GoogleTestNuGet
rem powershell .\Build.ps1 -Verbose
rem popd


:End
