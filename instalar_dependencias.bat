@echo off
echo ============================================================
echo   Instalador de dependencias - Compilador MiniGo
echo   Instituto Tecnologico de Costa Rica
echo ============================================================
echo.

:: Verificar si dotnet esta instalado
dotnet --version >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo [OK] .NET SDK encontrado:
    dotnet --version
    echo.
    goto :restaurar
)

:: No esta instalado - descargar winget o dar instrucciones
echo [!] .NET SDK no encontrado.
echo.
echo Opciones para instalarlo:
echo.
echo   OPCION A - Automatica (requiere winget, Windows 10/11):
echo     winget install Microsoft.DotNet.SDK.8
echo.
echo   OPCION B - Manual:
echo     1. Ir a https://dotnet.microsoft.com/download/dotnet/8.0
echo     2. Descargar ".NET SDK 8.0.x" para Windows x64
echo     3. Ejecutar el instalador
echo     4. Cerrar y volver a abrir esta ventana
echo     5. Ejecutar este script nuevamente
echo.

:: Intentar con winget si esta disponible
winget --version >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo Winget detectado. Intentando instalacion automatica...
    echo.
    winget install Microsoft.DotNet.SDK.8 --accept-package-agreements --accept-source-agreements
    if %ERRORLEVEL% NEQ 0 (
        echo [ERROR] La instalacion automatica fallo. Use la Opcion B manual.
        pause
        exit /b 1
    )
    echo.
    echo [OK] .NET SDK instalado. Por favor cierre y reabra esta ventana,
    echo      luego ejecute ejecutar_ide.bat
    pause
    exit /b 0
) else (
    echo Winget no disponible. Por favor use la Opcion B manual.
    pause
    exit /b 1
)

:restaurar
echo Restaurando paquetes NuGet (ANTLR4, LLVMSharp, AvalonEdit)...
echo Esto puede tardar unos minutos la primera vez...
echo.
dotnet restore MiniGoCompiler.sln
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Fallo la restauracion de paquetes.
    echo Verifique su conexion a internet e intente nuevamente.
    pause
    exit /b 1
)
echo.
echo [OK] Todas las dependencias instaladas correctamente.
echo.
echo Ahora puede ejecutar: ejecutar_ide.bat
echo.
pause
