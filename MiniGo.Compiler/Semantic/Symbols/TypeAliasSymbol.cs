using Antlr4.Runtime;

namespace MiniGo.Compiler.Semantic.Symbols;

/// <summary>
/// Symbol for a type alias declaration (e.g., "type MyInt int").
/// Stores the alias name and its underlying type.
/// </summary>
public sealed class TypeAliasSymbol(CommonToken token, Types underlyingType, int level, ParserRuleContext declaration)
	: BaseSymbol(token, underlyingType, level, declaration)
{
	/// <summary>
	/// The underlying type that this alias maps to.
	/// </summary>
	public Types UnderlyingType => GetTokenType();

	public override bool IsMethod() => false;
}
