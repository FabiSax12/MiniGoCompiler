using Antlr4.Runtime;

namespace MiniGo.Compiler.Semantic.Symbols;

public interface ISymbol
{
    CommonToken GetToken();
    Types GetTokenType();
    int GetLevel();
    ParserRuleContext GetDeclaration();
    bool IsMethod();
}