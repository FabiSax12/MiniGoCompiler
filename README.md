# Mini GO Compiler

Compilador para el lenguaje **Mini GO** — subconjunto de GoLang que genera código máquina real x86 via LLVM.

Desarrollado con **C# / .NET 8**, **ANTLR4 4.13.1**, **LLVMSharp.Interop 20.1.2** y **WPF + AvalonEdit 6.3.1**.

**Integrantes:**
- Fabián Ricardo Vargas Araya
- Joseh Daniel Salas Rivas

---

## ▶ Ejecución rápida sin Rider (solo .NET SDK)

> **Si no tenés Rider instalado**, podés compilar y ejecutar el IDE completo con solo el .NET SDK.

### Requisito único: .NET 8 SDK

Verificar si ya está instalado:
```cmd
dotnet --version
```

Si no está instalado, instalarlo con winget (Windows 10/11):
```cmd
winget install Microsoft.DotNet.SDK.8 --accept-package-agreements --accept-source-agreements
```
O descarga manual: https://dotnet.microsoft.com/download/dotnet/8.0 → *SDK 8.0.x, Windows x64*

Después de instalar, **cerrar y reabrir** la terminal.

---

### Paso 1 — Ubicarse en la raíz del proyecto

```cmd
cd ruta\a\MiniGoCompiler
```
*(la carpeta que contiene `MiniGoCompiler.sln`)*

### Paso 2 — Restaurar dependencias (solo la primera vez)

Descarga ANTLR4, LLVMSharp y AvalonEdit desde NuGet (~300 MB, se cachea):
```cmd
dotnet restore MiniGoCompiler.sln
```

### Paso 3 — Compilar toda la solución

```cmd
dotnet build MiniGoCompiler.sln -c Release
```

### Paso 4 — Ejecutar el IDE

**CMD:**
```cmd
IDE\bin\Release\net8.0-windows\IDE.exe
```

**PowerShell:**
```powershell
.\IDE\bin\Release\net8.0-windows\IDE.exe
```

---

### Todo junto (una sola línea)

**CMD:**
```cmd
dotnet restore MiniGoCompiler.sln && dotnet build MiniGoCompiler.sln -c Release && IDE\bin\Release\net8.0-windows\IDE.exe
```

**PowerShell:**
```powershell
dotnet restore MiniGoCompiler.sln; if ($?) { dotnet build MiniGoCompiler.sln -c Release }; if ($?) { .\IDE\bin\Release\net8.0-windows\IDE.exe }
```

---

### Comandos adicionales

| Acción | Comando |
|---|---|
| Correr tests | `dotnet test MiniGoCompiler.sln -c Release` |
| Compilar solo el compilador | `dotnet build MiniGo.Compiler\MiniGo.Compiler.csproj -c Release` |
| Limpiar binarios | `dotnet clean MiniGoCompiler.sln` |

### Solución de problemas

| Síntoma | Causa | Solución |
|---|---|---|
| `libLLVM.dll not found` | Faltó el restore o se usó Debug | Correr `dotnet restore` y usar `-c Release` |
| Colores del IDE no cargan | `MiniGoHighlighting.xshd` no está junto al exe | Se copia automáticamente al compilar con `-c Release` |
| Error al compilar en Debug | `MiniGo.Compiler` requiere `win-x64` | Siempre usar `-c Release` |
| El IDE no abre | WPF solo corre en Windows | Ejecutar desde Windows |

> Los scripts `instalar_dependencias.bat` y `ejecutar_ide.bat` en la raíz del proyecto automatizan estos pasos para Windows.

---

## Desarrollo con Rider (flujo completo)

### Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
- [JetBrains Rider](https://www.jetbrains.com/rider/)
- [Java (para ANTLR)](https://adoptium.net/) — solo si se regenera la gramática

Verificar instalaciones:
```powershell
dotnet --version
java -version
```

### Extensión ANTLR en Rider

Solo necesaria si se modifica la gramática:
```
File → Settings → Plugins → buscar "ANTLR v4" → instalar → reiniciar
```
Plugin: https://plugins.jetbrains.com/plugin/7358-antlr-v4

### Clonar y abrir

```powershell
git clone https://github.com/FabiSax12/MiniGoCompiler.git
cd MiniGoCompiler
dotnet restore
```

Abrir `MiniGoCompiler.sln` en Rider. Establecer `IDE` como proyecto de inicio y ejecutar con F5.

---

## Estructura del proyecto

```text
MiniGoCompiler/
├── MiniGo.Compiler/
│   ├── Grammar/        # Gramática ANTLR (.g4)
│   ├── Generated/      # Código generado por ANTLR (no editar)
│   ├── Semantic/       # TypeChecker — chequeo de tipos y alcances
│   ├── Symbols/        # Tabla de símbolos (SymbolsTable, VarSymbol, MethodSymbol)
│   ├── Encoder/        # MiniGoEncoder — generación de código LLVM IR
│   └── Errors/         # Manejo y reporte de errores
├── IDE/
│   ├── MainWindow.xaml(.cs)       # IDE principal (WPF + AvalonEdit)
│   ├── MiniGoHighlighting.xshd    # Resaltado de sintaxis
│   └── tools/                     # MinGW linker + runtime (ld.lld.exe)
├── MiniGo.Compiler.Tests/         # Tests unitarios y de integración
├── samples/                       # Archivos .mgo de prueba
├── docs/                          # Documentación del proyecto
└── MiniGoCompiler.sln
```

---

## Dependencias

```xml
<!-- MiniGo.Compiler -->
<PackageReference Include="Antlr4.Runtime.Standard" Version="4.13.1" />
<PackageReference Include="LLVMSharp.Interop" Version="20.1.2" />

<!-- IDE -->
<PackageReference Include="AvalonEdit" Version="6.3.1.120" />
```

---

## Convenciones

- No modificar archivos en `Generated/` — son generados por ANTLR automáticamente.
- Regenerar el parser cada vez que cambie la gramática (`MiniGo.Compiler/Grammar/MiniGo.g4`).
- Commits pequeños, descriptivos y en español.
- Mantener separadas las etapas: Parsing → Semantic → Encoder.

---

## Tecnologías

| Herramienta | Versión | Uso |
|---|---|---|
| C# / .NET | 8.0 | Lenguaje de implementación |
| ANTLR4 | 4.13.1 | Lexer y parser |
| LLVMSharp.Interop | 20.1.2 | Generación de código x86 |
| AvalonEdit | 6.3.1 | Editor de texto en el IDE |
| WPF | .NET 8 | Interfaz gráfica |
| MinGW (ld.lld) | bundled | Enlazado del objeto a ejecutable |
