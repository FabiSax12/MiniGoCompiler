using MiniGo.Compiler.Semantic;

namespace MiniGo.Compiler.Tests.Semantic;

public sealed class TypeSystemTests
{
	[Theory]
	[InlineData(Types.Integer, true)]
	[InlineData(Types.Float, true)]
	[InlineData(Types.Rune, true)]
	[InlineData(Types.String, false)]
	[InlineData(Types.Boolean, false)]
	[InlineData(Types.Array, false)]
	[InlineData(Types.Slice, false)]
	[InlineData(Types.Struct, false)]
	[InlineData(Types.Void, false)]
	[InlineData(Types.Unknown, false)]
	public void IsNumeric_ReturnsExpected(Types type, bool expected)
	{
		TypeSystem.IsNumeric(type).Should().Be(expected);
	}

	[Theory]
	[InlineData(Types.Integer, true)]
	[InlineData(Types.Rune, true)]
	[InlineData(Types.Float, false)]
	[InlineData(Types.String, false)]
	[InlineData(Types.Boolean, false)]
	public void IsInteger_ReturnsExpected(Types type, bool expected)
	{
		TypeSystem.IsInteger(type).Should().Be(expected);
	}

	[Theory]
	[InlineData(Types.Integer, true)]
	[InlineData(Types.Float, true)]
	[InlineData(Types.String, true)]
	[InlineData(Types.Rune, true)]
	[InlineData(Types.Boolean, false)]
	[InlineData(Types.Void, false)]
	public void IsOrdered_ReturnsExpected(Types type, bool expected)
	{
		TypeSystem.IsOrdered(type).Should().Be(expected);
	}

	[Theory]
	[InlineData(Types.Array, true)]
	[InlineData(Types.Slice, true)]
	[InlineData(Types.String, true)]
	[InlineData(Types.Integer, false)]
	[InlineData(Types.Boolean, false)]
	[InlineData(Types.Struct, false)]
	public void IsIndexable_ReturnsExpected(Types type, bool expected)
	{
		TypeSystem.IsIndexable(type).Should().Be(expected);
	}
}
