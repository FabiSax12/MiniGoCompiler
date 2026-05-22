using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Generated;
using MiniGo.Compiler.Errors;
using MiniGo.Compiler.Semantic.Symbols;
using MiniGo.Compiler.Shared;

namespace MiniGo.Compiler.Semantic;

public class TypeChecker : MiniGoParserBaseVisitor<object>
{
	private readonly SymbolsTable _table = new();
	private readonly ErrorCollector _collector;
	private readonly string _filePath;
	private Types _currentReturnType = Types.Unknown;
	private int _loopDepth = 0;
	private int _switchDepth = 0;

	public TypeChecker(ErrorCollector collector, string filePath)
	{
		_collector = collector;
		_filePath = filePath;
	}

	private void Error(string message, IToken token)
	{
		_collector.Add(Severity.Error, message, SourceSpan.FromToken(token, _filePath), CompilationPhase.Type);
	}

	private Types VisitType(IParseTree tree)
	{
		var result = Visit(tree);
		return result is Types t ? t : Types.Unknown;
	}

	private static bool IsNumeric(Types type) => type is Types.Integer or Types.Float or Types.Rune;
	private static bool IsInteger(Types type) => type is Types.Integer or Types.Rune;
	private static bool IsOrdered(Types type) => type is Types.Integer or Types.Float or Types.String or Types.Rune;
	private static bool IsIndexable(Types type) => type is Types.Array or Types.Slice or Types.String;

	public override object VisitRoot(MiniGoParser.RootContext context)
	{
		_table.OpenScope();
		base.Visit(context.topDeclarationList());
		_table.CloseScope();
		return null;
	}

	public override object VisitTopDeclarationList(MiniGoParser.TopDeclarationListContext context)
	{
		return base.VisitTopDeclarationList(context);
	}

	public override object VisitVariableDecl(MiniGoParser.VariableDeclContext context)
	{
		return base.VisitVariableDecl(context);
	}

	public override object VisitInnerVarDecls(MiniGoParser.InnerVarDeclsContext context)
	{
		return base.VisitInnerVarDecls(context);
	}

	public override object VisitSingleVarDecl(MiniGoParser.SingleVarDeclContext context)
	{
		if (context.singleVarDeclNoExps() != null || context.identifierList() == null)
		{
			return base.VisitSingleVarDecl(context);
		}

		var declaredType = TypeResolver.Resolve(context.declType());
		var idCount = context.identifierList().IDENTIFIER().Length;
		var varTypes = new Types[idCount];
		for (int i = 0; i < idCount; i++)
		{
			varTypes[i] = declaredType;
		}

		var exprList = context.expressionList();
		if (exprList != null)
		{
			var exprs = exprList.expression();
			if (exprs.Length != idCount)
			{
				Error($"Assignment count mismatch: {idCount} variables but {exprs.Length} values", context.Start);
			}
			else
			{
				for (int i = 0; i < idCount; i++)
				{
					var exprType = VisitType(exprs[i]);
					if (declaredType != Types.Unknown && declaredType != Types.Void)
					{
						if (exprType != declaredType && exprType != Types.Unknown)
						{
							Error($"Type mismatch: expected '{declaredType}' but got '{exprType}'", context.Start);
						}
					}
					else
					{
						varTypes[i] = exprType;
					}
				}
			}
		}

		var ids = context.identifierList().IDENTIFIER();
		for (int i = 0; i < idCount; i++)
		{
			var token = (CommonToken)ids[i].Symbol;
			var symbol = new VarSymbol(token, varTypes[i], _table.GetLevel(), context);
			if (!_table.Define(symbol))
			{
				_collector.Add(
					Severity.Error,
					$"Variable '{ids[i].GetText()}' is already defined in this scope",
					SourceSpan.FromToken(token, _filePath),
					CompilationPhase.Declaration
				);
			}
		}

		return base.VisitSingleVarDecl(context);
	}

