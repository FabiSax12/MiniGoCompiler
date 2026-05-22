using Antlr4.Runtime;

namespace MiniGo.Compiler.Semantic.Symbols;

/// <summary>
/// Symbol for a function/method declaration, storing its return type and parameter signatures.
/// </summary>
public sealed class MethodSymbol(CommonToken token, Types returnType, int level, ParserRuleContext declaration)
	: BaseSymbol(token, returnType, level, declaration)
{
	private readonly List<VarSymbol> _parameters = new();

	/// <summary>
	/// Parameters declared for this function (name, type).
	/// </summary>
	public IReadOnlyList<VarSymbol> Parameters => _parameters;

	/// <summary>
	/// Register a parameter for this function.
	/// </summary>
	public void AddParameter(VarSymbol param) => _parameters.Add(param);

	public override bool IsMethod() => true;
}
