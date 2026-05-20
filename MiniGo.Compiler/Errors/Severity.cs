namespace MiniGo.Compiler.Errors;

/// <summary>
/// Severity level for a compilation error.
/// Error blocks compilation; Warning does not (reserved for future phases).
/// </summary>
public enum Severity
{
    Error,
    Warning
}