	public override object VisitSingleVarDeclNoExps(MiniGoParser.SingleVarDeclNoExpsContext context)
	{
		var type = TypeResolver.Resolve(context.declType());
		foreach (var id in context.identifierList().IDENTIFIER())
		{
			var token = (CommonToken)id.Symbol;
			var symbol = new VarSymbol(token, type, _table.GetLevel(), context);
			if (!_table.Define(symbol))
			{
				_collector.Add(Severity.Error, $"Variable '{id.GetText()}' is already defined in this scope",
					SourceSpan.FromToken(token, _filePath), CompilationPhase.Declaration);
			}
		}

		return base.VisitSingleVarDeclNoExps(context);
	}

	public override object VisitTypeDecl(MiniGoParser.TypeDeclContext context)
	{
		return base.VisitTypeDecl(context);
	}

	public override object VisitInnerTypeDecls(MiniGoParser.InnerTypeDeclsContext context)
	{
		return base.VisitInnerTypeDecls(context);
	}

	public override object VisitSingleTypeDecl(MiniGoParser.SingleTypeDeclContext context)
	{
		return base.VisitSingleTypeDecl(context);
	}

	public override object VisitFuncDecl(MiniGoParser.FuncDeclContext context)
	{
		Visit(context.funcFrontDecl());
		Visit(context.block());
		_currentReturnType = Types.Unknown;
		return null;
	}

	public override object VisitFuncFrontDecl(MiniGoParser.FuncFrontDeclContext context)
	{
		var name = (CommonToken)context.IDENTIFIER().Symbol;
		var returnType = TypeResolver.Resolve(context.declType());
		_currentReturnType = returnType;

		if (!_table.Define(new MethodSymbol(name, returnType, _table.GetLevel(), context)))
		{
			_collector.Add(Severity.Error, $"Function '{name.Text}' is already defined in this scope",
				SourceSpan.FromToken(name, _filePath), CompilationPhase.Declaration);
		}

		return null;
	}

	public override object VisitFuncArgDecls(MiniGoParser.FuncArgDeclsContext context)
	{
		return base.VisitFuncArgDecls(context);
	}

	public override object VisitDeclType(MiniGoParser.DeclTypeContext context)
	{
		return base.VisitDeclType(context);
	}

	public override object VisitSliceDeclType(MiniGoParser.SliceDeclTypeContext context)
	{
		return base.VisitSliceDeclType(context);
	}

	public override object VisitArrayDeclType(MiniGoParser.ArrayDeclTypeContext context)
	{
		return base.VisitArrayDeclType(context);
	}

	public override object VisitStructDeclType(MiniGoParser.StructDeclTypeContext context)
	{
		_table.OpenScope();
		var result = base.VisitStructDeclType(context);
		_table.CloseScope();
		return result;
	}

	public override object VisitStructMemDecls(MiniGoParser.StructMemDeclsContext context)
	{
		return base.VisitStructMemDecls(context);
	}

	public override object VisitIdentifierList(MiniGoParser.IdentifierListContext context)
	{
		return base.VisitIdentifierList(context);
	}

