using Antlr4.Runtime;
using MiniGo.Compiler.Errors;
using MiniGo.Compiler.Semantic.Symbols;
using MiniGo.Compiler.Shared;

namespace MiniGo.Compiler.Semantic;

public sealed class ScopeTracker
{
	private readonly Stack<List<VarSymbol>> _scopeVarSymbols = new();

	public void OpenScope() => _scopeVarSymbols.Push(new List<VarSymbol>());

	public void CloseScope(ErrorCollector collector, string filePath)
	{
		var scopeVars = _scopeVarSymbols.Pop();
		foreach (var vs in scopeVars)
		{
			if (!vs.IsUsed)
			{
				collector.Add(
					Severity.Warning,
					$"Variable '{vs.GetToken().Text}' is declared but never used",
					SourceSpan.FromToken(vs.GetToken(), filePath),
					CompilationPhase.Type
				);
			}
		}
	}

	public void Track(VarSymbol symbol)
	{
		if (_scopeVarSymbols.Count > 0)
		{
			_scopeVarSymbols.Peek().Add(symbol);
		}
	}
}
