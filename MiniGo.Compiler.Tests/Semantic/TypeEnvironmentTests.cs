using Antlr4.Runtime;
using MiniGo.Compiler.Semantic;
using MiniGo.Compiler.Semantic.Symbols;

namespace MiniGo.Compiler.Tests.Semantic;

public sealed class TypeEnvironmentTests
{
	[Fact]
	public void RegisterAlias_AddsAlias()
	{
		var env = new TypeEnvironment();

		var result = env.RegisterAlias("MyInt", Types.Integer);

		result.Should().BeTrue();
		env.ResolveAlias("MyInt").Should().Be(Types.Integer);
	}

	[Fact]
	public void RegisterAlias_Duplicate_ReturnsFalse()
	{
		var env = new TypeEnvironment();

		env.RegisterAlias("MyInt", Types.Integer);
		var result = env.RegisterAlias("MyInt", Types.Float);

		result.Should().BeFalse();
		env.ResolveAlias("MyInt").Should().Be(Types.Integer);
	}

	[Fact]
	public void ResolveAlias_Nonexistent_ReturnsNull()
	{
		var env = new TypeEnvironment();

		env.ResolveAlias("NotFound").Should().BeNull();
	}

	[Fact]
	public void BeginStruct_EndStruct_StoresFields()
	{
		var env = new TypeEnvironment();

		env.BeginStruct("Point");
		env.AddField("x", Types.Integer);
		env.AddField("y", Types.Integer);
		env.EndStruct();

		var fields = env.GetStructFields("Point");
		fields.Should().NotBeNull();
		fields!["x"].Should().Be(Types.Integer);
		fields["y"].Should().Be(Types.Integer);
	}

	[Fact]
	public void GetStructFields_Nonexistent_ReturnsNull()
	{
		var env = new TypeEnvironment();

		env.GetStructFields("NotFound").Should().BeNull();
	}

	[Fact]
	public void AddField_Duplicate_ReturnsFalse()
	{
		var env = new TypeEnvironment();

		env.BeginStruct("Foo");
		env.AddField("x", Types.Integer);
		var result = env.AddField("x", Types.Float);

		result.Should().BeFalse();
	}

	[Fact]
	public void GetStructNameForVariable_ReturnsDeclaredTypeName()
	{
		var env = new TypeEnvironment();
		var symbol = new VarSymbol(new CommonToken(1, "u"), Types.Unknown, 0, null!, "User");

		var result = env.GetStructNameForVariable(symbol);

		result.Should().Be("User");
	}

	[Fact]
	public void GetStructNameForVariable_NullDeclaredTypeName_ReturnsNull()
	{
		var env = new TypeEnvironment();
		var symbol = new VarSymbol(new CommonToken(1, "x"), Types.Integer, 0, null!, null);

		var result = env.GetStructNameForVariable(symbol);

		result.Should().BeNull();
	}

	[Fact]
	public void EndStruct_WithoutBeginStruct_DoesNotThrow()
	{
		var env = new TypeEnvironment();

		var act = () => env.EndStruct();

		act.Should().NotThrow();
	}
}