	public override object VisitExpression(MiniGoParser.ExpressionContext context)
	{
		if (context.primaryExpression() != null)
		{
			return VisitType(context.primaryExpression());
		}

		var subExprs = context.expression();

		// Unary operators
		if (subExprs != null && subExprs.Length == 1)
		{
			var operand = VisitType(subExprs[0]);

			if (context.PLUS() != null || context.MINUS() != null || context.CARET() != null)
			{
				if (!IsNumeric(operand) && operand != Types.Unknown)
					Error("Unary operator requires numeric operand", context.Start);
				return operand;
			}

			if (context.NOT() != null)
			{
				if (operand != Types.Boolean && operand != Types.Unknown)
					Error("Logical not requires boolean operand", context.Start);
				return Types.Boolean;
			}
		}

		// Binary operators
		if (subExprs != null && subExprs.Length == 2)
		{
			var left = VisitType(subExprs[0]);
			var right = VisitType(subExprs[1]);

			if (context.STAR() != null || context.DIV() != null || context.MOD() != null ||
			    context.PLUS() != null || context.MINUS() != null ||
			    context.PIPE() != null || context.CARET() != null)
			{
				if ((!IsNumeric(left) || !IsNumeric(right)) && left != Types.Unknown && right != Types.Unknown)
					Error("Arithmetic operators require numeric operands", context.Start);
				return left == Types.Float || right == Types.Float ? Types.Float : left;
			}

			if (context.LSHIFT() != null || context.RSHIFT() != null ||
			    context.AMP() != null || context.BIT_CLEAR() != null)
			{
				if ((!IsInteger(left) || !IsInteger(right)) && left != Types.Unknown && right != Types.Unknown)
					Error("Bitwise operators require integer operands", context.Start);
				return Types.Integer;
			}

			if (context.EQUALS() != null || context.NOT_EQUALS() != null)
			{
				if (left != right && left != Types.Unknown && right != Types.Unknown)
					Error("Comparison requires matching types", context.Start);
				return Types.Boolean;
			}

			if (context.LESS() != null || context.LESS_EQUALS() != null ||
			    context.GREATER() != null || context.GREATER_EQUALS() != null)
			{
				if ((!IsOrdered(left) || !IsOrdered(right) || left != right) && left != Types.Unknown && right != Types.Unknown)
					Error("Ordered comparison requires matching ordered types", context.Start);
				return Types.Boolean;
			}

			if (context.LOGICAL_AND() != null || context.LOGICAL_OR() != null)
			{
				if ((left != Types.Boolean || right != Types.Boolean) && left != Types.Unknown && right != Types.Unknown)
					Error("Logical operators require boolean operands", context.Start);
				return Types.Boolean;
			}
		}

		return Types.Unknown;
	}

	public override object VisitExpressionList(MiniGoParser.ExpressionListContext context)
	{
		return base.VisitExpressionList(context);
	}

	public override object VisitPrimaryExpression(MiniGoParser.PrimaryExpressionContext context)
	{
		if (context.operand() != null)
		{
			return VisitType(context.operand());
		}

		if (context.selector() != null)
		{
			var baseType = VisitType(context.primaryExpression());
			if (baseType != Types.Struct && baseType != Types.Unknown)
				Error("Field access requires struct type", context.Start);
			return Types.Unknown;
		}

		if (context.index() != null)
		{
			var baseType = VisitType(context.primaryExpression());
			var indexType = VisitType(context.index());
			if (!IsIndexable(baseType) && baseType != Types.Unknown)
				Error("Indexing requires array, slice, or string", context.Start);
			if (!IsInteger(indexType) && indexType != Types.Unknown)
				Error("Index must be integer", context.Start);
			return baseType == Types.String ? Types.Rune : Types.Unknown;
		}

		if (context.arguments() != null)
		{
			var baseExpr = context.primaryExpression();
			if (baseExpr != null && baseExpr.operand()?.IDENTIFIER() != null)
			{
				var funcName = baseExpr.operand().IDENTIFIER().GetText();
				var symbol = _table.Lookup(funcName);
				if (symbol is MethodSymbol method)
				{
					var args = context.arguments().expressionList();
					if (args != null)
					{
						foreach (var arg in args.expression())
						{
							VisitType(arg);
						}
					}
					return method.GetTokenType();
				}
			}
			else if (baseExpr != null)
			{
				VisitType(baseExpr);
				var args = context.arguments().expressionList();
				if (args != null)
				{
					foreach (var arg in args.expression())
					{
						VisitType(arg);
					}
				}
			}
			return Types.Unknown;
		}

		if (context.appendExpression() != null)
		{
			return VisitType(context.appendExpression());
		}

		if (context.lengthExpression() != null)
		{
			return VisitType(context.lengthExpression());
		}

		if (context.capExpression() != null)
		{
			return VisitType(context.capExpression());
		}

		return Types.Unknown;
	}

