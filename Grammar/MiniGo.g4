grammar MiniGo;

root			            : 'package'  IDENTIFIER ';' topDeclarationList;


topDeclarationList	        : ( variableDecl | typeDecl | funcDecl)*;


variableDecl		        : 'var' singleVarDecl ';'
			                | 'var'  '(' innerVarDecls ')' ';'
			                | 'var'  '(' ')' ';'
			                ;

			                
innerVarDecls		        : singleVarDecl ';' (singleVarDecl ';')*;


singleVarDecl		        : identifierList declType '=' expressionList
			                | identifierList '=' expressionList
			                | singleVarDeclNoExps
			                ;

			                
singleVarDeclNoExps	        : identifierList declType;	
typeDecl		            : 'type' singleTypeDecl ';'
			                | 'type' '(' innerTypeDecls ')' ';'
			                | 'type' '(' ')' ';'
			                ;

			                
innerTypeDecls		        : singleTypeDecl ';' (singleTypeDecl ';')*;


singleTypeDecl		        : IDENTIFIER declType;


funcDecl		            : funcFrontDecl block ';';


funcFrontDecl		        : 'func' IDENTIFIER '(' (funcArgDecls|/*epsilon*/) ')' (declType|/*epsilon*/);


funcArgDecls		        : singleVarDeclNoExps (',' singleVarDeclNoExps)*;


declType		            : '(' declType ')'	
                            | IDENTIFIER
                            | sliceDeclType
                            | arrayDeclType
                            | structDeclType
                            ;

                            
sliceDeclType		        : '[' ']' declType;


arrayDeclType		        : '[' INTLITERAL ']' declType;


structDeclType		        : 'struct' '{' (structMemDecls|/*epsilon*/) '}';


structMemDecls	            : singleVarDeclNoExps ';' (singleVarDeclNoExps ';')*;


identifierList		        : IDENTIFIER (',' IDENTIFIER)*;


expression		            : primaryExpression
                            | expression  '*' expression
                            | expression '/' expression
                            | expression '%' expression
                            | expression '<<' expression
                            | expression '>>' expression
                            | expression '&' expression
                            | expression '&^ ' expression
                            | expression '+' expression
                            | expression '-' expression	
                            | expression '|' expression
                            | expression '^' expression	
                            | expression '==' expression
                            | expression '!=' expression
                            | expression '<' expression
                            | expression '<=' expression
                            | expression '>' expression
                            | expression '>=' expression
                            | expression '&&' expression
                            | expression '||' expression
                            | '+' expression 
                            | '-' expression
                            | '!' expression 
                            | '^' expression
                            ;
                            
                            
expressionList		        : expression (',' expression)*;


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
                            | '(' expression ')'
                            ;
                            
                            
literal			            : INTLITERAL								 
                            | FLOATLITERAL							 
                            | RUNELITERAL							 
                            | RAWSTRINGLITERAL						 
                            | INTERPRETEDSTRINGLITERAL
                            ;
                            
                            					
index			            : '[' expression ']';


arguments		            : '(' expressionList | /*epsilon*/ ')';


selector		            : '.' IDENTIFIER;


appendExpression	        : 'append' '(' expression ',' expression ')';

 
lengthExpression	        : 'len' '(' expression ')';


capExpression		        : 'cap' '(' expression ')';


statementList 		        : statement* ;


block 			            : '{' statementList '}';

 
statement		            : 'print' '(' expressionList | /*epsilon*/ ')' ';' 
                            | 'println' '(' expressionList | /*epsilon*/ ')' ';' 
                            | 'return' (expression | /*epsilon*/) ';' 
                            | 'break' ';' 
                            | 'continue' ';'
                            | simpleStatement ';' 
                            | block ';'
                            | switch ';'
                            | ifStatement ';'
                            | loop ';'
                            | typeDecl
                            | variableDecl
                            ;
                            
                            
simpleStatement	            : /*epsilon*/ 
                            | expression ('++' | '--' | /*epsilon*/) 
                            | assignmentStatement 
                            | expressionList ':=' expressionList
                            ;
                            
                            
assignmentStatement 	    : expressionList '=' expressionList 
                            |expression '+=' expression
                            |expression '&=' expression 
                            |expression '-=' expression
                            |expression '|=' expression
                            |expression '*=' expression 
                            |expression '^=' expression 
                            |expression '<<=' expression 
                            |expression '>>=' expression 
                            |expression '&^=' expression
                            |expression '%=' expression
                            |expression '/=' expression
                            ;
                            
                            
ifStatement 	            : 'if' expression block 
                            | 'if' expression block 'else' ifStatement 
                            | 'if' expression block 'else' block 
                            | 'if' simpleStatement  ';' expression block 
                            | 'if' simpleStatement ';'  expression block 'else' ifStatement
                            | 'if' simpleStatement  ';' expression block 'else' block
                            ;
                            
                             
loop			            : 'for' block 
                            | 'for' expression block 
                            | 'for' simpleStatement ';' expression ';' simpleStatement block
                            | 'for' simpleStatement ';' ';' simpleStatement block
                            ;
                            
                            
switch			            : 'switch' simpleStatement ';' expression '{' expressionCaseClauseList '}' 
                            | 'switch' expression '{' expressionCaseClauseList '}' 
                            | 'switch' simpleStatement ';' '{' expressionCaseClauseList '}' 
                            | 'switch' '{' expressionCaseClauseList '}'
                            ;
                            
                            
expressionCaseClauseList    : /*epsilon*/ 
			                | expressionCaseClause expressionCaseClauseList
			                ;
			                
			                 
expressionCaseClause 	    : expressionSwitchCase ':' statementList;

 
expressionSwitchCase        : 'case' expressionList
			                | 'default'
			                ;
			                