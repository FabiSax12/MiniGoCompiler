using System.Text;

namespace MiniGo.Compiler.Errors;

/// <summary>
/// Accumulates compilation errors and reports them sorted by source position.
/// Errors are collected throughout lexing and parsing, then reported together at the end.
/// </summary>
public sealed class ErrorCollector
{
    private readonly List<CompilationError> _errors = new();
    private readonly string _filePath;

    public ErrorCollector(string filePath)
    {
        _filePath = filePath;
    }

    public void Add(Severity severity, string message, SourceSpan span, string phase)
    {
        _errors.Add(new CompilationError(severity, message, span, phase));
    }

    public void AddLexerError(string message, int line, int column)
    {
        Add(Severity.Error, message, new SourceSpan(_filePath, line, column, 1), "Lexer");
    }

    public void AddLexerError(string message, Antlr4.Runtime.IToken token)
    {
        Add(Severity.Error, message, SourceSpan.FromToken(token, _filePath), "Lexer");
    }

    public void AddParserError(string message, Antlr4.Runtime.IToken token)
    {
        Add(Severity.Error, message, SourceSpan.FromToken(token, _filePath), "Parser");
    }

    public int Count => _errors.Count;
    public bool HasErrors => _errors.Any(e => e.Severity == Severity.Error);
    public int ErrorCount => _errors.Count(e => e.Severity == Severity.Error);
    public int WarningCount => _errors.Count(e => e.Severity == Severity.Warning);

    public IReadOnlyList<CompilationError> GetSortedErrors()
    {
        return _errors
            .OrderBy(e => e.Span.Line)
            .ThenBy(e => e.Span.Column)
            .ToList();
    }

    /// <summary>
    /// Writes all collected errors to the provided writer, sorted by position.
    /// </summary>
    public void Report(TextWriter writer)
    {
        var sorted = GetSortedErrors();
        foreach (var error in sorted)
        {
            // Write Errors in RED and warnings in YELLOW
            if (error.Severity == Severity.Error)
            {
                writer.WriteLine($"\x1b[31m{ error }\x1b[0m");
            } else if (error.Severity == Severity.Warning)
            {
                writer.WriteLine($"\033[33m{ error }\x1b[0m");
            }
        }
    }
}