using Antlr4.Runtime;

namespace MiniGo.Compiler.Semantic.Symbols;

public class MethodSymbol(CommonToken token, Types type, int level, ParserRuleContext declaration)
    : BaseSymbol(token, type, level, declaration)
{
    public override bool IsMethod()
    {
        return true;
    }
};