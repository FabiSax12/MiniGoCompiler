parser grammar MiniGoParser;

options {
    tokenVocab=MiniGoLexer;
}

root			            : PACKAGE  IDENTIFIER SEMICOLON topDeclarationList;


topDeclarationList	        : ( variableDecl | typeDecl | funcDecl)*;


variableDecl		        : VAR singleVarDecl SEMICOLON
			                | VAR  LPAREN innerVarDecls RPAREN SEMICOLON
			                | VAR  LPAREN RPAREN SEMICOLON
			                ;

			                
innerVarDecls		        : singleVarDecl SEMICOLON (singleVarDecl SEMICOLON)*;


singleVarDecl		        : identifierList declType ASSIGN expressionList
			                | identifierList ASSIGN expressionList
			                | singleVarDeclNoExps
			                ;

			                
singleVarDeclNoExps	        : identifierList declType;	
typeDecl		            : TYPE singleTypeDecl SEMICOLON
			                | TYPE LPAREN innerTypeDecls RPAREN SEMICOLON
			                | TYPE LPAREN RPAREN SEMICOLON
			                ;

			                
innerTypeDecls		        : singleTypeDecl SEMICOLON (singleTypeDecl SEMICOLON)*;


singleTypeDecl		        : IDENTIFIER declType;


funcDecl		            : funcFrontDecl block SEMICOLON;


funcFrontDecl		        : FUNC IDENTIFIER LPAREN (funcArgDecls|/*epsilon*/) RPAREN (declType|/*epsilon*/);


funcArgDecls		        : singleVarDeclNoExps (COMMA singleVarDeclNoExps)*;


declType		            : LPAREN declType RPAREN	
                            | IDENTIFIER
                            | sliceDeclType
                            | arrayDeclType
                            | structDeclType
                            ;

                            
sliceDeclType		        : LBRACKET RBRACKET declType;


arrayDeclType		        : LBRACKET INTLITERAL RBRACKET declType;


structDeclType		        : STRUCT LBRACE (structMemDecls|/*epsilon*/) RBRACE;


structMemDecls	            : singleVarDeclNoExps SEMICOLON (singleVarDeclNoExps SEMICOLON)*;


identifierList		        : IDENTIFIER (COMMA IDENTIFIER)*;


expression		            : primaryExpression
                            | expression STAR expression
                            | expression DIV expression
                            | expression MOD expression
                            | expression LSHIFT expression
                            | expression RSHIFT expression
                            | expression AMP expression
                            | expression BIT_CLEAR expression
                            | expression PLUS expression
                            | expression MINUS expression	
                            | expression PIPE expression
                            | expression CARET expression	
                            | expression EQUALS expression
                            | expression NOT_EQUALS expression
                            | expression LESS expression
                            | expression LESS_EQUALS expression
                            | expression GREATER expression
                            | expression GREATER_EQUALS expression
                            | expression LOGICAL_AND expression
                            | expression LOGICAL_OR expression
                            | PLUS expression 
                            | MINUS expression
                            | NOT expression 
                            | CARET expression
                            ;
                            
                            
expressionList		        : expression (COMMA expression)*;


primaryExpression	        : operand								
			                | primaryExpression selector 
			                | primaryExpression index 
			                | primaryExpression arguments 
			                | appendExpression 
			                | lengthExpression
			                | capExpression
			                ;

		                
operand		                : literal									
                            | IDENTIFIER 
                            | LPAREN expression RPAREN
                            ;
                            
                            
literal			            : INTLITERAL								 
                            | FLOATLITERAL							 
                            | RUNELITERAL							 
                            | RAWSTRINGLITERAL						 
                            | INTERPRETEDSTRINGLITERAL
                            ;
                            
                            					
index			            : LBRACKET expression RBRACKET;


arguments		            : LPAREN (expressionList | /*epsilon*/) RPAREN;


selector		            : DOT IDENTIFIER;


appendExpression	        : APPEND LPAREN expression COMMA expression RPAREN;

 
lengthExpression	        : LEN LPAREN expression RPAREN;


capExpression		        : CAP LPAREN expression RPAREN;


statementList 		        : statement* ;


block 			            : LBRACE statementList RBRACE;

 
statement		            : PRINT LPAREN (expressionList | /*epsilon*/) RPAREN SEMICOLON 
                            | PRINTLN LPAREN (expressionList | /*epsilon*/) RPAREN SEMICOLON 
                            | RETURN (expression | /*epsilon*/) SEMICOLON 
                            | BREAK SEMICOLON 
                            | CONTINUE SEMICOLON
                            | simpleStatement SEMICOLON 
                            | block SEMICOLON
                            | switch SEMICOLON
                            | ifStatement SEMICOLON
                            | loop SEMICOLON
                            | typeDecl
                            | variableDecl
                            ;
                            
                            
simpleStatement	            : /*epsilon*/ 
                            | expression (INCREMENT | DECREMENT | /*epsilon*/) 
                            | assignmentStatement 
                            | expressionList DECLARE_ASSIGN expressionList
                            ;
                            
                            
assignmentStatement 	    : expressionList ASSIGN expressionList 
                            |expression PLUS_ASSIGN expression
                            |expression AMP_ASSIGN expression 
                            |expression MINUS_ASSIGN expression
                            |expression PIPE_ASSIGN expression
                            |expression STAR_ASSIGN expression 
                            |expression CARET_ASSIGN expression 
                            |expression LSHIFT_ASSIGN expression 
                            |expression RSHIFT_ASSIGN expression 
                            |expression BIT_CLEAR_ASSIGN expression
                            |expression MOD_ASSIGN expression
                            |expression DIV_ASSIGN expression
                            ;
                            
                            
ifStatement 	            : IF expression block 
                            | IF expression block ELSE ifStatement 
                            | IF expression block ELSE block 
                            | IF simpleStatement  SEMICOLON expression block 
                            | IF simpleStatement SEMICOLON  expression block ELSE ifStatement
                            | IF simpleStatement  SEMICOLON expression block ELSE block
                            ;
                            
                             
loop			            : FOR block 
                            | FOR expression block 
                            | FOR simpleStatement SEMICOLON expression SEMICOLON simpleStatement block
                            | FOR simpleStatement SEMICOLON SEMICOLON simpleStatement block
                            ;
                            
                            
switch			            : SWITCH simpleStatement SEMICOLON expression LBRACE expressionCaseClauseList RBRACE 
                            | SWITCH expression LBRACE expressionCaseClauseList RBRACE 
                            | SWITCH simpleStatement SEMICOLON LBRACE expressionCaseClauseList RBRACE 
                            | SWITCH LBRACE expressionCaseClauseList RBRACE
                            ;
                            
                            
expressionCaseClauseList    : /*epsilon*/ 
			                | expressionCaseClause expressionCaseClauseList
			                ;
			                
			                 
expressionCaseClause 	    : expressionSwitchCase COLON statementList;

 
expressionSwitchCase        : CASE expressionList
			                | DEFAULT
			                ;
			                