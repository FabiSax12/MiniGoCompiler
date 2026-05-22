using MiniGo.Compiler.Semantic.Symbols;

namespace MiniGo.Compiler.Semantic;

public sealed class TypeEnvironment
{
	private readonly Dictionary<string, Types> _aliases = new();
	private readonly Dictionary<string, Dictionary<string, Types>> _structs = new();
	private Dictionary<string, Types>? _pendingFields;
	private string? _pendingName;

	public bool RegisterAlias(string name, Types type) => _aliases.TryAdd(name, type);
	public Types? ResolveAlias(string name) => _aliases.TryGetValue(name, out var t) ? t : null;

	public void BeginStruct(string name)
	{
		_pendingName = name;
		_pendingFields = new Dictionary<string, Types>();
	}

	public bool AddField(string name, Types type) => _pendingFields!.TryAdd(name, type);

	public void EndStruct()
	{
		if (_pendingName != null)
		{
			_structs[_pendingName] = _pendingFields!;
		}

		_pendingName = null;
		_pendingFields = null;
	}

	public Dictionary<string, Types>? GetStructFields(string name) =>
		_structs.TryGetValue(name, out var fields) ? fields : null;

	public string? GetStructNameForVariable(ISymbol? symbol) =>
		(symbol as VarSymbol)?.DeclaredTypeName;
}
