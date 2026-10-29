@ECHO OFF
:: Сборка <версия>\Libraries\StockDaySummary.dll против установленной игры.
:: Нужен .NET SDK (dotnet). Собирается под ту версию игры, что сейчас установлена:
::   build.cmd                      :: v0.15, игра в папке Steam по умолчанию
::   build.cmd 0.14                 :: v0.14 (нужна ветка v0_14_x в Steam)
::   build.cmd 0.15 "D:\Games\Stardeus"
SETLOCAL
CD /D "%~dp0"

SET "TARGET=%~1"
IF "%TARGET%"=="" SET "TARGET=0.15"

:: Разрешены только версии, для которых есть папка Code\V0xx
IF NOT "%TARGET%"=="0.15" IF NOT "%TARGET%"=="0.14" (
  ECHO Unknown game version "%TARGET%". Use 0.15 or 0.14.
  EXIT /B 1
)

IF "%~2"=="" (
  SET "GAME_DIR=C:\Program Files (x86)\Steam\steamapps\common\Stardeus"
) ELSE (
  SET "GAME_DIR=%~2"
)

:: Проверяем, что DLL игры на месте, иначе сборка упадёт с непонятными ошибками
IF NOT EXIST "%GAME_DIR%\Stardeus_Data\Managed\Game.dll" (
  ECHO Game.dll not found in "%GAME_DIR%\Stardeus_Data\Managed"
  EXIT /B 1
)

:: Чистим кэш obj этой версии, чтобы не подхватить сборку под другую версию игры
IF EXIST ".vscode\obj\%TARGET%" RMDIR /S /Q ".vscode\obj\%TARGET%"

dotnet build ".vscode\mod.csproj" -c Release -p:GameTarget=%TARGET% -p:StardeusDir="%GAME_DIR%"
EXIT /B %ERRORLEVEL%
