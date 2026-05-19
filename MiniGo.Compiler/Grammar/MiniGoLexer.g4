/*
 * =========================================================
 * LEXER RULES
 * =========================================================
 */

lexer grammar MiniGoLexer;

/*
 * KEYWORDS
 */

PACKAGE     : 'package';
VAR         : 'var';
TYPE        : 'type';
FUNC        : 'func';
STRUCT      : 'struct';

IF          : 'if';
ELSE        : 'else';

FOR         : 'for';

SWITCH      : 'switch';
CASE        : 'case';
DEFAULT     : 'default';

RETURN      : 'return';
BREAK       : 'break';
CONTINUE    : 'continue';

APPEND      : 'append';
LEN         : 'len';
CAP         : 'cap';

PRINT       : 'print';
PRINTLN     : 'println';

/*
 * OPERATORS
 */

PLUS                : '+';
MINUS               : '-';
STAR                : '*';
DIV                 : '/';
MOD                 : '%';

AMP                 : '&';
PIPE                : '|';
CARET               : '^';

LSHIFT              : '<<';
RSHIFT              : '>>';

BIT_CLEAR           : '&^';

LOGICAL_AND         : '&&';
LOGICAL_OR          : '||';

NOT                 : '!';

EQUALS              : '==';
NOT_EQUALS          : '!=';

LESS                : '<';
LESS_EQUALS         : '<=';

GREATER             : '>';
GREATER_EQUALS      : '>=';

ASSIGN              : '=';

DECLARE_ASSIGN      : ':=';

PLUS_ASSIGN         : '+=';
MINUS_ASSIGN        : '-=';
STAR_ASSIGN         : '*=';
DIV_ASSIGN          : '/=';
MOD_ASSIGN          : '%=';

AMP_ASSIGN          : '&=';
PIPE_ASSIGN         : '|=';
CARET_ASSIGN        : '^=';

LSHIFT_ASSIGN       : '<<=';
RSHIFT_ASSIGN       : '>>=';

BIT_CLEAR_ASSIGN    : '&^=';

INCREMENT           : '++';
DECREMENT           : '--';

/*
 * SEPARATORS
 */

LPAREN      : '(';
RPAREN      : ')';

LBRACE      : '{';
RBRACE      : '}';

LBRACKET    : '[';
RBRACKET    : ']';

COMMA       : ',';
DOT         : '.';
COLON       : ':';
SEMICOLON   : ';';

/*
 * LITERALS
 */

INTLITERAL
    : DECIMAL_LIT
    ;

FLOATLITERAL
    : DIGITS DOT DIGITS EXPONENT?
    | DIGITS EXPONENT
    ;

RUNELITERAL
    : '\'' ( ESCAPED_CHAR | ~['\\\r\n] ) '\''
    ;

RAWSTRINGLITERAL
    : '`' .*? '`'
    ;

INTERPRETEDSTRINGLITERAL
    : '"' ( ESCAPED_CHAR | ~["\\\r\n] )* '"'
    ;

/*
 * IDENTIFIERS
 */

IDENTIFIER
    : LETTER (LETTER | DIGIT)*
    ;

/*
 * FRAGMENTS
 */

fragment LETTER
    : [a-zA-Z_]
    ;

fragment DIGIT
    : [0-9]
    ;

fragment DIGITS
    : DIGIT+
    ;

fragment DECIMAL_LIT
    : '0'
    | [1-9] DIGIT*
    ;

fragment EXPONENT
    : [eE] [+-]? DIGITS
    ;

fragment ESCAPED_CHAR
    : '\\' [btnfr"'\\]
    ;

/*
 * COMMENTS & WHITESPACE
 */

LINE_COMMENT
    : '//' ~[\r\n]* -> skip
    ;

BLOCK_COMMENT
    : '/*' .*? '*/' -> skip
    ;

WS
    : [ \t\r\n]+ -> skip
    ;