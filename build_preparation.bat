@echo off
 
set VS_LOCATION=C:\Program Files (x86)\Microsoft Visual Studio\2017\Community
set DIA_SDK="%VS_LOCATION%\DIA SDK\bin"
set VC_VARS_BAT="%VS_LOCATION%\VC\Auxiliary\Build\vcvars32.bat"
set MS_BUILD="%VS_LOCATION%\MSBuild\15.0\Bin\MSBuild.exe"


if defined APPVEYOR goto Build


echo:
echo ======================================================================
echo You have to accept the following licenses before executing this batch file:
echo:
echo Google Test: BSD-3-Clause (https://raw.githubusercontent.com/google/googletest/master/googletest/LICENSE)
echo:
set /p input= "Do you accept these licenses? (yes/no) "

if not "%input%" == "yes" goto End


:Build

echo Setting adapter flavor to GTA
powershell -Command "(gc TestAdapterFlavor.props) -replace '>TAfGT<', '>GTA<' | Out-File TestAdapterFlavor.props"

echo Executing T4 scripts
set VisualStudioVersion=15.0
%MS_BUILD% ResolveTTs.proj

echo Removing TAfGT projects (for now)
powershell -ExecutionPolicy Bypass .\Tools\RemoveProjects.ps1 -flavor GTA

echo Restoring NuGet packages
cd GoogleTestAdapter
nuget restore

echo Setting up VS Developer Prompt environment
call %VC_VARS_BAT%

echo Copying DIA dlls
cd DiaResolver
copy %DIA_SDK%\msdia140.dll x86
copy %DIA_SDK%\amd64\msdia140.dll x64

echo Generating dia2.dll
cd dia2
powershell -ExecutionPolicy Bypass .\compile_typelib.ps1

echo NOT building Google Test NuGet packages
cd ..\..\..
rem echo Building Google Test NuGet packages
rem cd ..\..
rem nuget.exe restore GoogleTestAdapter.sln
rem cd ..
rem git submodule init
rem git submodule update
rem cd GoogleTestNuGet
rem powershell .\Build.ps1 -Verbose
rem cd ..


:End
