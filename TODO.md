# TODO

## Compiler

### Lexer & Parser Frontend
- [x] Grammar Tokens — `MiniGoLexer.g4`: keywords, operators, separators, literals, identifiers, comments/whitespace
- [x] Grammar Rules — `MiniGoParser.g4`: packages, var/type/func decls, expressions (binary/unary/primary), statements (if/for/switch/return/break/continue), short var decl (`:=`), compound assignments
- [x] Generated ANTLR4 — Run Rider ANTLR v4 plugin → generate `MiniGoLexer.cs`, `MiniGoParser.cs`, `MiniGoParserBaseVisitor.cs`, `MiniGoParserVisitor.cs` into `Generated/`
- [x] Lexer — Wired in `Program.cs`: `MiniGoLexer(ICharStream)` + `LexerErrorListener`
- [x] Parser — Wired in `Program.cs`: `MiniGoParser(CommonTokenStream)` + `ParserErrorListener` + `MiniGoErrorStrategy`
- [x] Lexer Errors — `LexerErrorListener : IAntlrErrorListener<int>` → `ErrorCollector`
- [x] Parser Errors — `ParserErrorListener : IAntlrErrorListener<IToken>` → `ErrorCollector`
- [x] Panic Mode Recovery — `MiniGoErrorStrategy`: single-token deletion + delimiter-based sync anchors (`;`, `{`, `}`, keywords, max 50 tokens)

### Semantic Analysis
- [x] Symbol Table — `SymbolsTable`: stack-based scopes, `OpenScope`/`CloseScope`, `Define`, `Lookup` (walks scopes upward), `LookupCurrent`, `GetLevel`
- [x] Symbol Hierarchy — `ISymbol` → `BaseSymbol` → `VarSymbol` / `MethodSymbol`
- [x] Type System — `Types` enum: `String`, `Integer`, `Float`, `Boolean`, `Rune`, `Array`, `Slice`, `Struct`, `Void`, `Unknown`
- [x] Type Resolver — `TypeResolver`: maps `MiniGoParser` type contexts → `Types` enum (handles identifiers, slices, arrays, structs)
- [x] Variable Declarations — Single `var x int = expr` and grouped `var ( ... )` with type inference
- [x] Function Declarations — Parameter registration in scope, return type checking (void vs non-void, type mismatch), function registration in symbol table
- [x] Binary Expressions — Arithmetic (numeric), bitwise (integer), comparison (matching ordered types), logical (boolean)
- [x] Unary Expressions — `+`, `-`, `^` (numeric); `!` (boolean)
- [x] Short Variable Declarations (`:=`) — Redefinition detection, type inference from RHS
- [x] Assignment Statements — Simple `=` and compound `+=`, `-=`, `*=`, `/=`, `%=` with type matching
- [x] If/Else Statements — Condition must be boolean; nested if/else-if/else supported
- [x] For Loops — Condition must be boolean; scope handling for init clause; while-style, classic for, infinite loop
- [x] Switch/Case/Default — Expression visiting, scope handling
- [x] Break/Continue — Loop/switch placement validation
- [x] Built-in Functions — `len` (array/slice/string → int)
- [x] Print/Println — Argument expression visiting
- [x] Undefined Identifier Detection — Unknown identifiers reported as errors
- [x] Redeclaration Detection — Same-scope redeclaration of variables and functions
- [x] Nested Scopes — Blocks `{...}`, function bodies, for-loop init scopes
- [x] Boolean Literals — `true`/`false` handled as special identifiers returning `Boolean` type
- [x] Return Value Type Checking — Single return value type checked; void returns detected

### Code Generation (LLVM — `MiniGoEncoder.cs`)
- [x] LLVM Module & Builder Setup — `LLVMModuleRef`, `LLVMBuilderRef`, global context
- [x] Scope Stack — `Stack<Dictionary<string, (LLVMValueRef, LLVMTypeRef)>>` for locals and globals
- [x] IR Verification & Output — `TryVerify` → `PrintToString` → `.ll` file
- [x] Integer / Float / Bool / Rune / String literals
- [x] RawString literals — `BuildGlobalStringPtr` for backtick strings
- [x] Variable declarations — global (`AddGlobal`) and local (`BuildAlloca`) with zero-init
- [x] Integer array declarations — `[n x i32]`, GEP2 read/write by index
- [x] Arithmetic expressions — `+,-,*,/,%` for int and float
- [x] Comparison expressions — `==,!=,<,<=,>,>=` for int and float
- [x] Logical / unary operators — `&&,||,!,^,-,+`
- [x] Function declarations — parameters, entry block, implicit `BuildRetVoid`
- [x] Function calls — `BuildCall2` with `_functions`/`_functionTypes` dicts
- [x] `if` / `else` / `else if` — `then`, `else`, `merge` blocks; init statement support
- [x] `for` (4 variants) — infinite, while-style, classic, init+post; no break/continue
- [x] `println` / `print` — dynamic format string via `printf`; `BuildZExt` for bool
- [x] `len(arr)` — compile-time `i32` constant from `elemType.ArrayLength`
- [x] Wire encoder into pipeline — emit `.ll` alongside source file

### Compiler Pipeline
- [x] Single-File Compilation — `args[0]` → lex → parse → typecheck → codegen → `.ll` output
- [x] Error Reporting — `ErrorCollector` with `GetSortedErrors()`, `HasErrors`, `ErrorCount`

---

## IDE

### Editor Core
- [x] AvalonEdit editor with line numbers and dark theme
- [x] File Explorer — TreeView with lazy-load directory expansion
- [x] Live error highlighting — wavy red underlines via `ErrorHighlighter` (500ms debounce)
- [ ] MiniGo Syntax Highlighting — `MiniGoHighlighting.xshd` loaded in `MainWindow.xaml.cs` *(IDE-2)*
- [ ] Error List Panel — Dock-bottom `DataGrid`: Severity / Line / Col / Message; click navigates editor *(IDE-3)*
- [ ] Build Button (F6) — Invokes compiler CLI; shows output in output panel *(IDE-4)*
- [ ] Run Button (F5) — Executes generated `.ll` via `lli`; shows stdout/stderr in output panel *(IDE-4)*
- [ ] Output Panel — Read-only `TextBox` showing compiler and program output *(IDE-4)*

### Sample Files
- [ ] Write `.go` test files in `samples/` demonstrating: variables, arrays, functions, if/else, for loops, println, len
