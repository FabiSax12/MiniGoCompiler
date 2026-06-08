/*
 * ============================================================================
 * Faltantes — Elementos del grammar/proyecto no funcionales en Happy Path
 * ============================================================================
 *
 * Este documento lista todos los tokens y rules del grammar
 * (MiniGoLexer.g4 + MiniGoParser.g4) y requerimientos del enunciado
 * que el parser/typechecker aceptan correctamente pero cuyo codegen
 * (MiniGoEncoder) falla, impidiendo la compilacion completa.
 *
 * Cada item incluye:
 *   - El token o rule afectado
 *   - El error producido
 *   - Referencia al archivo de grammar
 * ============================================================================
 */

## 1. Tokens del Lexer no funcionales en codegen

### 1.1 CONTINUE — ✅ RESUELTO
- **Token**: `CONTINUE : 'continue';`
- **Archivo**: `MiniGoLexer.g4:30`
- **Rule asociada**: `statement : ... | CONTINUE SEMICOLON`
- **Fix**: En `VisitLoop`, se creo un `postBlock` dedicado cuando `hasPost == true`. El `contBlock` del `_loopStack` ahora apunta a `postBlock` (no a `condBlock`) para que `continue` ejecute el post-statement (ej. `i++`) antes de volver a evaluar la condicion. El fallthrough del body tambien fue corregido para saltar siempre al `contBlock` (en vez de tener el post inline). Verificado con `Tests/E2E/continue_bare_switch.txt`.

### 1.2 APPEND
- **Token**: `APPEND : 'append';`
- **Archivo**: `MiniGoLexer.g4:32`
- **Rule asociada**: `appendExpression : APPEND LPAREN expression COMMA expression RPAREN`
- **Error**: `System.NullReferenceException` en `MiniGoEncoder.VisitExpr` (line 245)
- **Detalle**: El encoder no implementa la visita de `appendExpression`; al intentar evaluar la expresion `append(s, valor)` retorna null y casca tanto en asignacion (`s = append(s, x)`) como en paso a funcion (`println(append(s, x))`).

### 1.3 CAP
- **Token**: `CAP : 'cap';`
- **Archivo**: `MiniGoLexer.g4:34`
- **Rule asociada**: `capExpression : CAP LPAREN expression RPAREN`
- **Error**: `System.NullReferenceException` en `MiniGoEncoder.VisitExpr` (line 245)
- **Detalle**: Identico a APPEND — el encoder no implementa `VisitCapExpression`, retorna null y produce NRE. Tanto LEN como CAP son builtins reconocidos por el parser, pero solo LEN tiene implementacion en el encoder.

## 2. Rules del Parser no funcionales en codegen

### 2.1 switch — Forma 3 (bare switch con init) — ✅ RESUELTO
- **Rule**: `switch : SWITCH simpleStatement SEMICOLON LBRACE expressionCaseClauseList RBRACE`
- **Archivo**: `MiniGoParser.g4:206`
- **Fix**: En `VisitSwitch`, se detecta `isBareSwitch = context.expression() == null` y se delega a `EmitBareSwitch`, que genera una cadena if/else evaluando cada case expression en runtime. El `default` se emite al final si ninguna condicion matcheo. Verificado con `Tests/E2E/continue_bare_switch.txt`.

### 2.2 switch — Forma 4 (bare switch sin init ni expr) — ✅ RESUELTO
- **Rule**: `switch : SWITCH LBRACE expressionCaseClauseList RBRACE`
- **Archivo**: `MiniGoParser.g4:207`
- **Fix**: Mismo `EmitBareSwitch` que Forma 3 — la deteccion `context.expression() == null` cubre ambas formas. Verificado con `Tests/E2E/continue_bare_switch.txt`.

### 2.3 structDeclType — Structs pasados por valor — ✅ RESUELTO
- **Rule**: `funcFrontDecl : FUNC IDENTIFIER LPAREN (funcArgDecls|ε) RPAREN (declType|ε)`
- **Archivo**: `MiniGoParser.g4:44`
- **Fix (dos cambios en VisitFuncDecl y VisitSingleTypeDecl)**:
  1. `VisitFuncDecl`: parametros y retorno usaban `LlvmType(TypeResolver.Resolve(...))` que cae a `i32` para structs. Cambiado a `LlvmTypeFromDecl(...)` que resuelve correctamente structs, arrays y aliases.
  2. `VisitSingleTypeDecl`: se usaba `LLVMTypeRef.CreateStruct(fieldTypes)` (tipo anonimo). LLVM deduplica tipos anonimos con igual layout, causando colision en `_structTypeToName` cuando dos structs tienen los mismos campos (ej. `Point{x,y int}` y `Rect{w,h int}` son ambos `{i32,i32}`). Cambiado a `CreateNamedStruct(aliasName)` + `StructSetBody` para garantizar identidad unica por nombre.
