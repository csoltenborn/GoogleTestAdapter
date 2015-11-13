@echo off

if "%*"=="" goto USAGE

echo Ignoring tests: %*
PowerShell.exe -ExecutionPolicy Bypass -Command .\IgnoreTests.ps1 -tests %*
goto :eof


:USAGE
echo Usage  : IgnoreTests <test class>::<test name>[,<test class>::<test name>]*
echo Example: IgnoreTests MyTests.cs::ATest,Helpers\MyOtherTests.cs::AnotherTest
