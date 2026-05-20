namespace MiniGo.Compiler.Errors;

/// <summary>
/// A single compilation error or warning with source location.
/// </summary>
public sealed class CompilationError
{
    public Severity Severity { get; }
    public string Message { get; }
    public SourceSpan Span { get; }
    public string Phase { get; }

    public CompilationError(Severity severity, string message, SourceSpan span, string phase)
    {
        Severity = severity;
        Message = message;
        Span = span;
        Phase = phase;
    }

    public override string ToString()
    {
        return $"{Span}: {Message}";
    }
}