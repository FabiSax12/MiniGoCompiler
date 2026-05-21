# AGENTS.md — MiniGo Compiler

## Project Overview

A MiniGo language compiler written in C# (.NET 8) using ANTLR4 for lexing/parsing and LLVMSharp for code generation. Two projects in the solution:

- **MiniGo.Compiler** — Console app (`Program.cs` entry point)
- **IDE** — WPF IDE with AvalonEdit

## Build, Run & Test Commands

```powershell
# Restore dependencies
dotnet restore

# Build the entire solution
dotnet build

# Run the compiler on a source file
dotnet run --project MiniGo.Compiler -- <path-to-.txt-file>

# Run the WPF IDE
dotnet run --project IDE

# Regenerate ANTLR parser/lexer from grammar
# Use Rider's ANTLR v4 plugin: right-click MiniGoParser.g4 → Generate Recognizer
# OR: use the Rider toolbar button when a .g4 file is open
# Output goes to MiniGo.Compiler/Generated/ (git-ignored)
```

### Testing

There is **no C# test project yet**. The `Tests/` directory contains MiniGo source files for manual compiler testing:

- `Tests/Valid/` — programs that should parse successfully
- `Tests/Invalid/` — programs that should produce errors

Manual test (powershell):
```powershell
dotnet run --project MiniGo.Compiler -- Tests/Valid/main.txt
dotnet run --project MiniGo.Compiler -- Tests/Invalid/syntax_err.txt
```

When a test project is added, use **MSTest** + **FluentAssertions** + **Moq** (per the installed `dotnet-best-practices` skill). Expected commands:
```powershell
dotnet test                              # run all tests
dotnet test --filter "FullyQualifiedName~TypeChecker"   # run a single test class
```

## Code Style Guidelines

### Namespaces & Usings

- **File-scoped namespaces** everywhere: `namespace MiniGo.Compiler.Errors;`
- Namespace pattern: `MiniGo.Compiler.{Feature}` or `MiniGo.Compiler.{Feature}.{SubFeature}`
- IDE project uses flat `namespace IDE;`
- Imports order: externals first (`Antlr4.Runtime`, `Generated`), then project-internal namespaces
- `System` usings come first when present (IDE project)

### Formatting

- **Tabs for indentation** (not spaces)
- Opening braces on same line for methods/properties (Allman-like for types)
- No trailing whitespace
- Files saved as UTF-8 with BOM (`﻿`) — Rider default for .cs files

### Types & Members

- Prefer **primary constructors** (C# 12): `public class Foo(string bar, int baz)`
- Mark concrete classes `sealed` where inheritance isn't intended
- Use `readonly struct` for value types (`SourceSpan`)
- Use `readonly` for injected/immutable fields (`private readonly ErrorCollector _collector`)
- Private fields: `_camelCase` prefix
- Public members: PascalCase
- Interfaces: `I` prefix (`ISymbol`, `IAntlrErrorListener`)
- Prefer auto-properties with `{ get; }` over fields when state tracking isn't needed
- Enum members: PascalCase, singular enum type names (`Severity`, `Types`)

### Nullability

- `<Nullable>enable</Nullable>` is on in both projects
- Use null-forgiving operator (`!`) sparingly; prefer explicit null checks
- Use `RecognitionException?` for nullable ANTLR parameters

### Documentation

- XML doc comments (`/// <summary>`) on all public types and methods
- Keep summaries concise: describe _what_, not _how_

### Error Handling

- **Do not throw exceptions for compilation errors** — use the `ErrorCollector` pattern:
  1. Create `ErrorCollector` with the file path
  2. Pass it to lexer/parser error listeners
  3. Errors accumulate during parsing
  4. Call `collector.Report(Console.Error)` and check `collector.HasErrors`
- `MiniGoErrorStrategy` handles ANTLR panic mode recovery with delimiter-based sync anchors
- For runtime errors (invalid arguments, file not found), exit with `Console.WriteLine` + return (no exceptions)

### Project-Specific Patterns

- **Generated code**: `MiniGo.Compiler/Generated/` is ANTLR-generated and git-ignored. **Never edit manually.**
- **Grammar**: `.g4` files live in `MiniGo.Compiler/Grammar/`. Regenerate via Rider's ANTLR plugin after grammar changes.
- **Visitor pattern**: All compilation phases extend `MiniGoParserBaseVisitor<object>` and override `Visit*` methods, calling `base.Visit*(context)` as default. See `TypeChecker` and `MiniGoEncoder` for examples.
- **Compiler phases are separate**: Parsing → AST → Semantic Analysis → Codegen. Respect this separation; don't mix phases.
- **Symbols**: `ISymbol` interface → `BaseSymbol` abstract class → concrete symbols. Hierarchy-aware with `GetLevel()` for scope tracking.

### Dependencies

- ANTLR 4 Runtime Standard 4.13.1
- LLVMSharp Interop 20.1.2
- AvalonEdit 6.3.1.120 (IDE only)
- Target framework: `net8.0` (compiler), `net8.0-windows` (IDE)
- SDK 8.0.0 with `rollForward: latestMajor`, `allowPrerelease: true`
