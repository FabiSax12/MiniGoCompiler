namespace MiniGo.Compiler.Errors;

/// <summary>
/// Represents a position span in a source file.
/// Line is 1-based (human readable), column is 0-based.
/// </summary>
public readonly struct SourceSpan
{
    public string FilePath { get; }
    public int Line { get; }
    public int Column { get; }
    public int Length { get; }

    public SourceSpan(string filePath, int line, int column, int length = 1)
    {
        FilePath = filePath;
        Line = line;
        Column = column;
        Length = length;
    }

    /// <summary>
    /// Creates a span from an ANTLR RecognizerContext (offending token).
    /// </summary>
    public static SourceSpan FromToken(Antlr4.Runtime.IToken token, string filePath)
    {
        return new SourceSpan(
            filePath,
            token.Line,
            token.Column,
            token.StopIndex - token.StartIndex + 1
        );
    }

    /// <summary>
    /// Creates a span from lexer mode where we only have a character position.
    /// </summary>
    public static SourceSpan FromCharPosition(string filePath, int line, int column)
    {
        return new SourceSpan(filePath, line, column, 1);
    }

    public override string ToString() => $"line {Line}:{Column}";
}