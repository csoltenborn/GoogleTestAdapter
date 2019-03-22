@echo off
cd "%~dp0"
rem 
rem https://stackoverflow.com/questions/42713238/reliable-way-to-find-the-location-devenv-exe-of-visual-studio-2017/48915860#48915860
rem 
for /f "tokens=1,2*" %%a in ('reg query "HKLM\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\SxS\VS7" /v "15.0" 2^>nul') do set "VSPATH=%%c"
if "%VSPATH%" == "" (
    echo Visual studio 2017 is not installed on this machine
    exit /b
)

rem echo Visual studio %1 path is "%VSPATH%"
if %VisualStudioVersion%. == . call "%VSPATH%\Common7\Tools\VsDevCmd.bat"

echo Restoring nuget packages...
nuget restore GoogleTestAdapter\GoogleTestAdapter.sln >logNuget.txt
if errorlevel 1 type logNuget.txt && exit /b 1
del logNuget.txt >nul 2>&1

echo Building ResolveTTs T4 templates...
"%VSPATH%\MSBuild\15.0\Bin\MSBuild.exe" ResolveTTs.proj >logResolveTTs.txt
if errorlevel 1 type logResolveTTs.txt && exit /b 1
del logResolveTTs.txt >nul 2>&1

echo Copying dia140.dll's...
set "DIA_SDK=%VSPATH%\DIA SDK\bin"
pushd GoogleTestAdapter\DiaResolver
copy "%DIA_SDK%\msdia140.dll" x86
copy "%DIA_SDK%\amd64\msdia140.dll" x64
popd

echo Building dia2.dll...
pushd GoogleTestAdapter\DiaResolver\dia2
powershell -ExecutionPolicy Bypass .\compile_typelib.ps1 >logDia2.txt 2>&1
if errorlevel 1 type logDia2.txt && exit /b 1
del logDia2.txt >nul 2>&1
popd

