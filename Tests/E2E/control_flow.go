/*
 * ============================================================================
 * E2E Test: Control Flow
 * ============================================================================
 *
 * Output Esperado:
 *   Compilacion exitosa. Sin errores sintacticos ni semanticos.
 *   Salida: imprime los resultados de cada rama de control de flujo.
 *
 * Cobertura de Tokens:
 *   IF, ELSE, FOR, SWITCH, CASE, DEFAULT, BREAK, RETURN,
 *   PRINT, PRINTLN
 *   CONTINUE
 *
 * Cobertura de Rules:
 *   ifStatement (6 formas):
 *     1. IF expression block
 *     2. IF expression block ELSE ifStatement
 *     3. IF expression block ELSE block
 *     4. IF simpleStatement ; expression block
 *     5. IF simpleStatement ; expression block ELSE ifStatement
 *     6. IF simpleStatement ; expression block ELSE block
 *
 *   loop (4 formas):
 *     1. FOR block
 *     2. FOR expression block
 *     3. FOR simpleStatement ; expression ; simpleStatement block
 *     4. FOR simpleStatement ; ; simpleStatement block
 *
 *   switch (4 formas):
 *     1. SWITCH simpleStatement ; expression { ... }
 *     2. SWITCH expression { ... }
 *     3. SWITCH simpleStatement ; { ... }  (bare switch con init)
 *     4. SWITCH { ... }                     (bare switch sin init ni expr)
 *
 *   statement (12 formas): PRINT, PRINTLN, RETURN (con/sin expr),
 *     BREAK, CONTINUE, simpleStatement, block, switch, ifStatement,
 *     loop, typeDecl, variableDecl
 *
 *   expressionCaseClauseList, expressionCaseClause,
 *   expressionSwitchCase (CASE, DEFAULT)
 *
 *   simpleStatement: epsilon (vacia), expression, assignment,
 *     expressionList := expressionList
 * ============================================================================
 */

package main;

// ============================================================
// typeDecl y variableDecl como statements dentro de bloque
// ============================================================
func testStmtTypeVar() {
    {
        type InnerInt int;
        var x InnerInt = 10;
        println(x);
    };
};

// ============================================================
// ifStatement: Forma 1 — IF expression block
// ============================================================
func ifForm1(x int) {
    println("=== IF Forma 1 ===");
    if x > 0 {
        println("positive");
    };
};

// ============================================================
// ifStatement: Forma 2 — IF exp block ELSE ifStatement (else if chain)
// ============================================================
func ifForm2(x int) {
    println("=== IF Forma 2 ===");
    if x > 10 {
        println("> 10");
    } else if x > 5 {
        println("> 5");
    } else if x > 0 {
        println("> 0");
    };
};

// ============================================================
// ifStatement: Forma 3 — IF exp block ELSE block
// ============================================================
func ifForm3(x int) {
    println("=== IF Forma 3 ===");
    if x >= 0 {
        println("non-negative");
    } else {
        println("negative");
    };
};

// ============================================================
// ifStatement: Forma 4 — IF simpleStatement ; exp block
// ============================================================
func ifForm4() {
    println("=== IF Forma 4 ===");
    if a := 42; a > 0 {
        println(a);
    };
};

// ============================================================
// ifStatement: Forma 5 — IF simpleStatement ; exp block ELSE ifStatement
// ============================================================
func ifForm5() {
    println("=== IF Forma 5 ===");
    if x := 15; x > 10 {
        println("x > 10");
    } else if x > 5 {
        println("x > 5");
    };
};

// ============================================================
// ifStatement: Forma 6 — IF simpleStatement ; exp block ELSE block
//   (ademas ejercita simpleStatement: epsilon en el init del IF)
// ============================================================
func ifForm6() {
    println("=== IF Forma 6 ===");
    if x := -5; x >= 0 {
        println("non-negative");
    } else {
        println("negative");
    };
};

// ============================================================
// ifStatement con simpleStatement epsilon (init vacio)
//   IF ; expression block
// ============================================================
func ifFormEpsilonInit() {
    println("=== IF Epsilon Init ===");
    if ; true {
        println("epsilon init ok");
    };
};

// ============================================================
// loop: Forma 1 — FOR block (infinite loop, requiere break)
// ============================================================
func loopForm1() {
    println("=== Loop Forma 1 ===");
    var count int = 0;
    for {
        count++;
        if count >= 3 {
            break;
        };
        println("infinite:", count);
    };
};

// ============================================================
// loop: Forma 2 — FOR expression block (while-style)
// ============================================================
func loopForm2() {
    println("=== Loop Forma 2 ===");
    var i int = 0;
    for i < 3 {
        println("while:", i);
        i++;
    };
};

// ============================================================
// loop: Forma 3 — FOR simpleStatement ; exp ; simpleStatement block
// ============================================================
func loopForm3() {
    println("=== Loop Forma 3 ===");
    for j := 0; j < 3; j++ {
        println("classic:", j);
    };
};

// ============================================================
// loop: Forma 4 — FOR simpleStatement ; ; simpleStatement block
// ============================================================
func loopForm4() {
    println("=== Loop Forma 4 ===");
    var k int = 0;
    for k = 0; ; k++ {
        if k >= 2 {
            break;
        };
        println("nocond:", k);
    };
};

