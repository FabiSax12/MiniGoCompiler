using Antlr4.Runtime.Tree;
using Generated;
using LLVMSharp.Interop;
using MiniGo.Compiler.Semantic;

namespace MiniGo.Compiler.Encoder;

/// <summary>
/// MiniGo LLVM code generator. Traverses the parse tree produced by the parser
/// and emits LLVM IR for the subset of MiniGo required by the course spec:
/// simple variables (global and local), integer arrays, functions,
/// if/for control flow, println/len, and arithmetic + comparison expressions.
/// </summary>
public sealed class MiniGoEncoder : MiniGoParserBaseVisitor<object>, IDisposable
{
	#region LLVM Infrastructure

	/// <summary>The LLVM module that accumulates all declarations and function bodies.</summary>
	private readonly LLVMModuleRef _module;

	/// <summary>IR builder — repositioned at the active insertion point during codegen.</summary>
	private readonly LLVMBuilderRef _builder;

	/// <summary>
	/// Lexical scope stack. Each entry maps an identifier name to its alloca slot (local)
	/// or global value ref. The stack grows inward: bottom entry = global scope,
	/// top entry = innermost active scope.
	/// </summary>
	private readonly Stack<Dictionary<string, LLVMValueRef>> _scopes = new();

	/// <summary>Declared function values keyed by name — used when building call instructions.</summary>
	private readonly Dictionary<string, LLVMValueRef> _functions = new();

	/// <summary>
	/// LLVM function types keyed by name. <c>BuildCall2</c> requires the callee's
	/// <see cref="LLVMTypeRef"/> separately from the <see cref="LLVMValueRef"/>.
	/// </summary>
	private readonly Dictionary<string, LLVMTypeRef> _functionTypes = new();

	/// <summary>
	/// The function value currently being compiled.
	/// Set when entering a <c>funcDecl</c> visitor and cleared when exiting.
	/// </summary>
	private LLVMValueRef _currentFunction;

	/// <summary>
	/// Initialises the LLVM module and IR builder using the global LLVM context.
	/// </summary>
	/// <param name="moduleName">
	/// Name embedded in the generated IR — typically the source file name without extension.
	/// </param>
	public MiniGoEncoder(string moduleName = "minigo")
	{
		_module  = LLVMModuleRef.CreateWithName(moduleName);
		_builder = LLVMBuilderRef.Create(LLVMContextRef.Global);
	}

	/// <summary>Releases LLVM resources held by the module and builder.</summary>
	public void Dispose()
	{
		_builder.Dispose();
		_module.Dispose();
	}

	#endregion

	#region Type Helpers

	/// <summary>
	/// Maps a MiniGo <see cref="Types"/> enum value to the corresponding LLVM type ref.
	/// Called when allocating variables, declaring function parameters/returns,
	/// and constructing array types.
	/// </summary>
	private static LLVMTypeRef LlvmType(Types type) => type switch
	{
		Types.Integer => LLVMTypeRef.Int32,
		Types.Float   => LLVMTypeRef.Double,
		Types.Boolean => LLVMTypeRef.Int1,
		Types.Rune    => LLVMTypeRef.Int32,  // rune = int32 in Go
		Types.String  => LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0),
		Types.Void    => LLVMTypeRef.Void,
		_             => LLVMTypeRef.Int32   // safe fallback
	};

	// ── Convenience shorthands ────────────────────────────────────────────
	// Avoids repeating LLVMTypeRef.* throughout the visitor methods.

	/// <summary>LLVM i32 — MiniGo <c>int</c> and <c>rune</c>.</summary>
	private static LLVMTypeRef IntType    => LLVMTypeRef.Int32;

	/// <summary>LLVM double (f64) — MiniGo <c>float</c>.</summary>
	private static LLVMTypeRef FloatType  => LLVMTypeRef.Double;

	/// <summary>LLVM i1 — MiniGo <c>bool</c> and all comparison results.</summary>
	private static LLVMTypeRef BoolType   => LLVMTypeRef.Int1;

	/// <summary>LLVM i8 — byte element type for string pointers.</summary>
	private static LLVMTypeRef Int8Type   => LLVMTypeRef.Int8;

	/// <summary>LLVM i8* — MiniGo string values (pointer to UTF-8 char data).</summary>
	private static LLVMTypeRef StringType => LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);

	/// <summary>LLVM void — functions that return nothing.</summary>
	private static LLVMTypeRef VoidType   => LLVMTypeRef.Void;

	/// <summary>
	/// Builds an LLVM array type for a fixed-size integer array: <c>[length x i32]</c>.
	/// </summary>
	private static LLVMTypeRef IntArrayType(uint length) =>
		LLVMTypeRef.CreateArray(LLVMTypeRef.Int32, length);

	#endregion

	#region Scope and Symbol Helpers

	/// <summary>
	/// Opens a new lexical scope. Call at the start of every block (<c>{}</c>)
	/// and function body so variable lifetimes are properly isolated.
	/// </summary>
	private void PushScope() => _scopes.Push(new Dictionary<string, LLVMValueRef>());

	/// <summary>Closes the innermost lexical scope and discards its bindings.</summary>
	private void PopScope() => _scopes.Pop();

	/// <summary>
	/// Registers <paramref name="value"/> under <paramref name="name"/> in the current
	/// innermost scope. The value is normally an alloca slot (local) or global ref.
	/// </summary>
	private void DefineValue(string name, LLVMValueRef value)
	{
		if (_scopes.TryPeek(out var scope))
			scope[name] = value;
	}

	/// <summary>
	/// Resolves <paramref name="name"/> by walking from the innermost scope outward.
	/// Returns a default (null) <see cref="LLVMValueRef"/> if not found — this should
	/// not happen after successful semantic analysis.
	/// </summary>
	private LLVMValueRef ResolveValue(string name)
	{
		foreach (var scope in _scopes)
			if (scope.TryGetValue(name, out var val))
				return val;
		return default;
	}

	#endregion

	#region IR Output

	/// <summary>
	/// Verifies the generated module and returns its LLVM IR as a text string.
	/// </summary>
	/// <returns>Full LLVM IR text, ready to be written to a <c>.ll</c> file.</returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the IR fails LLVM's structural verification.
	/// This signals a bug in the encoder, not a user code error.
	/// </exception>
	public string EmitIr()
	{
		if (!_module.TryVerify(LLVMVerifierFailureAction.LLVMReturnStatusAction, out string error))
			throw new InvalidOperationException($"LLVM IR verification failed:\n{error}");
		return _module.PrintToString();
	}

	/// <summary>
	/// Verifies the module and writes the LLVM IR to <paramref name="path"/>
	/// as a <c>.ll</c> text file.
	/// </summary>
	public void EmitIrToFile(string path) => File.WriteAllText(path, EmitIr());

	#endregion

	#region Visitor Overrides

	public override object VisitRoot(MiniGoParser.RootContext context)
	{
		return base.VisitRoot(context);
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
		return base.VisitSingleVarDecl(context);
	}

	public override object VisitSingleVarDeclNoExps(MiniGoParser.SingleVarDeclNoExpsContext context)
	{
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
		return base.VisitFuncFrontDecl(context);
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
		return base.VisitStructDeclType(context);
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
		return base.VisitBlock(context);
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

	public override object Visit(IParseTree tree)
	{
		return base.Visit(tree);
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

	#endregion
}
