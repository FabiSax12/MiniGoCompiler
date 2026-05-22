using Generated;

namespace MiniGo.Compiler.Semantic;

public static class TypeResolver
{
	public static Types Resolve(MiniGoParser.DeclTypeContext? context)
	{
		if (context == null)
		{
			return Types.Void;
		}

		if (context.IDENTIFIER() != null)
		{
			var name = context.IDENTIFIER().GetText();
			return name switch
			{
				"int" => Types.Integer,
				"float" or "float64" or "float32" => Types.Float,
				"bool" => Types.Boolean,
				"string" => Types.String,
				"rune" => Types.Rune,
				_ => Types.Unknown
			};
		}

		if (context.sliceDeclType() != null)
		{
			return Types.Slice;
		}

		if (context.arrayDeclType() != null)
		{
			return Types.Array;
		}

		if (context.structDeclType() != null)
		{
			return Types.Struct;
		}

		if (context.declType() != null)
		{
			return Resolve(context.declType());
		}

		return Types.Unknown;
	}
}
