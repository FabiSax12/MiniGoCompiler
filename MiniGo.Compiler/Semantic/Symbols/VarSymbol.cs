using Antlr4.Runtime;

namespace MiniGo.Compiler.Semantic.Symbols;

public sealed class VarSymbol : BaseSymbol
{
	/// <summary>
	/// For variables declared with a named type (e.g., "var u User"), stores the type name
	/// for struct field resolution. Null for built-in types and expressions.
	/// </summary>
	public string? DeclaredTypeName { get; }

	/// <summary>Marks whether this variable has been read (referenced) somewhere.</summary>
	public bool IsUsed { get; set; }

	public VarSymbol(CommonToken token, Types type, int level, ParserRuleContext declaration, string? declaredTypeName = null)
		: base(token, type, level, declaration)
	{
		DeclaredTypeName = declaredTypeName;
	}

	public override bool IsMethod() => false;
}
