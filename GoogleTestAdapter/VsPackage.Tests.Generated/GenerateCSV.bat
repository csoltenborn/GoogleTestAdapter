@echo off

set BASE_DIR=%~dp0

set PICT_EXE="%BASE_DIR%pict\pict.exe"
set MODEL="%BASE_DIR%GTA_Console.pictmodel"
set CSV="%BASE_DIR%GTA_Console.csv"
set TEMP_SEED="%BASE_DIR%seed.pictmodel"


if exist %CSV% (
  echo Generating test data using existing data as seed =====
  mv %CSV% %TEMP_SEED%
  %PICT_EXE% %MODEL% -a:# -e:%TEMP_SEED% >%CSV%
  del %TEMP_SEED%
) else (
  echo Generating test data from scratch =====
  %PICT_EXE% %MODEL% -a:# >%CSV%
)
echo ===== Generation done, target file is
echo %CSV%
