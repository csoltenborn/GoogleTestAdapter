The DiaResolver project has additional dependencies on DIA SDK (which is
installed with Visual Studio) that need to be resolved as follows:

1. Build CLR assembly for DIA SDK:
    1. Open Developer Command Prompt for VS 2017.
    2. Go to `dia2` folder.
    3. Run `compile_typelib.ps1` (e.g. invoking
       `powershell -ExecutionPolicy Bypass .\compile_typelib.ps1`).
    4. `dia2.dll` file should be generated.
2. Copy `msdia140.dll` from `DIA SDK` folder in Visual Studio installation
   directory:
    1. `msdia140.dll` to `x86` folder.
    2. `amd64\msdia140.dll` to `x64` folder.
