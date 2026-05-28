using System.Globalization;
using System.Text;
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

	/// <summary>
	/// Resolves the LLVM type for a <c>declType</c> context, handling array types correctly.
	/// For <c>arrayDeclType</c> (<c>[n]int</c>), returns <c>[n x i32]</c>.
	/// For all other types, delegates to <see cref="LlvmType"/> via <see cref="TypeResolver"/>.
	/// </summary>
	private static LLVMTypeRef LlvmTypeFromDecl(MiniGoParser.DeclTypeContext ctx)
	{
		if (ctx.arrayDeclType() != null)
		{
			uint n = uint.Parse(ctx.arrayDeclType().INTLITERAL().GetText());
			return IntArrayType(n);
		}
		return LlvmType(TypeResolver.Resolve(ctx));
	}

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

		// Array element: arr[i]  →  GEP2 pointer to element (used by BuildStore in caller)
		if (primary.index() != null)
		{
			string arrName = primary.primaryExpression().operand().IDENTIFIER().GetText();
			var (arrPtr, arrType) = ResolveLocal(arrName);
			LLVMValueRef idx  = VisitExpr(primary.index().expression());
			LLVMValueRef zero = LLVMValueRef.CreateConstInt(IntType, 0, false);
			return _builder.BuildGEP2(arrType, arrPtr, new[] { zero, idx }, "elem_ptr");
		}

		return default;
	}

	/// <summary>Returns <see langword="true"/> when the builder is not inside any function body.</summary>
	private bool IsGlobalScope() => _currentFunction == default;

	/// <summary>
	/// Positions the IR builder at the end of <paramref name="block"/>.
	/// <para>
	/// LLVMSharp exposes <c>LLVMPositionBuilderAtEnd</c> only through the low-level static
	/// <see cref="LLVM"/> class, whose methods take raw opaque pointer types
	/// (<c>LLVMOpaqueBuilder*</c>, <c>LLVMOpaqueBasicBlock*</c>) and therefore require an
	/// <c>unsafe</c> context. This wrapper isolates that requirement to a single private method
	/// so the rest of the encoder stays safe.  <c>AllowUnsafeBlocks</c> is enabled in the
	/// project file for this reason.
	/// </para>
	/// </summary>
	private unsafe void PositionAtEnd(LLVMBasicBlockRef block) =>
		LLVM.PositionBuilderAtEnd(_builder, block);

	/// <summary>
	/// Lazily declares <c>@printf(i8*, ...) i32</c> in the module and caches it in
	/// <c>_functions</c> / <c>_functionTypes</c>. Subsequent calls return the cached value.
	/// </summary>
	private LLVMValueRef GetOrDeclarePrintf()
	{
		const string name = "printf";
		if (_functions.TryGetValue(name, out var existing)) return existing;

		// declare i32 @printf(i8* nocapture, ...)
		LLVMTypeRef ty = LLVMTypeRef.CreateFunction(IntType, new[] { StringType }, true);
		LLVMValueRef fn = _module.AddFunction(name, ty);
		_functions[name]     = fn;
		_functionTypes[name] = ty;
		return fn;
	}

	/// <summary>
	/// Emits a <c>printf</c> call for every expression in <paramref name="exprList"/>,
	/// separated by spaces and optionally followed by a newline.
	/// Type dispatch: float → <c>%f</c>, string/i8* → <c>%s</c>,
	/// bool (i1) → <c>%d</c> after zero-extension, everything else (int/rune) → <c>%d</c>.
	/// </summary>
	private void EmitPrintf(MiniGoParser.ExpressionListContext? exprList, bool newline)
	{
		LLVMValueRef printfFn   = GetOrDeclarePrintf();
		LLVMTypeRef  printfType = _functionTypes["printf"];

		var fmtBuilder = new StringBuilder();
		var argVals    = new List<LLVMValueRef>();

		if (exprList != null)
		{
			foreach (var expr in exprList.expression())
			{
				if (argVals.Count > 0) fmtBuilder.Append(' ');

				LLVMValueRef val = VisitExpr(expr);

				if (val.TypeOf == FloatType)
				{
					fmtBuilder.Append("%f");
				}
				else if (val.TypeOf == StringType)
				{
					fmtBuilder.Append("%s");
				}
				else if (val.TypeOf == BoolType)
				{
					// i1 is not a valid printf argument — zero-extend to i32
					fmtBuilder.Append("%d");
					val = _builder.BuildZExt(val, IntType, "bool2int");
				}
				else
				{
					// int (i32), rune (i32)
					fmtBuilder.Append("%d");
				}

				argVals.Add(val);
			}
		}

		if (newline) fmtBuilder.Append('\n');

		// Build the format string as a global i8* constant
		LLVMValueRef fmtStr = _builder.BuildGlobalStringPtr(fmtBuilder.ToString(), "println_fmt");

		// Assemble args: format string first, then value arguments
		var allArgs = new LLVMValueRef[1 + argVals.Count];
		allArgs[0] = fmtStr;
		argVals.CopyTo(allArgs, 1);

		_builder.BuildCall2(printfType, printfFn, allArgs, "");
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
			//   - Explicit type annotation → use LlvmTypeFromDecl (handles arrays)
			//   - Type inference (var x = expr) → derive from the evaluated value
			LLVMTypeRef elemType = context.declType() != null
				? LlvmTypeFromDecl(context.declType())
				: initVal.TypeOf;

			EmitVarBinding(name, initVal, elemType);
		}

		return null;
	}

	public override object VisitSingleVarDeclNoExps(MiniGoParser.SingleVarDeclNoExpsContext context)
	{
		// var x int  or  var a [5]int  (no initialiser → zero value)
		var ids = context.identifierList().IDENTIFIER();
		LLVMTypeRef elemType = LlvmTypeFromDecl(context.declType());

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
		var front = context.funcFrontDecl();
		string name = front.IDENTIFIER().GetText();

		// ── Return type ──────────────────────────────────────────────────────
		LLVMTypeRef returnType = front.declType() != null
			? LlvmType(TypeResolver.Resolve(front.declType()))
			: VoidType;

		// ── Parameter names and types ────────────────────────────────────────
		// funcArgDecls : singleVarDeclNoExps (COMMA singleVarDeclNoExps)*
		// singleVarDeclNoExps : identifierList declType
		// One singleVarDeclNoExps can declare multiple ids of the same type: a, b int
		var paramNames = new List<string>();
		var paramTypes = new List<LLVMTypeRef>();

		if (front.funcArgDecls() != null)
		{
			foreach (var argDecl in front.funcArgDecls().singleVarDeclNoExps())
			{
				LLVMTypeRef pType = LlvmType(TypeResolver.Resolve(argDecl.declType()));
				foreach (var id in argDecl.identifierList().IDENTIFIER())
				{
					paramNames.Add(id.GetText());
					paramTypes.Add(pType);
				}
			}
		}

		// ── Create LLVM function ─────────────────────────────────────────────
		LLVMTypeRef funcType = LLVMTypeRef.CreateFunction(returnType, paramTypes.ToArray());
		LLVMValueRef func    = _module.AddFunction(name, funcType);
		_functions[name]     = func;
		_functionTypes[name] = funcType;

		// ── Entry block ──────────────────────────────────────────────────────
		LLVMBasicBlockRef entry = func.AppendBasicBlock("entry");
		PositionAtEnd(entry);
		_currentFunction = func;

		// ── Parameter allocas (param scope lives outside the block scope) ────
		PushScope();
		for (int i = 0; i < paramNames.Count; i++)
		{
			LLVMValueRef alloca = _builder.BuildAlloca(paramTypes[i], paramNames[i]);
			_builder.BuildStore(func.GetParam((uint)i), alloca);
			DefineLocal(paramNames[i], alloca, paramTypes[i]);
		}

		// ── Body (VisitBlock opens its own inner scope) ──────────────────────
		Visit(context.block());

		// ── Implicit terminator for void functions or missing return ─────────
		// LLVM verification requires every basic block to end with a terminator.
		// If semantic analysis passed but the last block has no branch/ret, add one.
		var lastBlock = _currentFunction.LastBasicBlock;
		if (lastBlock.Terminator == default)
		{
			PositionAtEnd(lastBlock);
			_builder.BuildRetVoid();
		}

		// ── Cleanup ──────────────────────────────────────────────────────────
		PopScope();
		_currentFunction = default;

		return null;
	}

	public override object VisitFuncFrontDecl(MiniGoParser.FuncFrontDeclContext context)
	{
		// Processed directly by VisitFuncDecl; no standalone visit needed.
		return base.VisitFuncFrontDecl(context);
	}

	public override object VisitFuncArgDecls(MiniGoParser.FuncArgDeclsContext context)
	{
		// Processed directly by VisitFuncDecl; no standalone visit needed.
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

		// Function call: primaryExpression arguments
		// The nested primaryExpression is the callee; arguments wraps the arg list.
		if (context.arguments() != null)
		{
			// Callee must be a plain identifier (MiniGo has no first-class functions)
			string funcName = context.primaryExpression().operand().IDENTIFIER().GetText();

			if (!_functions.TryGetValue(funcName, out var funcVal) ||
			    !_functionTypes.TryGetValue(funcName, out var funcType))
				return LLVMValueRef.CreateConstNull(IntType); // unreachable after semantic analysis

			// Evaluate arguments
			var args = new List<LLVMValueRef>();
			if (context.arguments().expressionList() != null)
				foreach (var expr in context.arguments().expressionList().expression())
					args.Add(VisitExpr(expr));

			// Void calls must not carry a result name in the IR
			bool isVoid = funcType.ReturnType == VoidType;
			return _builder.BuildCall2(funcType, funcVal, args.ToArray(), isVoid ? "" : "call");
		}

		// Array read: primaryExpression index  →  arr[i]
		// GEP2 to get i32* element pointer, then Load2 to get the i32 value.
		if (context.index() != null)
		{
			string arrName = context.primaryExpression().operand().IDENTIFIER().GetText();
			var (arrPtr, arrType) = ResolveLocal(arrName);
			LLVMValueRef idx  = VisitExpr(context.index().expression());
			LLVMValueRef zero = LLVMValueRef.CreateConstInt(IntType, 0, false);
			LLVMValueRef elemPtr = _builder.BuildGEP2(arrType, arrPtr, new[] { zero, idx }, "elem_ptr");
			return _builder.BuildLoad2(IntType, elemPtr, "elem");
		}

		// selector, appendExpression, lengthExpression, capExpression
		// lengthExpression and capExpression dispatch through VisitChildren → their own Visit* methods.
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
		// len(arr) → compile-time i32 constant.
		// MiniGo only supports len on fixed-size integer arrays; the static length is
		// embedded in the array's LLVM type ([n x i32]) stored in the scope at declaration.
		// Array allocation is implemented in commit 9 — this will be fully testable then.
		var primary = context.expression()?.primaryExpression();
		var operand = primary?.operand();
		string? name = operand?.IDENTIFIER()?.GetText();

		if (name != null)
		{
			var (_, elemType) = ResolveLocal(name);
			if (elemType != default && elemType.Kind == LLVMTypeKind.LLVMArrayTypeKind)
				return LLVMValueRef.CreateConstInt(IntType, elemType.ArrayLength, false);
		}

		return LLVMValueRef.CreateConstInt(IntType, 0, false);
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
		// RETURN (expression | /*epsilon*/) SEMICOLON
		if (context.RETURN() != null)
		{
			if (context.expression() != null)
				_builder.BuildRet(VisitExpr(context.expression()));
			else
				_builder.BuildRetVoid();
			return null;
		}

		// PRINTLN LPAREN (expressionList | /*epsilon*/) RPAREN SEMICOLON
		if (context.PRINTLN() != null)
		{
			EmitPrintf(context.expressionList(), newline: true);
			return null;
		}

		// PRINT LPAREN (expressionList | /*epsilon*/) RPAREN SEMICOLON
		if (context.PRINT() != null)
		{
			EmitPrintf(context.expressionList(), newline: false);
			return null;
		}

		// ifStatement / loop / switch → commits 6, 7
		// simpleStatement, block, variableDecl, typeDecl → VisitChildren dispatches correctly
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
		// Optional init statement:  IF simpleStatement ; expression block ...
		if (context.simpleStatement() != null)
			Visit(context.simpleStatement());

		// ── Condition ────────────────────────────────────────────────────────
		LLVMValueRef cond = VisitExpr(context.expression());

		// Coerce non-i1 values to bool (e.g. an int used as a condition)
		if (cond.TypeOf != BoolType)
			cond = _builder.BuildICmp(
				LLVMIntPredicate.LLVMIntNE,
				cond,
				LLVMValueRef.CreateConstInt(cond.TypeOf, 0, false),
				"tobool");

		// ── Block layout ─────────────────────────────────────────────────────
		bool hasElse = context.ELSE() != null;

		LLVMBasicBlockRef thenBlock  = _currentFunction.AppendBasicBlock("if.then");
		LLVMBasicBlockRef elseBlock  = hasElse
			? _currentFunction.AppendBasicBlock("if.else")
			: default;
		LLVMBasicBlockRef mergeBlock = _currentFunction.AppendBasicBlock("if.merge");

		_builder.BuildCondBr(cond, thenBlock, hasElse ? elseBlock : mergeBlock);

		// ── Then branch ──────────────────────────────────────────────────────
		PositionAtEnd(thenBlock);
		Visit(context.block()[0]);
		// Use InsertBlock (not thenBlock) in case nested control flow moved the builder
		if (_builder.InsertBlock.Terminator == default)
			_builder.BuildBr(mergeBlock);

		// ── Else branch ──────────────────────────────────────────────────────
		if (hasElse)
		{
			PositionAtEnd(elseBlock);

			if (context.ifStatement() != null)
				Visit(context.ifStatement());          // else if chain (recursive)
			else
				Visit(context.block()[1]);             // else { }

			if (_builder.InsertBlock.Terminator == default)
				_builder.BuildBr(mergeBlock);
		}

		// ── Merge point — execution continues here ───────────────────────────
		PositionAtEnd(mergeBlock);

		return null;
	}

	public override object VisitLoop(MiniGoParser.LoopContext context)
	{
		// Detect which of the 4 loop variants we have:
		//   FOR block                                      → no init, no cond, no post
		//   FOR expression block                           → no init,    cond, no post
		//   FOR simpleStmt ; expression ; simpleStmt block →    init,    cond,    post
		//   FOR simpleStmt ; ; simpleStmt block            →    init, no cond,    post
		var stmts   = context.simpleStatement();
		bool hasInit = stmts.Length >= 1;
		bool hasPost = stmts.Length >= 2;
		bool hasCond = context.expression() != null;

		// ── Init ─────────────────────────────────────────────────────────────
		if (hasInit)
			Visit(stmts[0]);

		// ── Block layout ──────────────────────────────────────────────────────
		// cond_block only needed when there is a condition expression.
		// exit_block is always created; it may be unreachable for infinite loops.
		LLVMBasicBlockRef condBlock = hasCond
			? _currentFunction.AppendBasicBlock("loop.cond")
			: default;
		LLVMBasicBlockRef bodyBlock = _currentFunction.AppendBasicBlock("loop.body");
		LLVMBasicBlockRef exitBlock = _currentFunction.AppendBasicBlock("loop.exit");

		// Jump into the loop: check condition first, or go straight to body
		_builder.BuildBr(hasCond ? condBlock : bodyBlock);

		// ── Condition ─────────────────────────────────────────────────────────
		if (hasCond)
		{
			PositionAtEnd(condBlock);
			LLVMValueRef cond = VisitExpr(context.expression());
			if (cond.TypeOf != BoolType)
				cond = _builder.BuildICmp(
					LLVMIntPredicate.LLVMIntNE,
					cond,
					LLVMValueRef.CreateConstInt(cond.TypeOf, 0, false),
					"tobool");
			_builder.BuildCondBr(cond, bodyBlock, exitBlock);
		}

		// ── Body ──────────────────────────────────────────────────────────────
		PositionAtEnd(bodyBlock);
		Visit(context.block());

		// Post statement runs at the end of each iteration (e.g. i++)
		// Skip if the body already terminated (e.g. a return inside the loop).
		if (hasPost && _builder.InsertBlock.Terminator == default)
			Visit(stmts[1]);

		// Back-edge: jump back to condition check (or body for infinite loops)
		if (_builder.InsertBlock.Terminator == default)
			_builder.BuildBr(hasCond ? condBlock : bodyBlock);

		// ── Exit — execution continues here after the loop ────────────────────
		// For infinite loops this block is unreachable; VisitFuncDecl's implicit
		// BuildRetVoid handles any missing terminator if needed.
		PositionAtEnd(exitBlock);

		return null;
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