	public override object VisitOperand(MiniGoParser.OperandContext context)
	{
		if (context.literal() != null)
		{
			return VisitType(context.literal());
		}

		if (context.IDENTIFIER() != null)
		{
			var name = context.IDENTIFIER().GetText();
			if (name == "true" || name == "false")
			{
				return Types.Boolean;
			}

			var symbol = _table.Lookup(name);
			if (symbol == null)
			{
				_collector.Add(Severity.Error, $"Undefined identifier: '{name}'",
					SourceSpan.FromToken(context.IDENTIFIER().Symbol, _filePath), CompilationPhase.Declaration);
				return Types.Unknown;
			}
			return symbol.GetTokenType();
		}

		if (context.expression() != null)
		{
			return VisitType(context.expression());
		}

		return Types.Unknown;
	}

	public override object VisitLiteral(MiniGoParser.LiteralContext context)
	{
		if (context.INTLITERAL() != null) return Types.Integer;
		if (context.FLOATLITERAL() != null) return Types.Float;
		if (context.RUNELITERAL() != null) return Types.Rune;
		if (context.RAWSTRINGLITERAL() != null) return Types.String;
		if (context.INTERPRETEDSTRINGLITERAL() != null) return Types.String;
		return Types.Unknown;
	}

	public override object VisitIndex(MiniGoParser.IndexContext context)
	{
		return VisitType(context.expression());
	}

	public override object VisitArguments(MiniGoParser.ArgumentsContext context)
	{
		return base.VisitArguments(context);
	}

	public override object VisitSelector(MiniGoParser.SelectorContext context)
	{
		return base.VisitSelector(context);
	}

	public override object VisitAppendExpression(MiniGoParser.AppendExpressionContext context)
	{
		var subExprs = context.expression();
		if (subExprs != null && subExprs.Length >= 2)
		{
			var sliceType = VisitType(subExprs[0]);
			if (sliceType != Types.Slice && sliceType != Types.Array && sliceType != Types.Unknown)
				Error("Append requires slice or array", context.Start);
			return sliceType;
		}
		return Types.Unknown;
	}

	public override object VisitLengthExpression(MiniGoParser.LengthExpressionContext context)
	{
		var argType = VisitType(context.expression());
		if (!IsIndexable(argType) && argType != Types.Unknown)
			Error("Len requires array, slice, or string", context.Start);
		return Types.Integer;
	}

	public override object VisitCapExpression(MiniGoParser.CapExpressionContext context)
	{
		var argType = VisitType(context.expression());
		if (argType != Types.Array && argType != Types.Slice && argType != Types.Unknown)
			Error("Cap requires array or slice", context.Start);
		return Types.Integer;
	}

	public override object VisitStatementList(MiniGoParser.StatementListContext context)
	{
		return base.VisitStatementList(context);
	}

	public override object VisitBlock(MiniGoParser.BlockContext context)
	{
		_table.OpenScope();

		if (context.Parent is MiniGoParser.FuncDeclContext funcDecl)
		{
			var front = funcDecl.funcFrontDecl();
			if (front.funcArgDecls() != null)
			{
				foreach (var arg in front.funcArgDecls().singleVarDeclNoExps())
				{
					var argType = TypeResolver.Resolve(arg.declType());
					foreach (var id in arg.identifierList().IDENTIFIER())
					{
						var token = (CommonToken)id.Symbol;
						var symbol = new VarSymbol(token, argType, _table.GetLevel(), arg);
						if (!_table.Define(symbol))
						{
							_collector.Add(Severity.Error, $"Parameter '{id.GetText()}' is already defined",
								SourceSpan.FromToken(token, _filePath), CompilationPhase.Declaration);
						}
					}
				}
			}
		}

		var result = base.VisitBlock(context);
		_table.CloseScope();
		return result;
	}

