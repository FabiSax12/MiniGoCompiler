using Generated;

namespace MiniGo.Compiler.Semantic;

public static class TypeResolver
{
	public static Types Resolve(MiniGoParser.DeclTypeContext? context)
	{
		return Resolve(context, null);
	}

	/// <summary>
	/// Resolves a declType context to a Types enum value.
	/// If the type name is not a built-in, the lookup function is consulted for user-defined type aliases.
	/// </summary>
	public static Types Resolve(MiniGoParser.DeclTypeContext? context, Func<string, Types?>? lookup)
	{
		if (context == null)
		{
			return Types.Void;
		}

		if (context.IDENTIFIER() != null)
		{
			var name = context.IDENTIFIER().GetText();
			var result = name switch
			{
				"int" => Types.Integer,
				"float" or "float64" or "float32" => Types.Float,
				"bool" => Types.Boolean,
				"string" => Types.String,
				"rune" => Types.Rune,
				_ => Types.Unknown
			};

			if (result == Types.Unknown && lookup != null)
			{
				return lookup(name) ?? Types.Unknown;
			}

			return result;
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
			return Resolve(context.declType(), lookup);
		}

		return Types.Unknown;
	}
}
