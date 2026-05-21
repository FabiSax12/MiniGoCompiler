using Antlr4.Runtime;
using MiniGo.Compiler.Shared;

namespace MiniGo.Compiler.Errors.Listeners;

/// <summary>
/// Custom error listener for the parser. Collects errors into an ErrorCollector
/// instead of printing them immediately. Uses ANTLR's default message format.
/// </summary>
public sealed class ParserErrorListener : IAntlrErrorListener<IToken>
{
    private readonly ErrorCollector _collector;

    public ParserErrorListener(ErrorCollector collector)
    {
        _collector = collector;
    }

    public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int column, string msg, RecognitionException? e)
    {
        if (offendingSymbol != null)
        {
            _collector.AddParserError(msg, offendingSymbol);
        }
        else
        {
            _collector.Add(Compiler.Errors.Severity.Error, msg, new SourceSpan("", line, column, 0), CompilationPhase.Parser);
        }
    }
}