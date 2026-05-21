using Generated;

namespace MiniGo.Compiler.Semantic;

public static class TypeResolver
{
    public static Types Resolve(MiniGoParser.DeclTypeContext? context)
    {
        if (context == null)
        {
            return Types.Unknown;
        }

        if (context.IDENTIFIER() != null)
        {
            var name = context.IDENTIFIER().GetText();
            return name switch
            {
                "int" => Types.Integer,
                "bool" => Types.Boolean,
                "string" => Types.String,
                _ => Types.Unknown
            };
        }

        if (context.sliceDeclType() != null)
        {
            return Types.Integer;
        }

        if (context.arrayDeclType() != null)
        {
            return Types.Integer;
        }

        if (context.structDeclType() != null)
        {
            return Types.Integer;
        }

        if (context.declType() != null)
        {
            return Resolve(context.declType());
        }

        return Types.Integer;
    }
}