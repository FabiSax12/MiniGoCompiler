using Antlr4.Runtime;

namespace MiniGo.Compiler.Errors.Listeners;

/// <summary>
/// Custom error listener for the lexer. Collects errors into an ErrorCollector
/// instead of printing them immediately. Uses ANTLR's default message format.
/// </summary>
public sealed class LexerErrorListener : IAntlrErrorListener<int>
{
    private readonly ErrorCollector _collector;

    public LexerErrorListener(ErrorCollector collector)
    {
        _collector = collector;
    }

    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int column, string msg, RecognitionException? e)
    {
        _collector.AddLexerError(msg, line, column);
    }
}