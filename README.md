# Mini GO Compiler

Compilador para el lenguaje **Mini GO** desarrollado con:

- C#
- ANTLR4

---

# Requisitos

Antes de ejecutar el proyecto es necesario instalar las siguientes herramientas.

---

# 1. Instalar .NET SDK (C#)

Descargar e instalar:

https://dotnet.microsoft.com/en-us/download

Verificar instalación:

```powershell
dotnet --version
```
---

# 2. Instalar Rider

Descargar:

[https://www.jetbrains.com/rider/](https://www.jetbrains.com/rider/)

---

# 3. Instalar Java (Necesario para ANTLR)

ANTLR utiliza Java para generar el lexer y parser.

Descargar:

[https://adoptium.net/](https://adoptium.net/)

Verificar instalación:

```powershell
java -version
```

---

# 4. Instalar extensión ANTLR en Rider

Abrir Rider:

```text
File -> Settings -> Plugins
```

Buscar e instalar:

```text
ANTLR v4
```

Plugin:

[https://plugins.jetbrains.com/plugin/7358-antlr-v4](https://plugins.jetbrains.com/plugin/7358-antlr-v4)

Luego reiniciar Rider.

---

# 5. Clonar el proyecto

```powershell
git clone https://github.com/FabiSax12/MiniGoCompiler.git
cd MiniGoCompiler
```

---

# 6. Restaurar dependencias

Desde la raíz del proyecto:

```powershell
dotnet restore
```

---

# 7. Instalar LLVM (requerido para ejecutar archivos .ll)

El compilador genera código LLVM IR (archivos `.ll`). Para **ejecutar** esos archivos desde el IDE (botón Run / F5) se necesita `lli`, el intérprete de LLVM.

## Opción A — winget (recomendado en Windows)

```powershell
winget install LLVM.LLVM
```

Esto instala LLVM en `C:\Program Files\LLVM\bin\` y agrega el directorio al PATH automáticamente.

## Opción B — instalador oficial

Descargar el instalador Windows desde:

https://releases.llvm.org/download.html

Durante la instalación, seleccionar **"Add LLVM to the system PATH"**.

## Verificar instalación

```powershell
lli --version
```

Si el comando no se encuentra después de instalar, reiniciar la terminal o el IDE para que tome el nuevo PATH.

> **Nota:** El compilador (build) no requiere LLVM instalado — solo se necesita para la fase de ejecución.

---

# Dependencias utilizadas

```xml
<ItemGroup>
    <PackageReference Include="Antlr4.Runtime.Standard" Version="4.13.1" />
    <PackageReference Include="LLVMSharp.Interop" Version="20.1.2" />
</ItemGroup>
```

---

# Estructura del proyecto

```text
MiniGoCompiler/
│
├── Grammar/           # Gramática ANTLR (.g4)
│
├── Generated/         # Código generado automáticamente por ANTLR
│
├── AST/               # Nodos del AST
│
├── Semantic/          # Análisis semántico
│
├── Symbols/           # Tabla de símbolos y scopes
│
├── Types/             # Sistema de tipos
│
├── Codegen/           # Generación de código LLVM
│
├── Errors/            # Manejo de errores
│
├── Tests/             # Archivos MiniGO de prueba
│
├── Program.cs         # Punto de entrada
│
├── MiniGoCompiler.csproj
│
└── README.md
```

---

# Generar parser y lexer

Ubicarse en la raíz del proyecto.

## Generar código ANTLR

Hacer uso de la extensión/plugin del IDE para generar el código

Esto genera:

* Lexer
* Parser
* Visitors
* BaseVisitors
* Listeners

---

# Compilar el proyecto

```powershell
dotnet build
```

---

# Ejecutar el proyecto

```powershell
dotnet run
```

---

# Convenciones del proyecto

## Código generado

La carpeta `Generated/` es generada automáticamente por ANTLR.

No modificar manualmente estos archivos.

---

## Gramática

Toda la gramática del lenguaje debe mantenerse en:

```text
Grammar/MiniGo.g4
```

---

## AST

Los nodos del árbol sintáctico abstracto deben ubicarse en:

```text
AST/
```

---

## Semantic Analysis

Toda validación semántica debe implementarse en:

```text
Semantic/
```

Ejemplos:

* variables no declaradas
* redeclaraciones
* chequeo de tipos
* validación de returns
* validación de funciones

---

## LLVM

La generación de código LLVM debe ubicarse en:

```text
Codegen/
```

---

# Recomendaciones

* No modificar archivos generados por ANTLR manualmente.
* Regenerar el parser cada vez que cambie la gramática.
* Hacer commits pequeños y frecuentes.
* Mantener separadas las etapas:

    * Parsing
    * AST
    * Semantic Analysis
    * LLVM

---

# Tecnologías utilizadas

* C#
* .NET 8
* ANTLR4
* LLVM
* Rider

---

# Integrantes

* Fabián Ricardo Vargas Araya
* Joseh Daniel Salas Rivas