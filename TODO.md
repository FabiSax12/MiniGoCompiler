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
- [x] Assignment Statements — Simple `=` and compound `+=`, `-=`, `*=`, `/=`, `%=`, `&=`, `|=`, `^=`, `<<=`, `>>=`, `&^=` with type matching
- [x] If/Else Statements — Condition must be boolean; nested if/else-if/else supported
- [x] For Loops — Condition must be boolean; scope handling for init clause; while-style, classic for, infinite loop
- [x] Switch/Case/Default — Expression visiting, scope handling
- [x] Break/Continue — Loop/switch placement validation
- [x] Built-in Functions — `append` (requires slice/array), `len` (array/slice/string → int), `cap` (array/slice → int)
- [x] Print/Println — Argument expression visiting
- [x] Undefined Identifier Detection — Unknown identifiers reported as errors
- [x] Redeclaration Detection — Same-scope redeclaration of variables and functions
- [x] Nested Scopes — Blocks `{...}`, function bodies, for-loop init scopes, struct member scopes
- [x] Boolean Literals — `true`/`false` handled as special identifiers returning `Boolean` type
- [x] Return Value Type Checking — Single return value type checked; void returns detected
- [x] Struct Field Type Resolution — Selector expressions (`struct.field`) currently return `Unknown` instead of the actual field type declared in the struct definition
- [x] Function Call Argument Matching — Argument types are visited but not validated against declared parameter types and counts
- [x] Switch-Case Type Validation — Switch expression type vs case value types are not validated for consistency
- [x] Type Alias Declarations — `type MyInt int` is parsed by `singleTypeDecl` but not semantically checked
- [ ] Array Literal Type Inference — Array literals `[N]T{...}` not fully type-resolved
- [ ] Struct Literal Type Checking — `StructName{field: value}` field names and types not validated
- [x] Unused Variable Warning — No warnings emitted for declared-but-unused variables
- [x] Shadowed Declaration Warning — Inner scope shadowing outer variable not flagged
- [ ] Multiple Return Values — Function return count > 1 not yet supported

### Code Generation (LLVM — `MiniGoEncoder.cs`)

#### LLVM Infrastructure
- [ ] LLVM Module & Context Setup — `LLVMModuleCreateWithName`, target triple (`x86_64-pc-windows-msvc` or `x86_64-unknown-linux-gnu`)
- [ ] LLVM Builder Setup — `LLVMCreateBuilder` for IR generation
- [ ] Basic Block Management — Stack of `LLVMBasicBlockRef` for nested control flow (if/for/switch)
- [ ] Symbol → LLVM Value Mapping — `Dictionary<string, LLVMValueRef>` mapping MiniGo identifiers to `alloca` slots
- [ ] IR Verification — Call `LLVMVerifyModule` after generation
- [ ] IR Output — Write generated IR to `.ll` file via `LLVMPrintModuleToString`
- [ ] Object File Output — `LLVMTargetMachineEmitToFile` → `.o` or `.obj`
- [ ] Linking — Invoke system linker (`ld`, `link.exe`, or LLVM `lld`) to produce final executable

#### Values & Literals
- [ ] Integer Literals — `LLVMConstInt(LLVMInt32Type(), value, 0)` — sign-extended; handle `int` (i32) vs `int64` if MiniGo expands
- [ ] Float Literals — `LLVMConstReal(LLVMFloatType(), value)` — default float = f32; double = f64 if specified
- [ ] String Literals — Global `LLVMConstString` with null terminator, `LLVMAddGlobal`, `getelementptr` for `printf` format
- [ ] Rune Literals — `LLVMConstInt(LLVMInt32Type(), runeValue, 0)` — rune = int32 in Go
- [ ] Boolean Literals — `LLVMConstInt(LLVMInt1Type(), 1/0, 0)` — i1 type
- [ ] Identifier Load — `LLVMBuildLoad2` from `alloca` slot for variable references

