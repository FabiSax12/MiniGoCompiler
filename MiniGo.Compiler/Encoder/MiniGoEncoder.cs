using System.Globalization;
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
	/// Lexical scope stack. Each entry maps an identifier name to its alloca/global pointer
	/// and the LLVM element type of that pointer. Both are needed: the pointer for
	/// <c>BuildStore</c> and the element type for <c>BuildLoad2</c>.
	/// Stack grows inward: bottom = global scope, top = innermost active scope.
	/// </summary>
	private readonly Stack<Dictionary<string, (LLVMValueRef Ptr, LLVMTypeRef ElemType)>> _scopes = new();

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
	private void PushScope() =>
		_scopes.Push(new Dictionary<string, (LLVMValueRef Ptr, LLVMTypeRef ElemType)>());

	/// <summary>Closes the innermost lexical scope and discards its bindings.</summary>
	private void PopScope() => _scopes.Pop();

	/// <summary>
	/// Registers an alloca/global pointer and its element type under <paramref name="name"/>
	/// in the current innermost scope.
	/// </summary>
	/// <param name="name">Identifier as it appears in MiniGo source.</param>
	/// <param name="ptr">The alloca slot (local) or global value ref.</param>
	/// <param name="elemType">LLVM type of the stored element — needed by <c>BuildLoad2</c>.</param>
	private void DefineLocal(string name, LLVMValueRef ptr, LLVMTypeRef elemType)
	{
		if (_scopes.TryPeek(out var scope))
			scope[name] = (ptr, elemType);
	}

	/// <summary>
	/// Resolves <paramref name="name"/> by walking from the innermost scope outward.
	/// Returns <c>default</c> (null ptr + null type) if not found — this should not
	/// happen after successful semantic analysis.
	/// </summary>
	private (LLVMValueRef Ptr, LLVMTypeRef ElemType) ResolveLocal(string name)
	{
		foreach (var scope in _scopes)
			if (scope.TryGetValue(name, out var entry))
				return entry;
		return default;
	}

	#endregion

	#region Visitor Helpers

	/// <summary>
	/// Visits <paramref name="tree"/> and casts the result to <see cref="LLVMValueRef"/>.
	/// Use for any parse-tree node that is expected to produce an LLVM value (expressions,
	/// literals, operands). Statement visitors return <c>null</c> and must not use this.
	/// </summary>
	private LLVMValueRef VisitExpr(IParseTree tree) => (LLVMValueRef)Visit(tree);

	/// <summary>
	/// Processes a Go-style escape sequence string (content between double-quotes,
	/// with the outer quotes already removed).
	/// </summary>
	private static string UnescapeString(string s) =>
		s.Replace("\\n",  "\n")
		 .Replace("\\t",  "\t")
		 .Replace("\\r",  "\r")
		 .Replace("\\\"", "\"")
		 .Replace("\\\\", "\\");

	/// <summary>
	/// Returns the zero/null constant for the given LLVM type.
	/// Used to default-initialise variables declared without an explicit expression
	/// (<c>var x int</c>) and global variables whose initialiser is not a compile-time constant.
	/// </summary>
	private static LLVMValueRef LlvmDefaultValue(LLVMTypeRef type) =>
		LLVMValueRef.CreateConstNull(type);

	/// <summary>
	/// Resolves the l-value pointer for the left-hand side of an assignment.
	/// For a plain identifier <c>x</c> this is the alloca slot registered in the scope stack.
	/// Array element indexing (<c>arr[i]</c>) is handled in commit 9.
	/// Returns <c>default</c> for unresolvable expressions (should not occur after semantic analysis).
	/// </summary>
	private LLVMValueRef GetLValuePtr(MiniGoParser.ExpressionContext exprCtx)
	{
		var primary = exprCtx.primaryExpression();
		if (primary == null) return default;

		// Simple identifier: x
		var operand = primary.operand();
		if (operand?.IDENTIFIER() != null)
			return ResolveLocal(operand.IDENTIFIER().GetText()).Ptr;

		// Array index: arr[i] — GEP emitted in commit 9; placeholder for now
		// if (primary.index() != null) { ... }

		return default;
	}

	/// <summary>Returns <see langword="true"/> when the builder is not inside any function body.</summary>
	private bool IsGlobalScope() => _currentFunction == default;

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
		// Global scope: wraps all top-level declarations (var, func, type).
		PushScope();
		Visit(context.topDeclarationList());
		PopScope();
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
		// Delegate the no-expression variant: var x int
		if (context.singleVarDeclNoExps() != null)
			return Visit(context.singleVarDeclNoExps());

		var ids   = context.identifierList().IDENTIFIER();
		var exprs = context.expressionList().expression();

		for (int i = 0; i < ids.Length; i++)
		{
			string name = ids[i].GetText();

			// Evaluate the initialiser expression (if provided)
			LLVMValueRef initVal = i < exprs.Length
				? VisitExpr(exprs[i])
				: LLVMValueRef.CreateConstNull(IntType);

			// Determine the LLVM element type:
			//   - Explicit type annotation → use TypeResolver
			//   - Type inference (var x = expr) → derive from the evaluated value
			LLVMTypeRef elemType = context.declType() != null
				? LlvmType(TypeResolver.Resolve(context.declType()))
				: initVal.TypeOf;

			EmitVarBinding(name, initVal, elemType);
		}

		return null;
	}

	public override object VisitSingleVarDeclNoExps(MiniGoParser.SingleVarDeclNoExpsContext context)
	{
		// var x int  (no initialiser → zero value)
		var ids      = context.identifierList().IDENTIFIER();
		LLVMTypeRef elemType = LlvmType(TypeResolver.Resolve(context.declType()));

		foreach (var id in ids)
			EmitVarBinding(id.GetText(), LlvmDefaultValue(elemType), elemType);

		return null;
	}

	/// <summary>
	/// Creates either a local alloca (inside a function) or a global variable (at top level),
	/// stores <paramref name="initVal"/> as the initial value, and registers the binding
	/// in the current scope.
	/// </summary>
	private void EmitVarBinding(string name, LLVMValueRef initVal, LLVMTypeRef elemType)
	{
		if (IsGlobalScope())
		{
			// Global variable — initialiser must be a compile-time constant.
			var global = _module.AddGlobal(elemType, name);
			global.Linkage = LLVMLinkage.LLVMInternalLinkage;
			global.Initializer = initVal.IsConstant
				? initVal
				: LlvmDefaultValue(elemType);
			DefineLocal(name, global, elemType);
		}
		else
		{
			// Local variable — alloca in the current function body.
			var alloca = _builder.BuildAlloca(elemType, name);
			_builder.BuildStore(initVal, alloca);
			DefineLocal(name, alloca, elemType);
		}
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
		// ── Leaf: delegate to primaryExpression ─────────────────────────────
		if (context.primaryExpression() != null)
			return Visit(context.primaryExpression());

		var subExprs = context.expression();

		// ── Unary operators  (-x, +x, !x, ^x) ──────────────────────────────
		if (subExprs.Length == 1)
		{
			LLVMValueRef operand = VisitExpr(subExprs[0]);
			bool isFloat = operand.TypeOf == FloatType;

			if (context.MINUS() != null)
				return isFloat
					? _builder.BuildFNeg(operand, "fneg")
					: _builder.BuildNeg(operand, "neg");

			if (context.NOT()   != null) return _builder.BuildNot(operand, "not");
			if (context.CARET() != null) return _builder.BuildNot(operand, "bitnot"); // bitwise NOT
			if (context.PLUS()  != null) return operand;  // unary + is a no-op

			return operand;
		}

		// ── Binary operators (expr OP expr) ──────────────────────────────────
		if (subExprs.Length == 2)
		{
			LLVMValueRef lhs = VisitExpr(subExprs[0]);
			LLVMValueRef rhs = VisitExpr(subExprs[1]);
			bool isFloat = lhs.TypeOf == FloatType;

			// Arithmetic
			if (context.PLUS()  != null) return isFloat ? _builder.BuildFAdd(lhs, rhs, "fadd") : _builder.BuildAdd (lhs, rhs, "add");
			if (context.MINUS() != null) return isFloat ? _builder.BuildFSub(lhs, rhs, "fsub") : _builder.BuildSub (lhs, rhs, "sub");
			if (context.STAR()  != null) return isFloat ? _builder.BuildFMul(lhs, rhs, "fmul") : _builder.BuildMul (lhs, rhs, "mul");
			if (context.DIV()   != null) return isFloat ? _builder.BuildFDiv(lhs, rhs, "fdiv") : _builder.BuildSDiv(lhs, rhs, "div");
			if (context.MOD()   != null) return _builder.BuildSRem(lhs, rhs, "rem");

			// Comparisons — produce i1 (bool) results
			if (context.EQUALS()         != null) return isFloat
				? _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOEQ, lhs, rhs, "feq")
				: _builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ,    lhs, rhs, "eq");

			if (context.NOT_EQUALS()     != null) return isFloat
				? _builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, lhs, rhs, "fne")
				: _builder.BuildICmp(LLVMIntPredicate.LLVMIntNE,    lhs, rhs, "ne");

			if (context.LESS()           != null) return isFloat
				? _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLT, lhs, rhs, "flt")
				: _builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT,   lhs, rhs, "lt");

			if (context.LESS_EQUALS()    != null) return isFloat
				? _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLE, lhs, rhs, "fle")
				: _builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE,   lhs, rhs, "le");

			if (context.GREATER()        != null) return isFloat
				? _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGT, lhs, rhs, "fgt")
				: _builder.BuildICmp(LLVMIntPredicate.LLVMIntSGT,   lhs, rhs, "gt");

			if (context.GREATER_EQUALS() != null) return isFloat
				? _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGE, lhs, rhs, "fge")
				: _builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE,   lhs, rhs, "ge");

			// Logical short-circuit ops — MiniGo booleans are i1
			if (context.LOGICAL_AND() != null) return _builder.BuildAnd(lhs, rhs, "and");
			if (context.LOGICAL_OR()  != null) return _builder.BuildOr (lhs, rhs, "or");
		}

		return base.VisitExpression(context);
	}

	public override object VisitExpressionList(MiniGoParser.ExpressionListContext context)
	{
		return base.VisitExpressionList(context);
	}

	public override object VisitPrimaryExpression(MiniGoParser.PrimaryExpressionContext context)
	{
		// operand: literal | IDENTIFIER | ( expression )
		if (context.operand() != null)
			return Visit(context.operand());

		// index, selector, arguments, appendExpression, lengthExpression, capExpression
		// are implemented in later commits; fall through to base (returns null) for now.
		return base.VisitPrimaryExpression(context);
	}

	public override object VisitOperand(MiniGoParser.OperandContext context)
	{
		// Literal value (int, float, rune, string)
		if (context.literal() != null)
			return Visit(context.literal());

		// Identifier: true/false are keywords represented as identifiers in the grammar
		if (context.IDENTIFIER() != null)
		{
			string name = context.IDENTIFIER().GetText();

			if (name == "true")
				return LLVMValueRef.CreateConstInt(BoolType, 1, false);
			if (name == "false")
				return LLVMValueRef.CreateConstInt(BoolType, 0, false);

			// Regular variable: find its alloca pointer and load the value
			var (ptr, elemType) = ResolveLocal(name);
			if (ptr == default)
				return LLVMValueRef.CreateConstNull(IntType); // unreachable after semantic analysis
			return _builder.BuildLoad2(elemType, ptr, name);
		}

		// Parenthesised expression: ( expression )
		if (context.expression() != null)
			return Visit(context.expression());

		return LLVMValueRef.CreateConstNull(IntType);
	}

	public override object VisitLiteral(MiniGoParser.LiteralContext context)
	{
		// INTLITERAL → i32 constant (sign-extended so negatives work)
		if (context.INTLITERAL() != null)
		{
			long value = long.Parse(context.INTLITERAL().GetText());
			return LLVMValueRef.CreateConstInt(IntType, (ulong)value, true);
		}

		// FLOATLITERAL → double constant
		if (context.FLOATLITERAL() != null)
		{
			double value = double.Parse(
				context.FLOATLITERAL().GetText(),
				CultureInfo.InvariantCulture);
			return LLVMValueRef.CreateConstReal(FloatType, value);
		}

		// RUNELITERAL → i32 constant (rune = int32 in Go)
		// Grammar produces tokens like 'a', '\n', '\t'
		if (context.RUNELITERAL() != null)
		{
			string text = context.RUNELITERAL().GetText(); // e.g. 'a'
			char c = text.Length >= 3 ? text[1] : '\0';   // strip surrounding quotes
			return LLVMValueRef.CreateConstInt(IntType, (ulong)c, false);
		}

		// RAWSTRINGLITERAL → i8* global constant (backtick strings, used in println)
		// Content is taken verbatim — no escape processing.
		if (context.RAWSTRINGLITERAL() != null)
		{
			string text    = context.RAWSTRINGLITERAL().GetText();       // `content`
			string content = text.Substring(1, text.Length - 2);        // strip backticks
			return _builder.BuildGlobalStringPtr(content, "rawstr");
		}

		// INTERPRETEDSTRINGLITERAL → i8* global constant ("..." strings)
		if (context.INTERPRETEDSTRINGLITERAL() != null)
		{
			string text    = context.INTERPRETEDSTRINGLITERAL().GetText(); // "content"
			string content = UnescapeString(text.Substring(1, text.Length - 2));
			return _builder.BuildGlobalStringPtr(content, "str");
		}

		return LLVMValueRef.CreateConstNull(IntType);
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
		// Every { } block opens its own lexical scope.
		// Function bodies call PushScope themselves (commit 5) so this only
		// handles nested blocks (if/for/switch bodies, bare blocks).
		PushScope();
		Visit(context.statementList());
		PopScope();
		return null;
	}

	public override object VisitStatement(MiniGoParser.StatementContext context)
	{
		return base.VisitStatement(context);
	}

	public override object VisitSimpleStatement(MiniGoParser.SimpleStatementContext context)
	{
		// Short variable declaration:  x, y := expr1, expr2
		if (context.DECLARE_ASSIGN() != null)
		{
			var lhsExprs = context.expressionList()[0].expression();
			var rhsExprs = context.expressionList()[1].expression();

			for (int i = 0; i < lhsExprs.Length && i < rhsExprs.Length; i++)
			{
				// LHS of := must always be a plain identifier (validated by semantic analysis)
				string name   = lhsExprs[i].primaryExpression().operand().IDENTIFIER().GetText();
				LLVMValueRef val      = VisitExpr(rhsExprs[i]);
				LLVMTypeRef  elemType = val.TypeOf;
				var alloca = _builder.BuildAlloca(elemType, name);
				_builder.BuildStore(val, alloca);
				DefineLocal(name, alloca, elemType);
			}

			return null;
		}

		// Increment / Decrement:  x++  or  x--
		if (context.INCREMENT() != null || context.DECREMENT() != null)
		{
			var exprCtx = context.expression();
			LLVMValueRef ptr  = GetLValuePtr(exprCtx);
			if (ptr != default)
			{
				var (_, elemType) = ResolveLocal(
					exprCtx.primaryExpression().operand().IDENTIFIER().GetText());
				LLVMValueRef cur    = _builder.BuildLoad2(elemType, ptr, "inc_cur");
				LLVMValueRef one    = LLVMValueRef.CreateConstInt(elemType, 1, false);
				LLVMValueRef result = context.INCREMENT() != null
					? _builder.BuildAdd(cur, one, "inc")
					: _builder.BuildSub(cur, one, "dec");
				_builder.BuildStore(result, ptr);
			}

			return null;
		}

		// Expression statement (e.g. function call) or assignment
		if (context.assignmentStatement() != null)
			return Visit(context.assignmentStatement());

		// Standalone expression (function call result discarded, etc.)
		if (context.expression() != null)
			Visit(context.expression());

		return null;
	}

	public override object VisitAssignmentStatement(MiniGoParser.AssignmentStatementContext context)
	{
		// Simple assignment:  x = expr  or  x, y = a, b
		if (context.ASSIGN() != null)
		{
			var lhsExprs = context.expressionList()[0].expression();
			var rhsExprs = context.expressionList()[1].expression();

			for (int i = 0; i < lhsExprs.Length && i < rhsExprs.Length; i++)
			{
				LLVMValueRef ptr = GetLValuePtr(lhsExprs[i]);
				if (ptr != default)
					_builder.BuildStore(VisitExpr(rhsExprs[i]), ptr);
			}

			return null;
		}

		// Compound assignments:  x += expr,  x -= expr,  x *= expr,  x /= expr,  x %= expr
		var subExprs = context.expression();
		if (subExprs == null || subExprs.Length < 2) return null;

		LLVMValueRef lhsPtr = GetLValuePtr(subExprs[0]);
		if (lhsPtr == default) return null;

		// Determine element type from the resolved local
		string lhsName = subExprs[0].primaryExpression().operand().IDENTIFIER().GetText();
		var (_, lhsElemType) = ResolveLocal(lhsName);

		LLVMValueRef lhsVal = _builder.BuildLoad2(lhsElemType, lhsPtr, "compound_lhs");
		LLVMValueRef rhsVal = VisitExpr(subExprs[1]);

		bool isFloat = lhsElemType == FloatType;
		LLVMValueRef result;

		if      (context.PLUS_ASSIGN()    != null) result = isFloat ? _builder.BuildFAdd(lhsVal, rhsVal, "fadd") : _builder.BuildAdd (lhsVal, rhsVal, "add");
		else if (context.MINUS_ASSIGN()   != null) result = isFloat ? _builder.BuildFSub(lhsVal, rhsVal, "fsub") : _builder.BuildSub (lhsVal, rhsVal, "sub");
		else if (context.STAR_ASSIGN()    != null) result = isFloat ? _builder.BuildFMul(lhsVal, rhsVal, "fmul") : _builder.BuildMul (lhsVal, rhsVal, "mul");
		else if (context.DIV_ASSIGN()     != null) result = isFloat ? _builder.BuildFDiv(lhsVal, rhsVal, "fdiv") : _builder.BuildSDiv(lhsVal, rhsVal, "div");
		else if (context.MOD_ASSIGN()     != null) result = _builder.BuildSRem(lhsVal, rhsVal, "rem");
		else return null; // bitwise compound ops not in course scope

		_builder.BuildStore(result, lhsPtr);
		return null;
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