// ============================================================
// loop con simpleStatement epsilon en init: FOR ; exp ; simpleStatement block
// ============================================================
func loopFormEpsilonInit() {
    println("=== Loop Epsilon Init ===");
    var m int = 0;
    for ; m < 2; m++ {
        println("epsilon init:", m);
    };
};

// ============================================================
// BREAK en loop
// ============================================================
func testBreak() {
    println("=== Break ===");
    var i int = 0;
    for i < 5 {
        i++;
        if i == 3 {
            break;
        };
        println(i);
    };
};

// ============================================================
// CONTINUE: todas las formas de loop
// ============================================================
func testContinue() {
    println("=== Continue ===");

    // Continue en for clasico — saltar pares
    for n := 1; n <= 5; n++ {
        if n % 2 == 0 {
            continue;
        };
        println("odd:", n);
    };

    // Continue en for estilo-while
    var j int = 0;
    for j < 5 {
        j++;
        if j == 3 {
            continue;
        };
        println("skip3:", j);
    };

    // Continue en for infinito
    var k int = 0;
    for {
        k++;
        if k > 5 {
            break;
        };
        if k == 3 {
            continue;
        };
        println("inf:", k);
    };
};

// ============================================================
// switch: Forma 1 — SWITCH simpleStatement ; exp { ... }
// ============================================================
func switchForm1() {
    println("=== Switch Forma 1 ===");
    switch x := 2; x {
        case 1:
            println("one");
        case 2:
            println("two");
        default:
            println("other");
    };
};

// ============================================================
// switch: Forma 2 — SWITCH exp { ... }
// ============================================================
func switchForm2(day int) {
    println("=== Switch Forma 2 ===");
    switch day {
        case 1:
            println("Monday");
        case 2:
            println("Tuesday");
        case 3:
            println("Wednesday");
        case 4, 5:
            println("Thurs/Fri");
        default:
            println("Weekend");
    };
};

// ============================================================
// switch con simpleStatement epsilon (init vacio)
//   SWITCH ; exp { ... }  —  Forma 1 con simpleStatement epsilon
// ============================================================
func switchEpsilonInit() {
    println("=== Switch Epsilon Init ===");
    switch ; 2 {
        case 1:
            println("nope");
        case 2:
            println("epsilon init ok");
        default:
            println("default");
    };
};

// ============================================================
// switch: Forma 3 — bare switch con init (SWITCH simpleStatement ; { ... })
// ============================================================
func switchForm3() {
    println("=== Switch Forma 3 ===");
    switch x := 15; {
        case x > 100:
            println("x > 100");
        case x > 10:
            println("x > 10");
        default:
            println("x <= 10");
    };
};

// ============================================================
// switch: Forma 4 — bare switch sin init ni expr (SWITCH { ... })
// ============================================================
func switchForm4(x int) {
    println("=== Switch Forma 4 ===");
    switch {
        case x > 0:
            println("positive");
        case x < 0:
            println("negative");
        default:
            println("zero");
    };
};

// ============================================================
// switch con expressionCaseClauseList vacio (epsilon)
// ============================================================
func switchEmpty() {
    println("=== Switch Vacio ===");
    switch 0 {
    };
};

// ============================================================
// RETURN: con y sin expresion
// ============================================================
func returnWithExpr(a int) int {
    if a > 0 {
        return a;
    };
    return 0;
};

func returnVoid() {
    println("=== Return Void ===");
    var x int = 5;
    if x > 0 {
        return;
    };
    println("not reached");
};

// ============================================================
// PRINT statement
// ============================================================
func testPrint() {
    println("=== Print ===");
    print("Hello");
    print(" ");
    print("World");
    print(42);
};

// ============================================================
// PRINTLN con y sin expressionList (epsilon)
// ============================================================
func testPrintln() {
    println("=== Println ===");
    println();
    println("line with arg");
    println(1, 2, 3);
};

// ============================================================
// block como statement (bloques anidados)
// ============================================================
func testNestedBlocks() {
    println("=== Bloques Anidados ===");
    {
        var a int = 10;
        {
            var b int = 20;
            println(a, b);
        };
    };
};

// ============================================================
// MAIN: ejecuta todas las pruebas
// ============================================================
func main() {
    testStmtTypeVar();

    ifForm1(5);
    ifForm1(-1);

    ifForm2(15);
    ifForm2(7);
    ifForm2(3);

    ifForm3(5);
    ifForm3(-5);

    ifForm4();
    ifForm5();
    ifForm6();
    ifFormEpsilonInit();

    loopForm1();
    loopForm2();
    loopForm3();
    loopForm4();
    loopFormEpsilonInit();

    testBreak();
    testContinue();

    switchForm1();
    switchForm2(1);
    switchForm2(3);
    switchForm2(4);
    switchForm2(7);
    switchEpsilonInit();
    switchForm3();
    switchForm4(5);
    switchForm4(-3);
    switchForm4(0);
    switchEmpty();

    var r int = returnWithExpr(10);
    println(r);

    returnVoid();

    testPrint();
    testPrintln();

    testNestedBlocks();

    println("=== Control Flow Complete ===");
};
