@ECHO OFF
:: Сборка Libraries\StockDaySummary.dll против установленной игры.
:: Нужен .NET SDK (dotnet). Путь к игре можно передать первым аргументом:
::   build.cmd "D:\Games\Stardeus"
SETLOCAL
CD /D "%~dp0"

IF "%~1"=="" (
  SET "GAME_DIR=C:\Program Files (x86)\Steam\steamapps\common\Stardeus"
) ELSE (
  SET "GAME_DIR=%~1"
)

:: Проверяем, что DLL игры на месте, иначе сборка упадёт с непонятными ошибками
IF NOT EXIST "%GAME_DIR%\Stardeus_Data\Managed\Game.dll" (
  ECHO Game.dll not found in "%GAME_DIR%\Stardeus_Data\Managed"
  EXIT /B 1
)

:: Чистим кэш obj, чтобы не подхватить сборку под другую версию игры
IF EXIST ".vscode\obj" RMDIR /S /Q ".vscode\obj"

dotnet build ".vscode\mod.csproj" -c Release -p:StardeusDir="%GAME_DIR%"
EXIT /B %ERRORLEVEL%