	public override object VisitStatement(MiniGoParser.StatementContext context)
	{
		if (context.PRINT() != null || context.PRINTLN() != null)
		{
			var exprList = context.expressionList();
			if (exprList != null)
			{
				foreach (var expr in exprList.expression())
				{
					VisitType(expr);
				}
			}
			return null;
		}

		if (context.RETURN() != null)
		{
			var expr = context.expression();
			if (expr != null)
			{
				var exprType = VisitType(expr);
				if (_currentReturnType == Types.Void && exprType != Types.Unknown)
				{
					Error($"Function has no return type but returns '{exprType}'", context.Start);
				}
				else if (exprType != _currentReturnType && _currentReturnType != Types.Unknown && exprType != Types.Unknown)
				{
					Error($"Return type mismatch: expected '{_currentReturnType}' but got '{exprType}'", context.Start);
				}
			}
			else if (_currentReturnType != Types.Unknown && _currentReturnType != Types.Void)
			{
				Error($"Missing return value: expected '{_currentReturnType}'", context.Start);
			}
			return null;
		}

		if (context.BREAK() != null)
		{
			if (_loopDepth == 0 && _switchDepth == 0)
				Error("Break statement outside of loop or switch", context.Start);
			return null;
		}

		if (context.CONTINUE() != null)
		{
			if (_loopDepth == 0)
				Error("Continue statement outside of loop", context.Start);
			return null;
		}

		return base.VisitStatement(context);
	}

	public override object VisitSimpleStatement(MiniGoParser.SimpleStatementContext context)
	{
		if (context.assignmentStatement() != null)
		{
			return Visit(context.assignmentStatement());
		}

		if (context.INCREMENT() != null || context.DECREMENT() != null)
		{
			var expr = context.expression();
			if (expr != null)
			{
				var exprType = VisitType(expr);
				if (!IsNumeric(exprType) && exprType != Types.Unknown)
					Error("Increment/decrement requires numeric operand", context.Start);
			}
			return null;
		}

		if (context.DECLARE_ASSIGN() != null)
		{
			var exprLists = context.expressionList();
			if (exprLists != null && exprLists.Length == 2)
			{
				var lhsExprs = exprLists[0].expression();
				var rhsExprs = exprLists[1].expression();
				if (lhsExprs.Length != rhsExprs.Length)
				{
					Error($"Assignment count mismatch: {lhsExprs.Length} variables but {rhsExprs.Length} values", context.Start);
				}
				else
				{
					for (int i = 0; i < lhsExprs.Length; i++)
					{
						var rhsType = VisitType(rhsExprs[i]);
						var lhsExpr = lhsExprs[i];
						var lhsPrimary = lhsExpr.primaryExpression();
						if (lhsPrimary?.operand()?.IDENTIFIER() != null)
						{
							var id = lhsPrimary.operand().IDENTIFIER();
							var name = id.GetText();
							var existing = _table.Lookup(name);
							if (existing != null)
							{
								Error($"Variable '{name}' is already defined in this scope", id.Symbol);
							}
							else
							{
								var token = (CommonToken)id.Symbol;
								var symbol = new VarSymbol(token, rhsType, _table.GetLevel(), context);
								_table.Define(symbol);
							}
						}
					}
				}
			}
			return null;
		}

		if (context.expression() != null)
		{
			VisitType(context.expression());
			return null;
		}

		return null;
	}

	public override object VisitAssignmentStatement(MiniGoParser.AssignmentStatementContext context)
	{
		if (context.ASSIGN() != null)
		{
			var exprLists = context.expressionList();
			if (exprLists != null && exprLists.Length == 2)
			{
				var leftExprs = exprLists[0].expression();
				var rightExprs = exprLists[1].expression();
				if (leftExprs.Length != rightExprs.Length)
				{
					Error($"Assignment count mismatch: {leftExprs.Length} left but {rightExprs.Length} right", context.Start);
				}
				else
				{
					for (int i = 0; i < leftExprs.Length; i++)
					{
						var leftType = VisitType(leftExprs[i]);
						var rightType = VisitType(rightExprs[i]);
						if (leftType != rightType && leftType != Types.Unknown && rightType != Types.Unknown)
						{
							Error($"Assignment type mismatch: cannot assign '{rightType}' to '{leftType}'", context.Start);
						}
					}
				}
			}
			return null;
		}

		// Compound assignment
		var subExprs = context.expression();
		if (subExprs != null && subExprs.Length == 2)
		{
			var left = VisitType(subExprs[0]);
			var right = VisitType(subExprs[1]);
			if ((!IsNumeric(left) || !IsNumeric(right)) && left != Types.Unknown && right != Types.Unknown)
			{
				Error("Compound assignment requires numeric operands", context.Start);
			}
		}
		return null;
	}

