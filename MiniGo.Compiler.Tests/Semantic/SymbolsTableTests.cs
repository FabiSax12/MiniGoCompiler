using Antlr4.Runtime;
using MiniGo.Compiler.Semantic;
using MiniGo.Compiler.Semantic.Symbols;

namespace MiniGo.Compiler.Tests.Semantic;

public sealed class SymbolsTableTests
{
	private static CommonToken MakeToken(string text, int type = 1) =>
		new(type, text);

	[Fact]
	public void Define_SucceedsAndSymbolIsLookedUp()
	{
		var table = new SymbolsTable();
		table.OpenScope();

		var symbol = new VarSymbol(MakeToken("x"), Types.Integer, 0, null!);
		var result = table.Define(symbol);

		result.Should().BeTrue();
		table.Lookup("x").Should().Be(symbol);
	}

	[Fact]
	public void Define_DuplicateName_ReturnsFalse()
	{
		var table = new SymbolsTable();
		table.OpenScope();

		var a = new VarSymbol(MakeToken("x"), Types.Integer, 0, null!);
		var b = new VarSymbol(MakeToken("x"), Types.Float, 0, null!);

		table.Define(a);
		var result = table.Define(b);

		result.Should().BeFalse();
	}

	[Fact]
	public void Lookup_FindsSymbolInParentScope()
	{
		var table = new SymbolsTable();
		table.OpenScope();

		var symbol = new VarSymbol(MakeToken("parentVar"), Types.Integer, 0, null!);
		table.Define(symbol);

		table.OpenScope();
		table.Lookup("parentVar").Should().Be(symbol);
	}

	[Fact]
	public void Lookup_ShadowedSymbol_ReturnsInnermost()
	{
		var table = new SymbolsTable();
		table.OpenScope();

		var outer = new VarSymbol(MakeToken("x"), Types.Integer, 0, null!);
		table.Define(outer);

		table.OpenScope();
		var inner = new VarSymbol(MakeToken("x"), Types.Float, 1, null!);
		table.Define(inner);

		table.Lookup("x").Should().Be(inner);
	}

	[Fact]
	public void Lookup_NotFound_ReturnsNull()
	{
		var table = new SymbolsTable();
		table.OpenScope();

		table.Lookup("nonexistent").Should().BeNull();
	}

	[Fact]
	public void LookupCurrent_OnlyFindsInCurrentScope()
	{
		var table = new SymbolsTable();
		table.OpenScope();

		var outer = new VarSymbol(MakeToken("x"), Types.Integer, 0, null!);
		table.Define(outer);

		table.OpenScope();
		table.LookupCurrent("x").Should().BeNull();
		table.Lookup("x").Should().Be(outer);
	}

	[Fact]
	public void LookupCurrent_ReturnsNullWhenTableEmpty()
	{
		var table = new SymbolsTable();

		table.LookupCurrent("x").Should().BeNull();
	}

	[Fact]
	public void CloseScope_RemovesSymbolsDefinedInThatScope()
	{
		var table = new SymbolsTable();
		table.OpenScope();

		var symbol = new VarSymbol(MakeToken("x"), Types.Integer, 0, null!);
		table.Define(symbol);

		table.OpenScope();
		var inner = new VarSymbol(MakeToken("y"), Types.Float, 1, null!);
		table.Define(inner);

		table.Lookup("y").Should().Be(inner);
		table.CloseScope();

		table.Lookup("x").Should().Be(symbol);
		table.Lookup("y").Should().BeNull();
	}

	[Fact]
	public void Define_NoOpenScope_AutoCreatesOne()
	{
		var table = new SymbolsTable();

		var symbol = new VarSymbol(MakeToken("x"), Types.Integer, 0, null!);
		var result = table.Define(symbol);

		result.Should().BeTrue();
		table.Lookup("x").Should().Be(symbol);
	}

	[Fact]
	public void GetLevel_EmptyTable_ReturnsNegativeOne()
	{
		var table = new SymbolsTable();

		table.GetLevel().Should().Be(-1);
	}

	[Fact]
	public void GetLevel_TracksNesting()
	{
		var table = new SymbolsTable();
		table.OpenScope();

		table.GetLevel().Should().Be(0);

		table.OpenScope();
		table.GetLevel().Should().Be(1);

		table.CloseScope();
		table.GetLevel().Should().Be(0);
	}

	[Fact]
	public void CloseScope_EmptyTable_DoesNotThrow()
	{
		var table = new SymbolsTable();

		var act = () => table.CloseScope();

		act.Should().NotThrow();
	}
}
