namespace MiniGo.Compiler.Semantic;

using System.Collections.Generic;
using MiniGo.Compiler.Semantic.Symbols;

public sealed class SymbolsTable
{
	private readonly List<Dictionary<string, ISymbol>> _scopes = new();
	
	public void OpenScope()
	{
		_scopes.Add(new Dictionary<string, ISymbol>());
	}
	
	public void CloseScope()
	{
		if (_scopes.Count > 0)
		{
			_scopes.RemoveAt(_scopes.Count - 1);
		}
	}
	
	public int GetLevel() => _scopes.Count > 0 ? _scopes.Count - 1 : -1;

	public bool Define(ISymbol symbol)
	{
		if (_scopes.Count == 0)
		{
			OpenScope();
		}

		var currentScope = _scopes[^1];
		string name = symbol.GetToken().Text;

		if (currentScope.ContainsKey(name))
		{
			return false;
		}

		currentScope[name] = symbol;
		return true;
	}

	public ISymbol? Lookup(string name)
	{
		for (int i = _scopes.Count - 1; i >= 0; i--)
		{
			if (_scopes[i].TryGetValue(name, out var symbol))
			{
				return symbol;
			}
		}

		return null;
	}
	
	public ISymbol? LookupCurrent(string name)
	{
		if (_scopes.Count == 0)
		{
			return null;
		}

		_scopes[^1].TryGetValue(name, out var symbol);
		return symbol;
	}
}
