using Antlr4.Runtime;

namespace MiniGo.Compiler.Semantic.Symbols;

public abstract class BaseSymbol(CommonToken token, Types type, int level, ParserRuleContext declaration) : ISymbol
{
    public CommonToken GetToken() { return token;}

    public Types GetTokenType() { return type;}

    public int GetLevel() { return level;}

    public ParserRuleContext GetDeclaration() { return declaration;}

    public virtual bool IsMethod() { return false;}
}