#### Expressions
- [ ] Integer Arithmetic — `LLVMBuildAdd`, `LLVMBuildSub`, `LLVMBuildMul`, `LLVMBuildSDiv` (signed), `LLVMBuildSRem` — all with i32 type
- [ ] Float Arithmetic — `LLVMBuildFAdd`, `LLVMBuildFSub`, `LLVMBuildFMul`, `LLVMBuildFDiv`, `LLVMBuildFRem` — f32/f64 based on type
- [ ] Bitwise Operations — `LLVMBuildAnd`, `LLVMBuildOr`, `LLVMBuildXor`, `LLVMBuildShl`, `LLVMBuildLShr` (logical), `LLVMBuildAShr` (arithmetic) — i32/i64
- [ ] Bitwise AND NOT (`&^`) — `a &^ b` = `a & (^b)` = `LLVMBuildAnd(a, LLVMBuildNot(b))`
- [ ] Integer Comparison — `LLVMBuildICmp` with `LLVMIntEQ/NE/SGT/SGE/SLT/SLE` → i1
- [ ] Float Comparison — `LLVMBuildFCmp` with `LLVMRealOEQ/ONE/OGT/OGE/OLT/OLE` → i1
- [ ] Logical AND (`&&`) — Short-circuit: branch to evaluate RHS block only if LHS is true; phi merge result
- [ ] Logical OR (`||`) — Short-circuit: branch to evaluate RHS block only if LHS is false; phi merge result
- [ ] Logical NOT (`!`) — `LLVMBuildICmp(LLVMIntEQ, expr, LLVMConstInt(LLVMInt1Type(), 0, 0))`
- [ ] Unary Plus (`+`) — No-op, pass expression value through
- [ ] Unary Minus (`-`) — `LLVMBuildNeg` (int) or `LLVMBuildFNeg` (float)
- [ ] Bitwise NOT (`^`) — `LLVMBuildNot` (int) or `LLVMBuildXor(val, LLVMConstInt(LLVMInt32Type(), -1, 1))`
- [ ] Type Conversion — `LLVMBuildSIToFP` / `LLVMBuildFPToSI` / `LLVMBuildBitCast` as needed for mixed-type expressions

#### Statements & Control Flow
- [ ] Expression Statement — Evaluate expression, discard result (void context)
- [ ] Variable Assignment — `LLVMBuildStore` to variable's `alloca`
- [ ] Short Variable Decl (`:=`) — `LLVMBuildAlloca` + `LLVMBuildStore` in current function entry block
- [ ] Increment (`++`) — Load → `LLVMBuildAdd(1)` → Store
- [ ] Decrement (`--`) — Load → `LLVMBuildSub(1)` → Store
- [ ] Compound Assignment — Load → apply operation (Add/Sub/Mul/Div/And/Or/Xor/Shl/LShr) → Store
- [ ] Return (void) — `LLVMBuildRetVoid`
- [ ] Return (value) — `LLVMBuildRet(value)`
- [ ] Break — `LLVMBuildBr(loopExitBlock)` — needs loop block stack for context
- [ ] Continue — `LLVMBuildBr(loopConditionBlock)` — needs loop block stack for context
- [ ] Block Statement — Scope in symbols table; no LLVM-specific handling needed

#### If / Else
- [ ] If (no else) — `then_block` + `merge_block`; condition → `LLVMBuildCondBr` → `then_block` or `merge_block`
- [ ] If-Else — `then_block` + `else_block` + `merge_block`; condition → `LLVMBuildCondBr` → `then_block` or `else_block`; both then and else branch to `merge_block`
- [ ] If-Else-If Chain — Each `else if` generates its own condition block as the else branch, chaining merge blocks
- [ ] If with Init Statement — Scope + init codegen before condition; close scope at merge block

#### For Loops
- [ ] For (condition only / while-style) — `cond_block` (evaluate condition, br to body or exit), `body_block`, `exit_block`
- [ ] For (init; condition; post) — `init_block` (scope open), `cond_block`, `body_block`, `post_block` (br back to cond), `exit_block` (scope close)
- [ ] For (infinite) — `body_block` (br to itself), `exit_block`; break jumps to exit
- [ ] Loop Block Stack — Push/pop `{condBlock, bodyBlock, exitBlock}` to handle nested break/continue

#### Switch
- [ ] Switch Expression — Evaluate switch expression once, store in temp `alloca`
- [ ] Case Blocks — Per-case `case_block` + `next_case_block`; compare switch value against case literal via `LLVMBuildICmp(LLVMIntEQ)` → `LLVMBuildCondBr`
- [ ] Default Case — Final fallthrough block if all cases fail
- [ ] Break from Switch — Branch to `switch_exit_block` (same mechanism as loop break, stack-based)

