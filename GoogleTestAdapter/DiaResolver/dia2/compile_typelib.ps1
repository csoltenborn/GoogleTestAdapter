if (!(Test-Path Env:VSINSTALLDIR)) {
  Write-Output "ERROR: must run in Developer Command Prompt for Visual Studio"
  exit
}

mkdir tmp
midl "$ENV:VSINSTALLDIR/DIA SDK/idl/dia2.idl" /out tmp /I "$ENV:VSINSTALLDIR/DIA SDK/include"
tlbimp tmp/dia2.tlb /out:dia2.dll /namespace:Microsoft.Dia
Remove-Item -Recurse -Force tmp
