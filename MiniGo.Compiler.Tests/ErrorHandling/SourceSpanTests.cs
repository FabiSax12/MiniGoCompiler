using MiniGo.Compiler.Errors;

namespace MiniGo.Compiler.Tests.ErrorHandling;

public sealed class SourceSpanTests
{
	[Fact]
	public void Constructor_SetsAllProperties()
	{
		var span = new SourceSpan("file.txt", 10, 5, 3);

		span.FilePath.Should().Be("file.txt");
		span.Line.Should().Be(10);
		span.Column.Should().Be(5);
		span.Length.Should().Be(3);
	}

	[Fact]
	public void Constructor_DefaultLength_IsOne()
	{
		var span = new SourceSpan("file.txt", 1, 0);

		span.Length.Should().Be(1);
	}

	[Fact]
	public void FromCharPosition_CreatesSpanWithLengthOne()
	{
		var span = SourceSpan.FromCharPosition("file.txt", 7, 3);

		span.FilePath.Should().Be("file.txt");
		span.Line.Should().Be(7);
		span.Column.Should().Be(3);
		span.Length.Should().Be(1);
	}

	[Fact]
	public void FromToken_CalculatesLengthFromStartAndStopIndex()
	{
		var token = new TestToken { Line = 4, Column = 2, StartIndex = 10, StopIndex = 14 };

		var span = SourceSpan.FromToken(token, "file.txt");

		span.FilePath.Should().Be("file.txt");
		span.Line.Should().Be(4);
		span.Column.Should().Be(2);
		span.Length.Should().Be(5);
	}

	[Fact]
	public void ToString_ReturnsFormattedLineAndColumn()
	{
		var span = new SourceSpan("file.txt", 42, 7);

		span.ToString().Should().Be("line 42:7");
	}

	[Fact]
	public void Equality_TwoIdenticalSpans_AreEqual()
	{
		var a = new SourceSpan("file.txt", 1, 0, 3);
		var b = new SourceSpan("file.txt", 1, 0, 3);

		a.Equals(b).Should().BeTrue();
		a.GetHashCode().Should().Be(b.GetHashCode());
	}

	[Fact]
	public void Equality_DifferentSpans_AreNotEqual()
	{
		var a = new SourceSpan("file.txt", 1, 0, 3);
		var b = new SourceSpan("file.txt", 2, 0, 3);

		a.Equals(b).Should().BeFalse();
	}

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
