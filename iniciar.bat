@echo off
setlocal
title Autolavado - dotnet watch run
cd /d "%~dp0"

echo ============================================================
echo  Sistema de Gestion de Autolavado
echo  Iniciando servidor de desarrollo con: dotnet watch run
echo  Directorio: %CD%
echo ============================================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] No se encontro 'dotnet' en el PATH.
    echo Instale el .NET SDK 10 o superior y vuelva a intentarlo.
    echo.
    pause
    exit /b 1
)

if not exist "%~dp0PrototipoCRUD.csproj" (
    echo [ERROR] No se encontro PrototipoCRUD.csproj en:
    echo         %~dp0
    echo.
    pause
    exit /b 1
)

dotnet watch run
set EXITCODE=%ERRORLEVEL%

echo.
if not "%EXITCODE%"=="0" (
    echo [ERROR] dotnet watch run termino con codigo %EXITCODE%.
) else (
    echo El servidor se detuvo correctamente.
)
echo.
pause
endlocal