- Verificado con `Tests/E2E/structs_by_value.txt` (incluye dos structs de igual layout).

### 2.4 simpleStatement — CONTINUE — ✅ RESUELTO
- **Rule**: `statement : ... | CONTINUE SEMICOLON`
- **Archivo**: `MiniGoParser.g4:155`
- **Fix**: Ver seccion 1.1. Resuelto con `postBlock` dedicado en `VisitLoop`.

### 2.5 primaryExpression — appendExpression no implementado
- **Rule**: `primaryExpression : ... | appendExpression`
- **Archivo**: `MiniGoParser.g4:107`
- **Error**: Ver seccion 1.2 (APPEND)

### 2.6 primaryExpression — capExpression no implementado
- **Rule**: `primaryExpression : ... | capExpression`
- **Archivo**: `MiniGoParser.g4:109`
- **Error**: Ver seccion 1.3 (CAP)

## 3. Requerimientos del Enunciado no cubiertos

Segun el documento "Requerimientos Generales" y "Elementos a implementar para generacion de codigo":

### 3.1 CONTINUE
- **Enunciado**: "Instrucciones de control de flujo solamente IFs y LOOPs, sin uso de 'break' ni 'continue'."
- **Estado**: El enunciado EXPLICITAMENTE excluye `continue` y `break`. Sin embargo, el grammar define ambos tokens y rules. `break` SI funciona en el encoder; `continue` NO. Dado que el enunciado los excluye, esto es aceptable pero inconsistente (break si funciona, continue no).

### 3.2 APPEND
- **Enunciado**: "Para los tipos slice, deben considerarse las funciones preestablecidas append, len y cap (para efectos de tipos de entrada y de retorno)"
- **Estado**: `len` funciona. `append` y `cap` NO. El typechecker si valida tipos de entrada/salida, pero el encoder no genera codigo.

### 3.3 CAP
- **Estado**: Ver 3.2. Mismo caso que APPEND.

### 3.4 Structs como parametros/retorno — ✅ RESUELTO
- **Enunciado**: "Existiran ademas estructuras tipo registros con el mismo formato de Golang. Tanto la definicion de la estructura como su posterior uso deben ser validados y verificados."
- **Estado**: Definicion, declaracion, acceso a campos, paso como argumento y retorno por valor funcionan. Ver seccion 2.3.

## 4. Resumen

| Elemento | Parser | TypeChecker | Encoder |
|---|---|---|---|
| `continue` | OK | OK | ✅ OK |
| `append()` | OK | OK | CRASH (NRE) |
| `cap()` | OK | OK | CRASH (NRE) |
| `switch` bare (formas 3, 4) | OK | OK | ✅ OK |
| Struct by-value param/return | OK | OK | ✅ OK |
| `break` | OK | OK | OK |
| `len()` | OK | OK | OK |
| `switch` con expr entera | OK | OK | OK |
| `if`/`for` todas las formas | OK | OK | OK |
| Resto de operadores/tokens | OK | OK | OK |

## 5. Archivos E2E Afectados

Los siguientes archivos de test evitan deliberadamente los elementos no funcionales
para mantenerse en Happy Path. Las limitaciones del encoder estan documentadas
en sus respectivos headers:

- `control_flow.txt` — omite `continue` y bare switch (formas 3/4) por limitacion historica; ambos ya funcionan
- `continue_bare_switch.txt` — test dedicado que cubre `continue` en while y for clasico, bare switch formas 3 y 4
- `functions_builtins.txt` — omite `append()` y `cap()`; structs por valor ya funcionan
- `structs_by_value.txt` — test dedicado que cubre structs como parametros y retorno, incluyendo dos structs con igual layout
- `declarations_types.txt` — usa `len(slice)` en lugar de `append` para ejercitar slice
- `operations.txt` — no afectado (no usa los elementos problematicos)
