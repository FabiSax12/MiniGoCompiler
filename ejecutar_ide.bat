@echo off
echo ============================================================
echo   Compilador MiniGo - IDE
echo   Instituto Tecnologico de Costa Rica
echo ============================================================
echo.

:: Verificar dotnet
dotnet --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] .NET SDK no encontrado.
    echo Ejecute primero: instalar_dependencias.bat
    pause
    exit /b 1
)

:: Obtener directorio del script (raiz del proyecto)
set "SCRIPT_DIR=%~dp0"

echo Compilando proyecto...
dotnet build "%SCRIPT_DIR%IDE\IDE.csproj" -c Release --nologo -v quiet
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] La compilacion fallo.
    echo Asegurese de haber ejecutado instalar_dependencias.bat primero.
    pause
    exit /b 1
)

echo [OK] Compilacion exitosa. Iniciando IDE...
echo.

:: Ejecutar el binario compilado directamente (no dotnet run)
:: Esto garantiza que el working directory sea el de la DLL
set "IDE_EXE=%SCRIPT_DIR%IDE\bin\Release\net8.0-windows\IDE.exe"
if exist "%IDE_EXE%" (
    start "" "%IDE_EXE%"
) else (
    echo [!] No se encontro IDE.exe, usando dotnet run...
    cd /d "%SCRIPT_DIR%IDE"
    start "" dotnet run --no-build -c Release
)
