if(!(Test-Path Env:VSINSTALLDIR)) {
  echo "ERROR: must run in Developer Command Prompt for Visual Studio"
  exit
}

mkdir tmp
midl 3rdparty/dia2_merged.idl /out tmp /I 3rdparty/msdia110
tlbimp tmp/dia2_merged.tlb    /out:msdia110to140typelib.dll /namespace:Dia
rm -r -fo tmp
