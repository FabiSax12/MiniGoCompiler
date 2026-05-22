using Generated;

namespace MiniGo.Compiler.Semantic;

public static class TypeSystem
{
	public static bool IsNumeric(Types type) => type is Types.Integer or Types.Float or Types.Rune;
	public static bool IsInteger(Types type) => type is Types.Integer or Types.Rune;
	public static bool IsOrdered(Types type) => type is Types.Integer or Types.Float or Types.String or Types.Rune;
	public static bool IsIndexable(Types type) => type is Types.Array or Types.Slice or Types.String;

	public static Types LiteralType(MiniGoParser.LiteralContext context)
	{
		if (context.INTLITERAL() != null) return Types.Integer;
		if (context.FLOATLITERAL() != null) return Types.Float;
		if (context.RUNELITERAL() != null) return Types.Rune;
		if (context.RAWSTRINGLITERAL() != null) return Types.String;
		if (context.INTERPRETEDSTRINGLITERAL() != null) return Types.String;
		return Types.Unknown;
	}
}