	public override object VisitIfStatement(MiniGoParser.IfStatementContext context)
	{
		if (context.simpleStatement() != null)
		{
			Visit(context.simpleStatement());
		}

		var condExpr = context.expression();
		if (condExpr != null)
		{
			var condType = VisitType(condExpr);
			if (condType != Types.Boolean && condType != Types.Unknown)
			{
				Error("If condition must be boolean", condExpr.Start);
			}
		}

		var blocks = context.block();
		if (blocks != null && blocks.Length > 0)
		{
			Visit(blocks[0]);
		}

		if (context.ELSE() != null)
		{
			if (context.ifStatement() != null)
			{
				Visit(context.ifStatement());
			}
			else if (blocks != null && blocks.Length > 1)
			{
				Visit(blocks[1]);
			}
		}

		return null;
	}

	public override object VisitLoop(MiniGoParser.LoopContext context)
	{
		// for block
		var condExpr = context.expression();
		var simpleStmts = context.simpleStatement();
		var block = context.block();

		if (condExpr == null && (simpleStmts == null || simpleStmts.Length == 0))
		{
			_loopDepth++;
			if (block != null) Visit(block);
			_loopDepth--;
			return null;
		}

		// for expression block
		if (condExpr != null && (simpleStmts == null || simpleStmts.Length == 0))
		{
			var condType = VisitType(condExpr);
			if (condType != Types.Boolean && condType != Types.Unknown)
				Error("For condition must be boolean", condExpr.Start);
			_loopDepth++;
			if (block != null) Visit(block);
			_loopDepth--;
			return null;
		}

		// for init; cond; post block
		if (simpleStmts != null && simpleStmts.Length >= 2)
		{
			_table.OpenScope();
			Visit(simpleStmts[0]);
			if (condExpr != null)
			{
				var condType = VisitType(condExpr);
				if (condType != Types.Boolean && condType != Types.Unknown)
					Error("For condition must be boolean", condExpr.Start);
			}
			Visit(simpleStmts[1]);
			_loopDepth++;
			if (block != null) Visit(block);
			_loopDepth--;
			_table.CloseScope();
			return null;
		}

		return base.VisitLoop(context);
	}

	public override object VisitSwitch(MiniGoParser.SwitchContext context)
	{
		if (context.simpleStatement() != null)
		{
			Visit(context.simpleStatement());
		}
		var switchExpr = context.expression();
		if (switchExpr != null)
		{
			VisitType(switchExpr);
		}
		_switchDepth++;
		Visit(context.expressionCaseClauseList());
		_switchDepth--;
		return null;
	}

	public override object VisitExpressionCaseClauseList(MiniGoParser.ExpressionCaseClauseListContext context)
	{
		return base.VisitExpressionCaseClauseList(context);
	}

	public override object VisitExpressionCaseClause(MiniGoParser.ExpressionCaseClauseContext context)
	{
		return base.VisitExpressionCaseClause(context);
	}

	public override object VisitExpressionSwitchCase(MiniGoParser.ExpressionSwitchCaseContext context)
	{
		return base.VisitExpressionSwitchCase(context);
	}

	public override object VisitChildren(IRuleNode node)
	{
		return base.VisitChildren(node);
	}

	public override object VisitTerminal(ITerminalNode node)
	{
		return base.VisitTerminal(node);
	}

	public override object VisitErrorNode(IErrorNode node)
	{
		return base.VisitErrorNode(node);
	}
}
