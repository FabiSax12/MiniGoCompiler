using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Generated;

namespace MiniGo.Compiler.Errors;

/// <summary>
/// Custom ANTLR error strategy for the MiniGo parser.
/// Extends the default error strategy with delimiter-based panic mode recovery.
/// Only overrides Recover(); Sync() is left unchanged so the base class can
/// handle follow-set recovery naturally.
/// </summary>
public sealed class MiniGoErrorStrategy : DefaultErrorStrategy
{
    private const int EOF = TokenConstants.EOF;

    /// <summary>
    /// Tokens used as synchronization anchors in panic mode recovery.
    /// </summary>
    private static readonly HashSet<int> SyncAnchors = new()
    {
        MiniGoParser.SEMICOLON,
        MiniGoParser.LBRACE,
        MiniGoParser.RBRACE,
        MiniGoParser.LBRACKET,
        MiniGoParser.RBRACKET,
        MiniGoParser.LPAREN,
        MiniGoParser.RPAREN,
        MiniGoParser.PACKAGE,
        MiniGoParser.VAR,
        MiniGoParser.TYPE,
        MiniGoParser.FUNC,
        MiniGoParser.STRUCT,
        MiniGoParser.IF,
        MiniGoParser.ELSE,
        MiniGoParser.FOR,
        MiniGoParser.SWITCH,
        MiniGoParser.CASE,
        MiniGoParser.DEFAULT,
        MiniGoParser.RETURN,
        MiniGoParser.BREAK,
        MiniGoParser.CONTINUE,
        EOF
    };

    /// <summary>
    /// Main error recovery entry point. For InputMismatchException, attempts
    /// single-token deletion first. For other exceptions, goes directly to
    /// delimiter-based panic mode recovery. Falls back to the base implementation
    /// for unknown exception types.
    /// </summary>
    public override void Recover(Parser recognizer, RecognitionException? e)
    {
        if (e is InputMismatchException)
        {
            // Try single-token deletion for operator tokens that are clearly garbage
            IToken offending = ((InputMismatchException)e).OffendingToken;
            int t = offending.Type;

            // Only delete if it's NOT a sync anchor (we don't want to skip structure tokens)
            if (!SyncAnchors.Contains(t) && t != EOF)
            {
                recognizer.Consume();
                return;
            }
        }

        // Fall back to delimiter-based panic mode recovery
        PanicModeRecover(recognizer);
    }

    /// <summary>
    /// Inline recovery when parser cannot match within a rule. If current token
    /// is a sync anchor, skip it; otherwise delegate to base implementation.
    /// </summary>
    public override IToken RecoverInline(Parser recognizer)
    {
        IToken current = recognizer.CurrentToken;

        if (SyncAnchors.Contains(current.Type) && current.Type != EOF)
        {
            recognizer.Consume();
            return current;
        }

        return base.RecoverInline(recognizer);
    }

    /// <summary>
    /// Panic mode: skip tokens until finding a sync anchor, then CONSUME the anchor
    /// so parsing continues from the token AFTER it.
    /// </summary>
    private void PanicModeRecover(Parser recognizer)
    {
        int skipped = 0;
        const int MAX_SKIP = 50;

        // Skip until we find a sync anchor
        while (!SyncAnchors.Contains(recognizer.CurrentToken.Type)
               && recognizer.CurrentToken.Type != EOF
               && skipped < MAX_SKIP)
        {
            recognizer.Consume();
            skipped++;
        }

        // CONSUME the sync anchor so we're positioned after it
        if (recognizer.CurrentToken.Type != EOF)
        {
            recognizer.Consume();
        }
    }
}