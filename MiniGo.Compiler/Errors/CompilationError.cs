using MiniGo.Compiler.Shared;

namespace MiniGo.Compiler.Errors;

/// <summary>
/// A single compilation error or warning with source location.
/// </summary>
public sealed class CompilationError
{
    public Severity Severity { get; }
    public string Message { get; }
    public SourceSpan Span { get; }
    public CompilationPhase Phase { get; }

    public CompilationError(Severity severity, string message, SourceSpan span, CompilationPhase phase)
    {
        Severity = severity;
        Message = message;
        Span = span;
        Phase = phase;
    }

    public override string ToString()
    {
        var phase = Phase.ToString().ToUpper();
        var label = Severity == Severity.Error ? "Error" : "Warning";
        return $"[{phase} {label}] {Span}: {Message}";
    }
}