#### Functions
- [ ] Function Declaration — `LLVMAddFunction(module, name, LLVMFunctionType(returnType, paramTypes, isVarArg))`
- [ ] Function Parameters — `LLVMGetParam(func, i)` → `LLVMBuildAlloca` + `LLVMBuildStore` for each param
- [ ] Function Entry Block — `LLVMAppendBasicBlock(func, "entry")`, position builder at entry
- [ ] Function Return Type — Void return → `LLVMVoidType()`; single return → typed LLVM type; multiple return → `LLVMStructType` of return types
- [ ] Function Calls — `LLVMBuildCall2(builder, funcType, func, args, nArgs, "")`
- [ ] Recursive Functions — Already works via `LLVMAddFunction` before body codegen
- [ ] Main Function — Declare `main` returning `i32` with `ret i32 0` at end

#### Built-in Functions
- [ ] `println(...)` — Declare `@printf(i8*, ...)`; for each argument, generate format specifier (`%d` for int, `%f` for float, `%s` for string), concatenate format string, call `printf`; append `\n` to format string
- [ ] `print(...)` — Same as `println` but without trailing `\n`
- [ ] `len(x)` — For arrays: return length field from array struct; for slices: return length field; for strings: call `strlen` or store string as `{i8*, i64}` and return length
- [ ] `cap(x)` — Return capacity field from slice struct
- [ ] `append(slice, elem)` — Allocate new backing array (capacity * 2 if full), `memcpy` old elements, store new element at end, return new `{ptr, len+1, newCap}` slice header

#### Composite Types — Structs
- [ ] Struct Type Definition — `LLVMStructCreateNamed` + `LLVMStructSetBody` with field types
- [ ] Struct Field Access — `LLVMBuildStructGEP2` for field offset, `LLVMBuildLoad2` for value
- [ ] Struct Field Assignment — `LLVMBuildStructGEP2` for field offset, `LLVMBuildStore` for new value
- [ ] Struct Value Construction — `LLVMBuildAlloca` for struct type, GEP + Store each field
- [ ] Struct Pass-by-Value — Store whole struct via `LLVMBuildStore`, load via `LLVMBuildLoad2`
- [ ] Struct Return — Return LLVM struct value; caller extracts fields via `LLVMBuildExtractValue`

#### Composite Types — Arrays
- [ ] Array Type — `LLVMArrayType(elementType, length)` for fixed-size arrays
- [ ] Array Alloca — `LLVMBuildAlloca(arrayType, "arr")`
- [ ] Array Index Access — `LLVMBuildGEP2(builder, arrayType, ptr, [zero, index], "idxptr")` → `LLVMBuildLoad2`
- [ ] Array Index Assignment — GEP + Store
- [ ] Array Literal Construction — Alloca + GEP + Store for each element
- [ ] Array as Function Param — Pass by pointer (decay to slice or pointer semantics)
- [ ] Bounds Checking — Optional: compare index against length before GEP, branch to panic block if out of bounds

#### Composite Types — Slices
- [ ] Slice Struct Representation — `{ i8*, i64, i64 }` = `{ pointer to backing array, length, capacity }` (type-erased pointer via `bitcast`)
- [ ] Slice Alloca — `LLVMBuildAlloca(sliceType, "slice")` → allocate backing array + set len + set cap
- [ ] Slice Literal Construction — Allocate backing array on heap (via `malloc` or global), set len = cap = N
- [ ] Slice Index Access — GEP into backing array (pointer field), `LLVMBuildGEP2` + `LLVMBuildLoad2`
- [ ] Slice Slicing (`a[low:high]`) — Compute new pointer (base + low), new len (high - low), new cap (cap - low), construct new slice header
- [ ] Nil Slice — `{ null, 0, 0 }` constant

### Compiler Pipeline (Program.cs)
- [x] Single-File Compilation — `args[0]` → lex → parse → typecheck

