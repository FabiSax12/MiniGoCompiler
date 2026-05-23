using MiniGo.Compiler.Errors;
using MiniGo.Compiler.Shared;

namespace MiniGo.Compiler.Tests.ErrorHandling;

public sealed class ErrorCollectorTests
{
	private const string FilePath = "test.txt";

	[Fact]
	public void Add_Error_IncrementsCountAndHasErrors()
	{
		var collector = new ErrorCollector(FilePath);

		collector.Add(Severity.Error, "test error", new SourceSpan(FilePath, 1, 0, 1), CompilationPhase.Parser);

		collector.Count.Should().Be(1);
		collector.ErrorCount.Should().Be(1);
		collector.HasErrors.Should().BeTrue();
	}

	[Fact]
	public void Add_Warning_DoesNotSetHasErrors()
	{
		var collector = new ErrorCollector(FilePath);

		collector.Add(Severity.Warning, "test warning", new SourceSpan(FilePath, 1, 0, 1), CompilationPhase.Type);

		collector.Count.Should().Be(1);
		collector.WarningCount.Should().Be(1);
		collector.ErrorCount.Should().Be(0);
		collector.HasErrors.Should().BeFalse();
	}

	[Fact]
	public void Add_MixedErrorsAndWarnings_TracksBoth()
	{
		var collector = new ErrorCollector(FilePath);

		collector.Add(Severity.Error, "err", new SourceSpan(FilePath, 1, 0, 1), CompilationPhase.Lexer);
		collector.Add(Severity.Warning, "warn", new SourceSpan(FilePath, 2, 0, 1), CompilationPhase.Type);
		collector.Add(Severity.Error, "err2", new SourceSpan(FilePath, 3, 0, 1), CompilationPhase.Parser);

		collector.Count.Should().Be(3);
		collector.ErrorCount.Should().Be(2);
		collector.WarningCount.Should().Be(1);
		collector.HasErrors.Should().BeTrue();
	}

	[Fact]
	public void AddLexerError_WithLineColumn_SetsCorrectPhase()
	{
		var collector = new ErrorCollector(FilePath);

		collector.AddLexerError("lexer error", 5, 10);

		var error = collector.GetSortedErrors()[0];
		error.Phase.Should().Be(CompilationPhase.Lexer);
		error.Span.Line.Should().Be(5);
		error.Span.Column.Should().Be(10);
	}

	[Fact]
	public void AddParserError_SetsParserPhase()
	{
		var collector = new ErrorCollector(FilePath);
		var token = new TestToken { Line = 3, Column = 5, StartIndex = 4, StopIndex = 6 };

		collector.AddParserError("parser error", token);

		var error = collector.GetSortedErrors()[0];
		error.Phase.Should().Be(CompilationPhase.Parser);
	}

	[Fact]
	public void GetSortedErrors_ReturnsSortedByLineThenColumn()
	{
		var collector = new ErrorCollector(FilePath);

		collector.Add(Severity.Error, "line 3 col 5", new SourceSpan(FilePath, 3, 5, 1), CompilationPhase.Parser);
		collector.Add(Severity.Error, "line 1 col 10", new SourceSpan(FilePath, 1, 10, 1), CompilationPhase.Lexer);
		collector.Add(Severity.Error, "line 1 col 2", new SourceSpan(FilePath, 1, 2, 1), CompilationPhase.Lexer);

		var sorted = collector.GetSortedErrors();

		sorted[0].Span.Line.Should().Be(1);
		sorted[0].Span.Column.Should().Be(2);
		sorted[1].Span.Line.Should().Be(1);
		sorted[1].Span.Column.Should().Be(10);
		sorted[2].Span.Line.Should().Be(3);
	}

	[Fact]
	public void Report_WritesToWriter()
	{
		var collector = new ErrorCollector(FilePath);
		collector.Add(Severity.Error, "test", new SourceSpan(FilePath, 1, 0, 1), CompilationPhase.Lexer);

		using var writer = new StringWriter();

		collector.Report(writer);

		writer.ToString().Should().Contain("test");
	}

	[Fact]
	public void Empty_Collector_HasNoErrors()
	{
		var collector = new ErrorCollector(FilePath);

		collector.Count.Should().Be(0);
		collector.HasErrors.Should().BeFalse();
		collector.ErrorCount.Should().Be(0);
		collector.WarningCount.Should().Be(0);
		collector.GetSortedErrors().Should().BeEmpty();
	}

	/// <summary>
	/// Minimal IToken implementation for tests that don't need the full ANTLR runtime.
	/// </summary>
	private sealed class TestToken : Antlr4.Runtime.IToken
	{
		public string Text { get; set; } = "";
		public int Type { get; set; }
		public int Line { get; set; }
		public int Column { get; set; }
		public int Channel { get; set; }
		public int TokenIndex { get; set; }
		public int StartIndex { get; set; }
		public int StopIndex { get; set; }
		public Antlr4.Runtime.ICharStream? InputStream { get; set; }
		public Antlr4.Runtime.ITokenSource? TokenSource { get; set; }
		public System.IO.Stream? TokenSourceInputStream { get; set; }
	}
}
