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

	public TypeChecker(ErrorCollector collector, string filePath)
	{
		_collector = collector;
		_filePath = filePath;
	}

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

		var type = TypeResolver.Resolve(context.declType());
		foreach (var id in context.identifierList().IDENTIFIER())
		{
			var token = (CommonToken)id.Symbol;
			var symbol = new VarSymbol(token, type, _table.GetLevel(), context);
			if (!_table.Define(symbol))
			{
				_collector.Add(
					Severity.Error,
					$"Variable '{id.GetText()}' is already defined in this scope",
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
		return base.VisitFuncDecl(context);
	}

	public override object VisitFuncFrontDecl(MiniGoParser.FuncFrontDeclContext context)
	{
		var name = (CommonToken)context.IDENTIFIER().Symbol;
		var returnType = TypeResolver.Resolve(context.declType());

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
		return base.VisitExpression(context);
	}

	public override object VisitExpressionList(MiniGoParser.ExpressionListContext context)
	{
		return base.VisitExpressionList(context);
	}

	public override object VisitPrimaryExpression(MiniGoParser.PrimaryExpressionContext context)
	{
		return base.VisitPrimaryExpression(context);
	}

	public override object VisitOperand(MiniGoParser.OperandContext context)
	{
		if (context.IDENTIFIER() != null)
		{
			var name = context.IDENTIFIER().GetText();
			if (name is not "true" and not "false")
			{
				var symbol = _table.Lookup(name);
				if (symbol == null)
				{
					_collector.Add(Severity.Error, $"Undefined identifier: '{name}'",
						SourceSpan.FromToken(context.IDENTIFIER().Symbol, _filePath), CompilationPhase.Declaration);
				}
			}
		}

		return base.VisitOperand(context);
	}

	public override object VisitLiteral(MiniGoParser.LiteralContext context)
	{
		return base.VisitLiteral(context);
	}

	public override object VisitIndex(MiniGoParser.IndexContext context)
	{
		return base.VisitIndex(context);
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
		return base.VisitAppendExpression(context);
	}

	public override object VisitLengthExpression(MiniGoParser.LengthExpressionContext context)
	{
		return base.VisitLengthExpression(context);
	}

	public override object VisitCapExpression(MiniGoParser.CapExpressionContext context)
	{
		return base.VisitCapExpression(context);
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
		return base.VisitStatement(context);
	}

	public override object VisitSimpleStatement(MiniGoParser.SimpleStatementContext context)
	{
		return base.VisitSimpleStatement(context);
	}

	public override object VisitAssignmentStatement(MiniGoParser.AssignmentStatementContext context)
	{
		return base.VisitAssignmentStatement(context);
	}

	public override object VisitIfStatement(MiniGoParser.IfStatementContext context)
	{
		return base.VisitIfStatement(context);
	}

	public override object VisitLoop(MiniGoParser.LoopContext context)
	{
		return base.VisitLoop(context);
	}

	public override object VisitSwitch(MiniGoParser.SwitchContext context)
	{
		return base.VisitSwitch(context);
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