[//]: # (- [ ] Multi-File Compilation — Parse and type-check multiple `.txt` files as a single package)
- [ ] Codegen Phase Wiring — Instantiate `MiniGoEncoder`, call `encoder.Visit(tree)`, write IR output
- [ ] Pipeline Flags — `--emit-llvm`, `--emit-obj`, `--output <path>`, `--optimize`, `--target <triple>`
- [ ] LLVM Optimization Passes — `LLVMCreatePassManager` → run `-O1`/`-O2`/`-O3` passes on module
- [ ] Compilation Timing — Print elapsed time per phase (lex/parse/typecheck/codegen)
- [ ] Error Exit Codes — `Environment.Exit(1)` on compilation failure

### Error Reporting & UX
- [x] `ErrorCollector` — Error accumulation, ANSI-colored output (red = error, yellow = warning)
- [x] `SourceSpan` — File path, 1-based line, 0-based column, length
- [x] `Severity` — `Error` and `Warning` enums
- [ ] Warnings — Emit warnings for: unused variables, shadowed declarations, unreachable code, division by zero literal
- [ ] Source Context in Errors — Print the source line with `^^^^` underline pointing to the error span
- [ ] Error Limit — Stop after N errors (e.g., 50) to avoid error flood
- [ ] JSON Error Output — `--error-format json` for IDE/tool integration

### Testing
- [x] xUnit Project Setup — Create test project with MSTest + FluentAssertions + Moq (`dotnet new mstest`)
- [x] Lexer Unit Tests — Token recognition: each keyword, operator, literal type, identifier patterns
- [ ] Lexer Error Tests — Invalid characters, unterminated strings/comments
- [?] Parser Unit Tests — Each grammar rule: parse tree structure verification
- [ ] Parser Error Tests — Syntax error positions, error recovery correctness
- [ ] Parser Valid Test Files — Run all files in `Tests/Valid/` and assert zero errors
- [ ] Parser Invalid Test Files — Run all files in `Tests/Invalid/` and assert expected error count
- [?] TypeChecker Unit Tests — Each type rule: valid cases produce no errors, invalid cases produce expected errors
- [ ] TypeChecker Edge Cases — Nested scopes, shadowed variables, recursive functions, mutually recursive types
- [x] Symbol Table Tests — Define/Lookup/LookupCurrent/scope isolation
- [ ] Codegen Unit Tests — Verify LLVM IR output for each construct (`FileCheck`-style or string matching)
- [?] Integration Tests — End-to-end: MiniGo source → compile → execute → assert stdout matches expected
- [ ] Regression Test Suite — All `Tests/` directory files as automated regression tests
- [ ] CI/CD Integration — GitHub Actions workflow: build + test on push/PR

---

## Code Editor (IDE)

### Editor Core
- [ ] MiniGo Syntax Highlighting — Define `IHighlightingDefinition` for AvalonEdit:
  - Keywords (blue): `package`, `var`, `type`, `func`, `struct`, `if`, `else`, `for`, `switch`, `case`, `default`, `return`, `break`, `continue`, `append`, `len`, `cap`, `print`, `println`
  - Types (teal): `string`, `int`, `float`, `bool`, `rune`, `void` + user-defined type names (requires semantic info)
  - String literals (orange): interpreted `"..."` and raw `` `...` ``
  - Rune literals (orange): `'...'`
  - Numeric literals (light green): integers and floats
  - Comments (dark green): `// line` and `/* block */`
  - Operators and punctuation (light gray): `:=`, `==`, `!=`, `<=`, `>=`, `+`, `-`...
  - Boolean literals (blue): `true`, `false`
  - Function names (yellow): user-defined identifiers in call position
- [ ] Dark Theme Palette — Background `#1E1E1E`, foreground `#D4D4D4`, line numbers `#858585`, selection `#264F78`
- [ ] Light Theme Palette — Optional alternate white-background theme
- [ ] Bracket Matching — Highlight matching `{`/`}`, `(`/`)`, `[`/`]` when cursor is adjacent
- [ ] Auto-Indentation — After pressing Enter:
  - Increase indent after `{`
  - Increase indent after `if`, `else`, `for`, `switch`, `case`, `default` (lines ending without `{`)
  - Decrease indent on `}` or `)`
  - Preserve indent level for wrapped lines
- [ ] Code Folding — Collapsible regions for `{...}` blocks, `func` bodies, `struct` bodies, `var (...)` groups, `type (...)` groups
- [ ] Word Wrap Toggle — Toggle between no-wrap and character-wrap modes
- [ ] Font Size Controls — Ctrl+MouseWheel zoom in/out, Ctrl+Plus / Ctrl+Minus, Ctrl+0 reset to default
- [ ] Cursor Position Display — `Ln X, Col Y` in the status bar, updating on cursor move
- [ ] Current Line Highlight — Subtle background highlight on the line containing the cursor
- [ ] Whitespace Rendering — Toggle to show dots for spaces and arrows for tabs
- [ ] Line Numbers — Already present; make toggleable via View menu
- [ ] Gutter Icons — Error/warning icons in the gutter margin next to problematic lines

### File Management
- [ ] Save File (Ctrl+S) — Write editor `Text` content to the current file path; update title bar dirty indicator
- [ ] Save As (Ctrl+Shift+S) — `SaveFileDialog` → write to new path → update file tree
- [ ] Save All (Ctrl+Shift+S) — Save all open tabs with unsaved changes
- [ ] New File (Ctrl+N) — Create new untitled tab with empty content; prompt for save on close
- [ ] Open File (Ctrl+O) — `OpenFileDialog` for `.txt` / `.g` files
- [ ] Close File (Ctrl+W) — Close current tab; prompt "Save changes?" if dirty
- [ ] Multi-Tab Editing — Replace single `TextEditor` with `TabControl`; each tab = one open file
- [ ] Tab Header — File name + `*` dirty indicator + close button (×)
- [ ] Tab Reordering — Drag-and-drop tab reordering
- [ ] Unsaved Changes Dialog — "You have unsaved changes. Save / Don't Save / Cancel" on close
- [ ] Recent Files List — Maintain `RecentFiles.json` in `%APPDATA%`; show in File menu (max 10)
- [ ] File Watcher — `FileSystemWatcher` on open files: detect external modifications, prompt "Reload / Keep / Ignore"
- [ ] Drag & Drop Files — Accept file drops from Windows Explorer onto editor area → open file
- [ ] File Encoding — Detect and preserve file encoding (UTF-8 with/without BOM, UTF-16)

### Project & Build
- [ ] New Project — Create folder with `main.txt` template, optionally a `mini-go.project.json` manifest
- [ ] Open Project (Ctrl+Shift+O) — `OpenFolderDialog` → populate File Explorer tree → set workspace root
- [ ] Project Explorer Context Menus:
  - Right-click file: Open, Rename (F2), Delete (Del), Copy Path, Copy Relative Path
  - Right-click folder: New File, New Folder, Rename, Delete, Collapse All
  - Right-click blank area: New File, New Folder, Refresh, Open in File Explorer
- [ ] File Explorer Icons — Differentiate files vs folders, show MiniGo `.txt` files with a custom icon
- [ ] File Explorer Sorting — Folders first, then alphabetical
- [ ] File Explorer Filter — `*.txt`, `*.g` files (option to show all files)
- [ ] Build Button — Toolbar button "Build" (F6) → run full compiler pipeline on current project
- [ ] Run Button — Toolbar button "Run" (F5) → compile + execute + capture output
- [ ] Build Output Panel — Dockable bottom panel with:
  - Build progress (lexing → parsing → type checking → codegen → linking)
  - Compiler errors/warnings in a clickable list
  - LLVM IR output text (when `--emit-llvm`)
  - Raw stdout/stderr from compiled program execution
- [ ] Error List Panel — Dockable grid: severity icon, file, line, column, message; click to navigate; F8 / Shift+F8 for next/previous
- [ ] Build Configuration Dropdown — Debug (no optimizations, debug symbols) / Release (LLVM -O2)
- [ ] Output Directory Configuration — Project settings: output `.exe` path, intermediate `.ll`/`.bc` paths
- [ ] Multi-File Project Compilation — Discover all `.txt` files in project, compile as a single package

### Code Intelligence
- [ ] Live Error Checking — Already implemented (500ms debounced); add throttling for large files
- [ ] Error Underline Tooltip — Hover over red wavy underline → show error message in popup
- [ ] Error Navigation — Ctrl+Shift+F8 to jump to next error without touching mouse
- [ ] Go to Definition (F12) — Cursor on identifier → find declaration location via symbol table → navigate
- [ ] Go to Symbol (Ctrl+T) — Searchable popup listing all functions, variables, types in current file
- [ ] Find All References (Shift+F12) — List all usages of identifier under cursor in Find Results panel
- [ ] Highlight References — When cursor is on an identifier, highlight all usages in the current file
- [ ] Basic Autocomplete (Ctrl+Space) — Completion window showing:
  - Keywords (`var`, `func`, `if`, `for`, `switch`, `return`...)
  - Types (`int`, `string`, `bool`, `float`, `rune`)
  - Variables and functions in scope (from symbol table)
  - Struct field names after `.` on a struct-typed variable
- [ ] Parameter Info — When typing `(` after a function name, show tooltip: `funcName(param1 int, param2 string) bool`
- [ ] Quick Info (Hover) — Hover over identifier → tooltip: type, declaration location, doc comment
- [ ] Rename Symbol (F2) — Rename identifier across all references in scope; preview changes before applying
- [ ] Format Document (Ctrl+K, Ctrl+D) — Auto-format indentation, spacing, line breaks according to MiniGo style
- [ ] Comment/Uncomment (Ctrl+K, Ctrl+C / Ctrl+U) — Toggle `//` on selected lines
- [ ] Brace Completion — Auto-insert closing `}`, `)`, `]`, `"`, `` ` `` after typing opening one
- [ ] Surround With — Wrap selection in `{...}`, `(...)`, `"..."`

### Search & Navigation
- [ ] Find (Ctrl+F) — Inline find bar at editor top: search text, match count, case-sensitive, whole-word, regex
- [ ] Find in Files (Ctrl+Shift+F) — Search across all project files; results in Find Results panel
- [ ] Replace (Ctrl+H) — Inline replace bar: replace text, replace one, replace all
- [ ] Replace in Files (Ctrl+Shift+H) — Replace across all project files with preview
- [ ] Go to Line (Ctrl+G) — Dialog: enter line number → jump
- [ ] Navigate Forward/Backward — Ctrl+- (back), Ctrl+Shift+- (forward) through cursor position history
- [ ] Document Outline Panel — Sidebar tree view: `func name()`, `type Name struct`, `var name` in current file; click to jump
- [ ] Breadcrumb Bar — Above editor: `project > file > func > block` navigation

### UI & UX
- [ ] Status Bar Enhancements — Show: file encoding (UTF-8), line ending (CRLF/LF), language mode (MiniGo), cursor Ln/Col, current zoom level
- [ ] Dark/Light Theme Toggle — View menu option; persists to `%APPDATA%` settings file
- [ ] Customizable Editor Colors — Settings dialog for: background, foreground, line numbers, selection, current line, error underline
- [ ] Font Configuration — Settings for: font family, font size, line height
- [ ] Split Editor — View > Split Horizontally / Vertically; two editor panes viewing same or different files
- [ ] Minimap — AvalonEdit built-in scrollbar minimap showing zoomed-out code overview
- [ ] Full Screen Mode — F11 to toggle; hides title bar and menu
- [ ] Keyboard Shortcuts Reference — Help > Keyboard Shortcuts; searchable grid of all bindings
- [ ] Keyboard Shortcut Customization — Settings dialog: remap any command to custom keybinding
- [ ] Toolbar — Quick-access buttons: New, Open, Save, Undo, Redo, Build, Run, Debug target dropdown
- [ ] Context Menu in Editor — Cut, Copy, Paste, Go to Definition, Find All References, Rename, Format Document
- [ ] Zoom Persistence — Save and restore zoom level per session in settings

### Console & Output
- [ ] Integrated Terminal Panel — Bottom dockable terminal running `cmd.exe` or `pwsh.exe` in the project directory
- [ ] Compiler Output Panel — Shows raw stdout/stderr from `dotnet run --project MiniGo.Compiler`
- [ ] Program Output Panel — Shows stdout/stderr of the compiled and executed MiniGo program
- [ ] Output Panel Auto-Show — Auto-open output panel on build/run; auto-clear on new build
- [ ] Colorized Output — Parse ANSI color codes from compiler output and render in output panel

### Debugging (Future)
- [ ] Debug Adapter Protocol (DAP) — Implement DAP server for step-through debugging
- [ ] Breakpoints — Gutter click to set/unset breakpoints
- [ ] Variable Watch — Display variable values at breakpoints
- [ ] Call Stack Panel — Show function call chain at breakpoints
- [ ] Step Over / Step Into / Step Out — Debug toolbar buttons + F10/F11/Shift+F11 shortcuts

### REPL (Optional)
- [ ] Interactive REPL Panel — Type MiniGo expressions/statements → evaluate immediately → show result
- [ ] REPL History — Up/Down arrow through command history
- [ ] REPL Multi-line — Shift+Enter for multi-line input (functions, blocks)

### Package & Distribution
- [ ] Application Icon — Custom `.ico` for window title bar and taskbar
- [ ] About Dialog — Help > About: version, build date, GitHub link, credits, license (MIT)
- [ ] Publish Configuration — `dotnet publish -c Release --self-contained -r win-x64` for single-file executable
- [ ] Cross-Platform Build — `win-x64`, `linux-x64`, `osx-x64` RIDs in publish profiles
- [ ] Auto-Update — Check GitHub Releases for new version; notify on startup (optional)
- [ ] Installer — WiX Toolset or `dotnet publish` zip distribution
- [ ] File Association — `.migo` extension associated with IDE on Windows (registry)
- [ ] Command-Line Launch — `IDE.exe path/to/file.txt` opens file on launch; `IDE.exe path/to/project/` opens project
- [ ] Splash Screen — Quick splash window on startup while loading assemblies
- [ ] First-Run Experience — "Welcome" dialog: Open Project / New Project / Open Sample
